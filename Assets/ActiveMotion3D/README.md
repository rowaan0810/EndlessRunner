# Active Motion 3D integration

This folder is isolated from the original EndlessRunner assets. It adds two scenes without replacing the student's existing scenes or scripts.

## Setup

1. Open the Unity project and wait for compilation to finish.
2. Run `Tools > Active Motion 3D > Add Integration Scenes to Build`.
3. Open `Assets/ActiveMotion3D/Scenes/AM3D_MainMenu.unity`.
4. Enter Play Mode and select **Start Game**.

Added scenes:

- `AM3D_MainMenu`
- `AM3D_ActiveMotionBase`

## 2026-10-21 update

Ported source commits 69b4e88 and d9c1e43 (October 19–20).

- Left/right overhead punch targets: raise the corresponding arm when yellow; lower and raise again for the next target. Each hit adds one coin (10 total-score points), particles and impact audio.
- Human Tetris: six arm poses, wall silhouette feedback and recognition grace window. First wall after 8 seconds, then every 12–18 seconds. Match the indicated pose to survive; this feature does not itself award coins.
- Wider track (lane width 3.2), camera adjustment, horizontal sensitivity 5.6, and webcam pose guidance.
- Existing integration speed settings (15 / 0.5 / 50), scene names, GUIDs, menu navigation and student assets are preserved. No global scripts, packages or project settings were changed.
- SceneSetup and ActiveMotionSceneAuthoring changes are editor/bootstrap helpers absent from this integration; their camera and track changes are included in the migrated scene and runtime track code. They were not added because they target source-project paths.

### Manual checks

Open AM3D_MainMenu, enter Play Mode and Start Game. Check left/right body movement, jump and duck; strike both yellow targets and confirm +1 coin / +10 points with particles and sound; hold an arm up and confirm it does not repeatedly collect targets. Match and deliberately miss Human Tetris walls. Test restart and return to menu. Finally open the student's original scene and confirm its behavior is unchanged.
