# Endless Runner — Project Handoff

## Active scene decision — 2026-11-05

The owner selected `Assets/ActiveMotion3D/Scenes/AM3D_MainMenu.unity` and
`Assets/ActiveMotion3D/Scenes/AM3D_Morioh_Full.unity` for all work going forward.
This supersedes older gameplay-scene references below. The menu already routes
to Morioh Full, and both scenes are enabled in Build Settings.

Sheer Heart Attack is an editable prefab under
`Assets/ActiveMotion3D/Art/Obstacles/SheerHeartAttack/`. Morioh Full's obstacle
spawner enables it. It targets the player's lane at spawn, approaches 4 units/s
faster than the track, and can be jumped or dodged. Impact detonates it through
the existing player collision/shield rules. Its encounter drains prior hazards
and reserves the track until it passes. Timing and prefab assignment are exposed
on the spawner. Other scenes leave its spawn toggle disabled.

Validation: the full runtime compiled with the installed Unity Roslyn compiler.
Unity 6000.4.7f1 batch component checks passed for the prefab, targeting and lane
commitment, encounter reservation, shield, detonation, restart/reuse, pause, and
fatal collision. These invoke component logic in an isolated preview scene;
live keyboard/webcam play and a standalone build were not checked. The rendered
model and check results are in `Docs/SheerHeartAttack/`.

Explosion visibility fix: gameplay still ends immediately on impact, while the
results panel waits for Sheer Heart Attack's `Impact Presentation Seconds`
(default 0.85 s). Ordinary obstacle results remain immediate. Impact particles
use unscaled time and a soft additive shader. The batch component checks also
cover particle emission after fatal collision, delayed results, restart during
the delay, and ordinary collision timing; `impact.png` shows the rendered burst.

**Prepared:** 2026-11-02  
**Purpose:** Preserve the project state, release-path decision, and verification limits without mistaking local work in progress for a clean release.

## Project at a glance

This is a Unity 6 student project for a three-lane endless runner. The intended experience combines keyboard play and webcam body/gesture input through MediaPipe, a cyberpunk/Jojoverse-inspired presentation, character selection, and the Active Motion 3D activities.

The repository contains **two parallel runtime implementations**:

1. `Assets/Scripts/` and `Assets/Scenes/MainScene.unity` — the original `EndlessRunner` implementation.
2. `Assets/ActiveMotion3D/Scripts/` and `Assets/ActiveMotion3D/Scenes/` — an isolated integration using the `ActiveMotion3D.EndlessRunner` namespaces and its own scenes.

The owner has chosen the **ActiveMotion3D implementation** as the game. Its canonical opening scene is `Assets/ActiveMotion3D/Scenes/AM3D_MainMenu.unity`; `AM3D_ActiveMotionBase.unity` is the gameplay scene. The original runtime and `AM3D_MainMenu_F.unity` remain in the workspace but are not selected for the game. Keep their paths intact unless the owner explicitly requests a change. Changes to one script tree do not automatically affect the other. The gameplay Quit button now has an ActiveMotion3D-local script.

## Decision and verification update — 2026-11-02

- `ProjectSettings/EditorBuildSettings.asset` already enables `AM3D_MainMenu` first and `AM3D_ActiveMotionBase` second. The ActiveMotion3D game-over code returns to `AM3D_MainMenu`. No scene-routing edit was needed for this decision.
- Unity **6000.4.7f1** opened and rendered both menu scenes. The configured menu displays the newer illustrated layout; `_F` displays a smaller text-based card. The configured menu's four serialized model/avatar entries match its display-name order: Big Vegas, David, TungTung, Ty. `_F` has four model/avatar entries and no serialized display names.
- In Editor Play Mode with Keyboard selected, Start loaded `AM3D_ActiveMotionBase`; **A** visibly moved the runner left; an obstacle collision showed Game Over; Play Again restarted the run; Return to Menu loaded `AM3D_MainMenu`; the gameplay Quit button stopped Editor Play Mode. The Play Mode Console showed zero errors and zero warnings during these checks. Right movement, jump, duck, coins, Active Motion activities, webcam input, and a standalone build were not separately checked in this session.
- The owner reports that **all features are working well** and does not want further feature checks. Keep that report distinct from the specific Editor observations above.
- Git status was unchanged after the Editor session. All pre-existing uncommitted and untracked work remains in place.

## Current state

