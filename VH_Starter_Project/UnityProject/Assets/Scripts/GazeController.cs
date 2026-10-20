using UnityEngine;
using UnityEngine.Animations.Rigging;

// Body ▸ Gaze: look at the user, with small random saccades of the eyes only.
// The IK is not in this script: it is an Animation Rigging setup you build in the Inspector.
//   character root:  Animator + Rig Builder (+ this component)
//   └─ GazeRig (Rig)
//      ├─ HeadTarget, EyeTarget             two empty objects: what the constraints aim at
//      ├─ Aim Chest / Aim Neck / Aim Head   Multi-Aim Constraints → HeadTarget (weights 0.15 / 0.3 / 0.6)
//      └─ Aim Eye L / Aim Eye R             Multi-Aim Constraints → EyeTarget (weight 1)
// This script only moves the two targets: HeadTarget follows the camera smoothly,
// EyeTarget sits on the camera plus a small random offset (the saccade), so the eyes jump
// and the head stays steady. The rig's weight fades the whole gaze in and out.
public class GazeController : MonoBehaviour
{
    [SerializeField] Transform target;                 // the Main Camera
    [SerializeField] Rig gazeRig;
    [SerializeField] Transform headTarget, eyeTarget;
    [SerializeField, Range(0, 1)] float weight = 1f;   // 0 = no gaze, 1 = full gaze
    [SerializeField] float headFollowTime = 0.3f;      // seconds: how lazily the head follows
    [SerializeField] Vector2 saccadeInterval = new Vector2(1.5f, 4f);
    [SerializeField] float saccadeSize = 0.05f;        // metres around the target
    Vector3 offset, headVelocity;
    float nextSaccade;

    void Start()
    {
        if (target != null && headTarget != null) headTarget.position = target.position;
    }

    void Update()
    {
        if (target == null || headTarget == null || eyeTarget == null) return;

        if (Time.time >= nextSaccade)                  // saccade: a quick glance, no smoothing
        {
            offset = Random.insideUnitSphere * saccadeSize;
            nextSaccade = Time.time + Random.Range(saccadeInterval.x, saccadeInterval.y);
        }

        headTarget.position = Vector3.SmoothDamp(headTarget.position, target.position, ref headVelocity, headFollowTime);
        eyeTarget.position = target.position + offset;

        if (gazeRig != null) gazeRig.weight = Mathf.MoveTowards(gazeRig.weight, weight, 2f * Time.deltaTime);
    }

    // Look at the user (1) or let the clips drive head and eyes (0), fading in half a second.
    public void SetWeight(float w) => weight = Mathf.Clamp01(w);
}
