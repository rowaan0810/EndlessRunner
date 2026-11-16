# Steel Ball Run

Steel Ball Run is a three-lane endless runner set in Morioh. Choose D4C or Killer Queen, dodge obstacles, and collect coins as the pace picks up. Play with a keyboard or use a webcam with MediaPipe pose tracking.

## Open the project

1. Install Git LFS before cloning so Git can download the Unity assets. For an existing clone, run `git lfs pull` from the project root.
2. Open the project in Unity **6000.4.7f1** and let Unity finish importing.
3. Open [AM3D_MainMenu.unity](Assets/ActiveMotion3D/Scenes/AM3D_MainMenu.unity) and enter Play Mode.
4. Choose a character and input mode, then start the run. For webcam mode, stand centered in the camera frame during calibration.

The build starts in `AM3D_MainMenu`, which loads [AM3D_Morioh_Full.unity](Assets/ActiveMotion3D/Scenes/AM3D_Morioh_Full.unity) for gameplay.

## Controls

| Action | Keyboard | Webcam |
| --- | --- | --- |
| Change lanes | A / D or Left / Right | Lean left or right |
| Jump | W, Up, or Space | Jump |
| Duck | S or Down | Crouch |

- Raise the matching arm when a punch target lights up to earn a coin. Match the pose shown on an approaching Human Tetris wall to pass through.
- Change lanes to dodge Menacing Road Signs. Jump over Sheer Heart Attack or move out of its lane before it reaches you.
- Press **Esc** or select **Pause** to open the pause menu. From there you can resume, restart, return to the main menu, or quit.
- The results screen shows your score, distance, coins, and time. Select **Play Again** to retry or **Main Menu** to change characters or controls.

## Stand Coach on Windows

From the project root, run `.\Tools\Setup-StandCoach.ps1` in PowerShell before entering Play Mode or making a Windows x64 build with Stand Coach. The first setup downloads the language model and voices into `StandCoachRuntime/`. Unity copies that folder into Windows x64 builds automatically. Share the whole build folder so the executable can find its `_Data` folder and the bundled runtime.

On the main menu, click the voice name to switch between the two voices for the selected Stand, and click **Preview voice** to hear it. After a crash, your Stand speaks an AI-generated run assessment, cause of the crash, and next-run tip. The results card has **Replay feedback** and an optional captions toggle. The game runs both the language and speech models locally.

Choose between **Onyx** (default) and **Fenrir** for D4C, or **George** (default) and **Fable** for Killer Queen. See [Stand Coach third-party notices](Tools/StandCoach-ThirdParty.txt) for the bundled components.

### Edit the game-over screen in Unity

1. In the Project window, open `Assets/ActiveMotion3D/Art/UI/Resources/RunResults.prefab` in Prefab Mode.
2. Expand **Results frame**. **Score and actions** contains the headline, score, stats, and navigation buttons. **Stand Coach** contains the portrait, captions, and playback controls.
3. Use **Rect Transform** to move or resize an element, **Text** to edit fonts and labels, and **Image** to edit colors. The root **Run Results View** exposes the portrait background color and the two model references.
4. Save the prefab, then play a run from `AM3D_MainMenu` to see the populated screen. The sample score and Stand name in Prefab Mode are replaced by the actual run data.

## Project files

- [Gameplay scripts](Assets/ActiveMotion3D/Art/Scripts)
- [Morioh background notes](Docs/MoriohDesign/HYBRID_BACKGROUND.md) for the painted sky, drifting clouds, and town skyline.
- [Menacing Road Signs](Docs/MenacingRoadSigns/README.md) for obstacle variants, tuning, and targeted Editor checks.
- [Steel Ball Run Morioh showcase](deck_output/Steel-Ball-Run-Morioh-Showcase.pptx)