This brief consolidates the root README, ActiveMotion3D integration notes, visual checklist, current working tree, and the owner decision above. The last committed revision is `c258bba` (`main`, tracking `origin/main`). The working directory has substantial **uncommitted and untracked changes**. The table below describes source presence; the specific Unity observations are recorded in the update above. No standalone build or webcam check was run in this session.

| Area | What is present | Confidence / open point |
|---|---|---|
| Runner loop | The selected ActiveMotion3D tree includes player, track, obstacles, coins, score, HUD, and game-over scripts; the original tree has a separate loop | The limited Editor flow above was observed; the owner reports all features working. |
| Input | Keyboard and MediaPipe pose input paths are present | Left movement was observed in the Editor; the owner reports the other inputs working. |
| Characters | Character FBX files and animation controller are under `Assets/Art/Character/`; selectors exist in both code trees | The configured menu's serialized model, avatar, and name arrays align; the owner reports character selection working. |
| Character abilities | `Assets/Scripts/Player/PlayerPowerups.cs` defines David shield, Ty coin magnet, Big Vegas 2× score and Tung Tung smash | This script is in the original tree; do not assume it is wired into the ActiveMotion3D scenes |
| Active Motion activities | Integration code includes punch targets and Human Tetris walls; `Assets/ActiveMotion3D/README.md` records their intended behavior | The owner reports these features working; they were not separately observed in this session. |
| Menu / art | ActiveMotion3D UI art and fonts are present locally; two menu scene candidates currently exist | Owner selected `AM3D_MainMenu`; Build Settings already use it. `_F` remains an unused alternate. |
| Audio | SFX scripts and `Assets/ActiveMotion3D/Audio/` exist | The owner reports features working; audio was not separately inspected in this session. |
| Visual polish | `NewDesign/` has references; `Assets/ActiveMotion3D/Art/UI/` and `Art/FontEX/` contain newer assets | The old wishlist is not an accurate inventory; inspect current scenes before creating/replacing art |

## Important current local changes

These files and folders are already in the workspace and must be carried into the new project if the goal is to continue this exact work. They have not been committed.

- Both `Assets/Scripts/Player/CharacterSelector.cs` and `Assets/ActiveMotion3D/Scripts/Player/CharacterSelector.cs` now support optional display names, falling back to the model name.
- `Assets/Scripts/UI/ExitGameButton.cs` is a new original-tree UI button helper.
- `Assets/ActiveMotion3D/Scenes/AM3D_MainMenu.unity` and `AM3D_ActiveMotionBase.unity` have local scene edits.
- `Assets/ActiveMotion3D/AM3D_MainMenu_F.unity` is an untracked alternate menu scene. The owner selected `Assets/ActiveMotion3D/Scenes/AM3D_MainMenu.unity` as canonical. `_F` is not in Build Settings; retain it untouched unless the owner asks otherwise.
- New UI artwork is in `Assets/ActiveMotion3D/Art/UI/`; added fonts are in `Assets/ActiveMotion3D/Art/FontEX/`.
- Package manifests now include the built-in `com.unity.2d.sprite` package. `EndlessRunner.slnx` also includes the MediaPipe EditMode project.
- `ProjectSettings/ProjectSettings.asset` appears as an enormous replacement in Git because the committed revision is an LFS pointer while the local file is expanded Unity YAML. Check Git LFS setup and compare the intended Unity settings before staging this file; do not treat the whole-file diff as an intentional project-settings change.
- Other untracked local items include `Assets/_Recovery/0 (1).unity`, `ProjectSettings/ProjectAuditorSettings.asset`, `Assets/TextMesh Pro.meta`, `NewDesign/`, and `GameExec_v2.2/` / `GameExec_v3/`. Inspect before moving or committing. The `GameExec_*` folders are local build outputs, not the Unity source project.

Before editing, inspect `git status --short --branch` and preserve the current local changes. The working copy is not a clean checkout.

## Repository map

