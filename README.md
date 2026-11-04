# Active Motion 3D — Endless Runner

This Unity 6 project is a three-lane endless runner with keyboard and webcam pose input, character selection, punch targets, and Human Tetris walls. The intended game uses the **ActiveMotion3D runtime** in `Assets/ActiveMotion3D/Scripts/`.

## Open the game

1. Open the project in Unity **6000.4.7f1**.
2. Open `Assets/ActiveMotion3D/Scenes/AM3D_MainMenu.unity`.
3. Enter Play Mode and select an input mode and character, then select **Start Game**.

The configured build scenes are `AM3D_MainMenu` first and `AM3D_ActiveMotionBase` second. The gameplay scene returns to `AM3D_MainMenu`. The alternate `Assets/ActiveMotion3D/AM3D_MainMenu_F.unity` is retained in the workspace but is not the selected menu or a build scene.

## Play

- Keyboard: **A/Left** and **D/Right** change lanes; **W/Up/Space** jumps; **S/Down** ducks.
- Webcam mode uses MediaPipe pose input. Follow the guidance shown in the game for movement, jump, duck, punch targets, and Human Tetris poses.
- The menu switches input mode and character. Gameplay includes score, coins, obstacles, game over, restart, and return to menu.

The project owner reports that all features are working and does not need another feature check. In a separate Unity 6000.4.7f1 Editor session, we observed keyboard menu-to-gameplay routing, left-lane movement, obstacle game over, Play Again, Return to Menu, and the gameplay Quit button stopping Play Mode. Those observations are **Editor checks**, not evidence of webcam behavior or a standalone build. See `PROJECT_HANDOFF.md` for the working-tree state and verification details.

## Project paths

- `Assets/ActiveMotion3D/Scenes/` and `Assets/ActiveMotion3D/Scripts/` — intended game scenes and runtime.
- `Assets/ActiveMotion3D/README.md` — integration notes and activity descriptions.
- `Assets/Scripts/` and `Assets/Scenes/MainScene.unity` — original runner implementation, retained but not selected for the game. Its runtime is separate from the selected ActiveMotion3D game.

See `ROADMAP.md` for the agreed order of controls, visual work, and release preparation. Character abilities are out of scope.

The working tree contains uncommitted source, scene, art, and project-setting changes. Preserve each asset with its `.meta` file, and review `ProjectSettings/ProjectSettings.asset` with Git LFS before staging it. Local `GameExec_*` folders are build outputs, not the Unity source project. Check third-party asset redistribution rights before sharing a full project or build.
