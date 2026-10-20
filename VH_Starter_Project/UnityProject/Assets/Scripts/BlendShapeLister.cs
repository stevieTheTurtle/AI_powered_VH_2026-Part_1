using UnityEngine;

// Body ▸ Face: prints name and index of every blendshape of every face mesh.
// Right-click the component header -> "Print blendshapes". The output is the blendshape mapping sheet.
public class BlendShapeLister : MonoBehaviour
{
    [SerializeField] SkinnedMeshRenderer[] faces;

    void Reset() => faces = GetComponentsInChildren<SkinnedMeshRenderer>();

    [ContextMenu("Print blendshapes")]
    void Print()
    {
        foreach (var face in faces)
        {
            if (face == null || face.sharedMesh == null) continue;
            var mesh = face.sharedMesh;
            var sb = new System.Text.StringBuilder($"{face.name}: {mesh.blendShapeCount} blendshapes\n");
            for (int i = 0; i < mesh.blendShapeCount; i++)
                sb.AppendLine($"{i}: {mesh.GetBlendShapeName(i)}");
            Debug.Log(sb.ToString(), face);
        }
    }
}