- `README.md` — current project overview for the selected ActiveMotion3D path.
- `Assets/Scripts/` — original runtime scripts.
- `Assets/Scenes/MainScene.unity` — original scene.
- `Assets/ActiveMotion3D/Scripts/` — separate Active Motion runtime implementation.
- `Assets/ActiveMotion3D/Scenes/AM3D_MainMenu.unity` — current integration menu scene path used by the build installer.
- `Assets/ActiveMotion3D/Scenes/AM3D_ActiveMotionBase.unity` — integration gameplay scene.
- `Assets/ActiveMotion3D/AM3D_MainMenu_F.unity` — untracked, unused alternate menu.
- `Assets/ActiveMotion3D/Editor/AM3DBuildSettingsInstaller.cs` — menu command **Tools → Active Motion 3D → Add Integration Scenes to Build**. It appends the two integration scenes without removing existing build scenes. Its `ValidateIntegration()` helper checks the two configured scenes for missing MonoBehaviours, but has not been run as part of this handoff.
- `Assets/ActiveMotion3D/README.md` — integration notes, including the manual punch-target, Human Tetris, restart, and menu-return checklist.
- `NewDesign/Visual_Polish_Checklist.txt` — older visual wishlist; audit against the current art and scenes before treating its items as missing.
- `NewDesign/` — visual and character design references.

The project version file specifies **Unity 6000.4.7f1**. Keep `.meta` files paired with assets when transferring or moving Unity files so GUID references remain valid. The current checkout has character FBX assets and a Demo City asset folder; confirm applicable licenses before redistribution.

## Remaining release preparation

### 1. Use the chosen release path

- Use the ActiveMotion3D runtime and `AM3D_MainMenu` as the opening scene. The current scene routing and Build Settings already match this choice.
- Confirm the delivery format and target platform before producing a release artifact. Unity Hub lists this project as Windows 64-bit, but the owner has not explicitly chosen a delivery format.

### 2. Preserve and reconcile the current Unity work

- Review the modified integration scenes, alternate menu, and recovery scene individually before staging, moving, or discarding anything.
- Keep `.meta` files with their assets. The ActiveMotion3D gameplay Quit button now references its own script and `.meta` GUID.
- The configured menu's model, avatar, and display-name arrays align in the serialized scene. The gameplay selector has four model and avatar entries; its behavior was observed in the Editor only as part of the limited flow above.

### 3. Feature checks, only if requested later

- The owner reports all features working and explicitly declined further feature checks. Do not present the unchecked features as independently verified in this handoff.
- If the owner later requests targeted testing, use the manual activity checklist in `Assets/ActiveMotion3D/README.md` and report Editor, webcam, and build observations separately.

### 4. Finish visual and UX work

- Audit current art first. Existing assets include `SBR_title*.png`, `LOGO_v2.png`, character/mode/exit button artwork, background art and multiple display fonts under `Assets/ActiveMotion3D/Art/`.
- Decide which design references in `NewDesign/` remain authoritative and use a consistent font/color treatment across menu, HUD and game-over screens.
- Review remaining wishlist items in `NewDesign/Visual_Polish_Checklist.txt` (coin icon/model, obstacle/road art, power-up icons, button states, loading screen and app icon) and mark each as **needed**, **already present**, or **out of scope**. The checklist currently says all listed art is missing, which is no longer reliable.
- Check text legibility, safe areas, aspect ratios, and visual feedback for pose tracking and power-ups.

### 5. Stabilize delivery and project docs

- Resolve Git LFS availability and review the large `ProjectSettings.asset` difference before staging. Review all scene, package, project-setting and recovery-file changes individually.
- Keep build outputs separate from source commits unless they are explicitly required deliverables.
- Build for the agreed platform, record Unity version/build settings and any required webcam setup, then update `README.md` and this handoff with verified results.
- Confirm licenses/redistribution rights for third-party city, character, font, audio and image assets before sharing a build or complete project.

## Suggested first session for a future collaborator

1. Add this file and the **entire Unity project folder**, including uncommitted assets and `.meta` files, to the new project context. This document alone is not enough to reconstruct scenes or imported artwork.
2. Read `README.md` and `Assets/ActiveMotion3D/README.md`; then inspect the working tree. Treat `NewDesign/Visual_Polish_Checklist.txt` as an older wishlist, not an inventory of missing art.
3. Do not reset, clean, or overwrite the workspace. First preserve the current state and inspect any desired Git LFS setup.
4. The runtime and canonical menu are already decided: ActiveMotion3D with `AM3D_MainMenu`. Ask about target platform or delivery format only when preparing a release artifact.
5. Preserve the owner's report that features work separately from the Editor observations in this document; do not repeat feature checks unless requested.

## Copyable kickoff message

> Continue this Unity project using `PROJECT_HANDOFF.md` as the current-state brief. Preserve the existing uncommitted work. The intended game is the ActiveMotion3D runtime with `AM3D_MainMenu` as its opening scene. Keep the original runtime and `_F` alternate intact. Clearly distinguish source-code presence, the owner's feature report, and behavior observed in Unity.
