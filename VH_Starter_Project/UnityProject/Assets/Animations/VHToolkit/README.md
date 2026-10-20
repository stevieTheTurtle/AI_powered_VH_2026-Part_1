# USC Virtual Human Toolkit animations

Twelve body animations from the **USC ICT Virtual Human Toolkit** (VHToolkit): the idle, five co-speech beats for the Speaking state, and the six webinar gestures.

* Source: <https://github.com/USC-ICT/vhtoolkit/tree/main/VHUnityURP-Assets/Assets/Art/Animation> (branch `main`, downloaded 2026-10-15)
* Licence: **USC-RL v3.0**, see [LICENSE_USC-RL.md](LICENSE_USC-RL.md). Free for educational, research and non-profit use; keep this notice and the licence file with the clips. Commercial use needs permission from USC.
* Copyright © University of Southern California. All Rights Reserved.
* Reference: *Re-architecting the Virtual Human Toolkit*, IVA 2022 (`references/vh_foundations/2022-Re-architectingtheVirtualHumanToolkit-IVA.pdf`).

| Slot (MasterclassConfig) | Clip name in Unity | Source file | Folder in the repo |
| :-- | :-- | :-- | :-- |
| idle | `Idle_StandingUpright01` | `ChrGenericFmlAdult@IdleStandingUpright01.fbx` | `ChrGenericFmlAdult/` |
| wave | `Wave_Greeting01` | `ChrGenericFmlAdult@IdleStandingUpright01_Greeting01.fbx` | `ChrGenericFmlAdult/` |
| nod | `Nod_HeadNod01` | `ChrGenericMleAdult@IdleStandingUpright01_HeadNod01.fbx` | `BodyAnimation/` |
| shakeHead | `ShakeHead_NegativeBt01` | `ChrGenericFmlAdult@IdleStandingUpright01_NegativeBt01.fbx` | `ChrGenericFmlAdult/` |
| shrug | `Shrug_Shrug01` | `ChrGenericFmlAdult@IdleStandingUpright01_Shrug01.fbx` | `ChrGenericFmlAdult/` |
| thinking | `Thinking_ThinkAboutIt01` | `ChrGenericMleAdult@IdleStandingUpright01_ThinkAboutIt01.fbx` | `BodyAnimation/` |
| point | `Point_PointRt01` | `ChrGenericFmlAdult@IdleStandingUpright01_PointRt01.fbx` | `ChrGenericFmlAdult/` |
| talkBeats[0] | `Beat1_MidBt01` | `ChrGenericFmlAdult@IdleStandingUpright01_BeatMidBt01.fbx` | `ChrGenericFmlAdult/` |
| talkBeats[1] | `Beat2_LowRt01` | `ChrGenericFmlAdult@IdleStandingUpright01_BeatLowRt01.fbx` | `ChrGenericFmlAdult/` |
| talkBeats[2] | `Beat3_LowBt01` | `ChrGenericFmlAdult@IdleStandingUpright01_BeatLowBt01.fbx` | `ChrGenericFmlAdult/` |
| talkBeats[3] | `Beat4_MidLf01` | `ChrGenericFmlAdult@IdleStandingUpright01_BeatMidLf01.fbx` | `ChrGenericFmlAdult/` |
| talkBeats[4] | `Beat5_LowLf01` | `ChrGenericFmlAdult@IdleStandingUpright01_BeatLowLf01.fbx` | `ChrGenericFmlAdult/` |

**Speaking.** The set has no standing female talk loop; its only talk loops (`ChrGenericMleAdult@IdleStandingUpright01_TalkingHeadLoop01–03`) barely move (talking-head close-ups). So the Speaking state cycles the five beats in order (each → next at 85 %, 0.3 s blend; any → Idle as soon as `Speaking` goes false). Empty `talkBeats` in the config = the handout version: one looping talk clip, `Armature|Idle_Talking_Loop` from the Quaternius Universal Animation Library (CC0) in `../UAL1_Standard.fbx`.

## Changes from the originals

* FBX files unchanged. The `.meta` files are USC's (they hold the Humanoid bone map for the `Jt*` skeleton, which Unity cannot auto-map), with two edits: *Avatar Definition = Create From This Model* (USC's copy from an avatar that is not in this project removed) and the clip renamed to its slot, so `Masterclass → Build Everything` finds it by name.
* The importer logs "Avatar Rig Configuration mis-match" warnings for these files: USC's stored T-pose differs slightly from the file. The avatars are valid and the clips retarget correctly.
* Choices: no clip in the set is a pure head shake. `NegativeBt01` is a "no" with a small head turn and both hands. `ThinkAboutIt01` (2.5 s, hand up) instead of `Think01` (14 s, too long for a gesture). Nod and Think come from the male set (no standing female equivalent); with the upper-body mask the arms briefly take the male idle pose.
