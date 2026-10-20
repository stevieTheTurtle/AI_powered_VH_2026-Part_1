using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Body ▸ Face: idle life. Random blink every 2-6 s, ~60 ms close, ~100 ms open.
// Owns the eyelid-closing blendshapes: no other script may write them.
public class BlinkController : MonoBehaviour
{
    // All face meshes: the eyelashes, eye occlusion and tearline must close with the lids.
    [SerializeField] SkinnedMeshRenderer[] faces;
    // Fragments of the blink shape names, taken from your list. Every shape whose name
    // contains a fragment is driven, so one fragment can cover both eyes.
    [SerializeField] string[] blinkShapesContain = { "Blink" };
    [SerializeField] Vector2 interval = new Vector2(2f, 6f);
    [SerializeField] float closeTime = 0.06f;
    [SerializeField] float openTime = 0.10f;

    List<ShapeRef> shapes = new List<ShapeRef>();

    void Start()
    {
        if (!FaceShapes.Any(faces)) { Debug.LogWarning("BlinkController: no face renderer assigned", this); enabled = false; return; }
        foreach (var part in blinkShapesContain)
        {
            var found = FaceShapes.Find(faces, part);
            if (found.Count == 0) Debug.LogWarning($"Blink shape containing '{part}' not found", this);
            shapes.AddRange(found);
        }
        if (shapes.Count == 0) { enabled = false; return; }
        StartCoroutine(BlinkLoop());
    }

    IEnumerator BlinkLoop()
    {
        // ===== LIVE-1 · Body ▸ Face · the idle blink =====
        // Connects the timer to the face: wait, close the lids, open them, forever.
        // Write: a loop that waits a random time in `interval`,
        //        then Animate(0 -> 100) in closeTime and Animate(100 -> 0) in openTime.
        yield break;   // placeholder: replace with the loop
        // ===== end LIVE-1 =====
    }

    // Moves every blink shape from `from` to `to` over `duration` seconds.
    IEnumerator Animate(float from, float to, float duration)
    {
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            SetAll(Mathf.Lerp(from, to, t / duration));
            yield return null;
        }
        SetAll(to);
    }

    void SetAll(float weight)
    {
        foreach (var s in shapes) s.Set(weight);
    }
}
