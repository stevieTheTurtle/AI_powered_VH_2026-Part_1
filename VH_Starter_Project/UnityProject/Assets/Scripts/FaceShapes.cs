using System.Collections.Generic;
using UnityEngine;

// One blendshape on one mesh.
public struct ShapeRef
{
    public SkinnedMeshRenderer renderer;
    public int index;
    public void Set(float weight) => renderer.SetBlendShapeWeight(index, weight);
}

// A face is often several meshes that share the same shape names
// (CC4: body, eye occlusion, tearline, brows, teeth, tongue, hair).
// Find a shape by name on all of them, so the brows and eyelashes follow the skin.
public static class FaceShapes
{
    // Per mesh: the exact name if it exists ("V_Tight" must not also catch "V_Tight_O"),
    // otherwise every shape containing the fragment ("Eye_Blink" -> Eye_Blink_L and Eye_Blink_R).
    public static List<ShapeRef> Find(IEnumerable<SkinnedMeshRenderer> faces, string part)
    {
        var found = new List<ShapeRef>();
        if (string.IsNullOrEmpty(part) || faces == null) return found;
        foreach (var r in faces)
        {
            if (r == null || r.sharedMesh == null) continue;
            var mesh = r.sharedMesh;
            int exact = mesh.GetBlendShapeIndex(part);
            if (exact >= 0) { found.Add(new ShapeRef { renderer = r, index = exact }); continue; }
            for (int i = 0; i < mesh.blendShapeCount; i++)
                if (mesh.GetBlendShapeName(i).Contains(part)) found.Add(new ShapeRef { renderer = r, index = i });
        }
        return found;
    }

    public static bool Any(IList<SkinnedMeshRenderer> faces)
    {
        if (faces == null) return false;
        foreach (var r in faces) if (r != null && r.sharedMesh != null) return true;
        return false;
    }
}
