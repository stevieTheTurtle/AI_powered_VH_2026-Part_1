using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Rebuilds character materials whose shader is missing in this project (Reallusion CC4 HDRP
// shader graphs, HDRP/Lit) as URP Lit, from the textures the original materials already reference.
// Non-destructive: the original materials and the FBX are not touched (an HDRP project can still
// use them); the URP copies go to Assets/Characters/URP_Materials and are assigned
// only on the character instances in the generated scenes.
// Not reproduced: wrinkle maps, subsurface scattering, refractive eyes, two-pass hair sorting.
public static class CC4MaterialConverter
{
    const string OutDir = "Assets/Characters/URP_Materials";
    static readonly Dictionary<Material, Material> done = new Dictionary<Material, Material>();

    public static void BeginBuild() => done.Clear();

    public static bool NeedsConversion(Material m) =>
        m != null && (m.shader == null || m.shader.name == "Hidden/InternalErrorShader" || !m.shader.isSupported
                      || m.shader.name.StartsWith("HDRP/"));

    public static void ApplyToInstance(GameObject go)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
                if (NeedsConversion(mats[i])) { mats[i] = Convert(mats[i]); changed = true; }
            if (changed) r.sharedMaterials = mats;
        }
    }

    public static Material Convert(Material src)
    {
        if (done.TryGetValue(src, out var cached) && cached != null) return cached;
        if (!AssetDatabase.IsValidFolder(OutDir))
        {
            AssetDatabase.CreateFolder("Assets/Characters", "URP_Materials");
        }

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        string path = $"{OutDir}/{src.name}_URP.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
        else { m.shader = shader; m.shaderKeywords = new string[0]; }

        var tex = SavedTextures(src);
        var flt = SavedFloats(src);
        string n = src.name.ToLowerInvariant();
        float F(string key, float def) => flt.TryGetValue(key, out var v) ? v : def;

        Texture albedo = First(tex, "_BaseColorMap", "_DiffuseMap", "_MainTex", "_CorneaDiffuseMap");
        Texture normal = First(tex, "_NormalMap");
        Texture mask = First(tex, "_MaskMap");
        if (mask != null && !AssetDatabase.GetAssetPath(mask).EndsWith("_HDRP.png")) mask = null;   // only HDRP mask maps (R metal, G AO, A smooth)

        bool transparent = F("_SurfaceType", 0) > 0.5f;
        bool clip = F("_AlphaCutoffEnable", 0) > 0.5f;
        float cutoff = F("_AlphaCutoff", F("_AlphaClip", 0.5f));
        bool doubleSided = F("_DoubleSidedEnable", 0) > 0.5f;
        float smooth = 0.5f;
        Color color = Color.white;

        if (n.Contains("eye_occlusion") || n.Contains("tearline"))
        {
            // Procedural HDRP shading with no textures: invisible in URP.
            albedo = null; normal = null; mask = null; transparent = true; clip = false;
            color = new Color(1, 1, 1, 0);
        }
        else if (n.Contains("cornea"))
        {
            // Glossy clear shell over the eyeball.
            albedo = null; normal = null; mask = null; transparent = true; clip = false; smooth = 0.95f;
            color = new Color(1, 1, 1, 0.06f);
        }
        else if (n.StartsWith("std_eye_"))
        {
            albedo = FindTexture($"{src.name}_Diffuse") ?? albedo;           // iris + sclera from the FBX textures
            normal = FindTexture($"{src.name}_Normal") ?? normal;
            transparent = false; clip = false; smooth = 0.7f;
        }
        else if (n.Contains("hair_transparency") || n.Contains("brow_transparency"))
        {
            // Two-pass hair: 1st pass alpha-tested and depth-writing, 2nd pass soft edges.
            bool second = n.EndsWith("2nd_pass");
            transparent = second; clip = !second; cutoff = second ? 0.05f : 0.5f; doubleSided = true; smooth = 0.4f;
        }
        else if (n.Contains("eyelash") || n.Contains("scalp") || n.Contains("brow_base"))
        {
            transparent = true; clip = false; doubleSided = true; smooth = 0.1f;
        }
        else if (n.Contains("skin") || n.Contains("nails"))
        {
            transparent = false; clip = false; smooth = 0.45f;
        }
        else if (n.Contains("teeth") || n.Contains("tongue"))
        {
            transparent = false; clip = false; smooth = 0.6f;
        }

        if (albedo != null) m.SetTexture("_BaseMap", albedo);
        m.SetColor("_BaseColor", color);
        if (normal != null)
        {
            EnsureNormalMap(normal);
            m.SetTexture("_BumpMap", normal); m.EnableKeyword("_NORMALMAP");
        }
        if (mask != null)
        {
            EnsureLinear(mask);
            m.SetTexture("_MetallicGlossMap", mask); m.EnableKeyword("_METALLICSPECGLOSSMAP");
            m.SetTexture("_OcclusionMap", mask); m.EnableKeyword("_OCCLUSIONMAP");
            m.SetFloat("_OcclusionStrength", 1f);
        }
        m.SetFloat("_Metallic", 0f);
        m.SetFloat("_Smoothness", smooth);
        SetupSurface(m, transparent, clip, cutoff, doubleSided);

        EditorUtility.SetDirty(m);
        done[src] = m;
        return m;
    }

    static void SetupSurface(Material m, bool transparent, bool clip, float cutoff, bool doubleSided)
    {
        m.SetFloat("_Surface", transparent ? 1 : 0);
        m.SetFloat("_Blend", 0);
        // URP 17 defaults "Preserve Specular Lighting" on: specular is added at full strength even
        // where alpha is ~0, so hair and brow strand edges glow white. Plain alpha blend instead.
        m.SetFloat("_BlendModePreserveSpecular", 0);
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.DisableKeyword("_ALPHAMODULATE_ON");
        m.SetFloat("_AlphaClip", clip ? 1 : 0);
        m.SetFloat("_Cutoff", cutoff);
        m.SetFloat("_Cull", doubleSided ? 0 : 2);
        m.doubleSidedGI = doubleSided;
        if (clip) m.EnableKeyword("_ALPHATEST_ON"); else m.DisableKeyword("_ALPHATEST_ON");
        if (transparent)
        {
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        else
        {
            m.SetOverrideTag("RenderType", clip ? "TransparentCutout" : "Opaque");
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
            m.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.Zero);
            m.SetFloat("_ZWrite", 1);
            m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = clip ? (int)UnityEngine.Rendering.RenderQueue.AlphaTest : (int)UnityEngine.Rendering.RenderQueue.Geometry;
        }
    }

    // Saved properties survive a missing shader, the Material API does not: read them serialized.
    static Dictionary<string, Texture> SavedTextures(Material src)
    {
        var result = new Dictionary<string, Texture>();
        var envs = new SerializedObject(src).FindProperty("m_SavedProperties.m_TexEnvs");
        for (int i = 0; envs != null && i < envs.arraySize; i++)
        {
            var e = envs.GetArrayElementAtIndex(i);
            var t = e.FindPropertyRelative("second.m_Texture").objectReferenceValue as Texture;
            if (t != null) result[e.FindPropertyRelative("first").stringValue] = t;
        }
        return result;
    }

    static Dictionary<string, float> SavedFloats(Material src)
    {
        var result = new Dictionary<string, float>();
        var fl = new SerializedObject(src).FindProperty("m_SavedProperties.m_Floats");
        for (int i = 0; fl != null && i < fl.arraySize; i++)
        {
            var e = fl.GetArrayElementAtIndex(i);
            result[e.FindPropertyRelative("first").stringValue] = e.FindPropertyRelative("second").floatValue;
        }
        return result;
    }

    static Texture First(Dictionary<string, Texture> t, params string[] keys)
    {
        foreach (var k in keys) if (t.TryGetValue(k, out var v)) return v;
        return null;
    }

    static Texture FindTexture(string exactName)
    {
        var guid = AssetDatabase.FindAssets($"{exactName} t:Texture2D")
            .FirstOrDefault(g => Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(g)) == exactName);
        return guid == null ? null : AssetDatabase.LoadAssetAtPath<Texture>(AssetDatabase.GUIDToAssetPath(guid));
    }

    static void EnsureNormalMap(Texture t)
    {
        if (AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(t)) is TextureImporter ti && ti.textureType != TextureImporterType.NormalMap)
        { ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport(); }
    }

    static void EnsureLinear(Texture t)
    {
        if (AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(t)) is TextureImporter ti && ti.sRGBTexture)
        { ti.sRGBTexture = false; ti.SaveAndReimport(); }
    }

    [MenuItem("Masterclass/Convert Selected Materials to URP")]
    static void ConvertSelected()
    {
        BeginBuild();
        int count = 0;
        foreach (var m in Selection.GetFiltered<Material>(SelectionMode.Assets | SelectionMode.DeepAssets))
            if (NeedsConversion(m)) { Convert(m); count++; }
        AssetDatabase.SaveAssets();
        Debug.Log($"Masterclass: {count} material(s) converted to {OutDir}");
    }
}
