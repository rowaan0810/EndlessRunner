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
| Jump | W, Up, or Space | Point your index finger up or jump |
| Duck | S or Down | Point your index finger down or crouch |
| Pause | Esc | Show both open palms at the same time |
| Resume | Esc | Make a V sign with either hand on the pause screen |
| Quit | Select **Quit** on the pause screen | Hold a thumbs-down sign with either hand for about ¾ second during a run or while paused |

- When a punch target lights up, raise the matching arm or press **Q** for left and **E** for right to earn a coin. For Human Tetris walls, hold the displayed pose or number key until you pass through.
- Change lanes to dodge Menacing Road Signs. Jump over Sheer Heart Attack or move out of its lane before it reaches you.
- Press **Esc** or select **Pause** to open the pause menu. From there you can resume, restart, return to the main menu, or quit.
- The results screen shows your score, distance, coins, and time. Select **Play Again** to retry or **Main Menu** to change characters or controls.

In the main menu's **Settings** or the Morioh pause menu, set **Run speed** to **Auto** for a pace that increases during the run, or choose a fixed speed from 5 to 50 m/s. The game remembers your selection.

## Stand Coach on Windows

From the project root, run `.\Tools\Setup-StandCoach.ps1` in PowerShell before entering Play Mode or making a Windows x64 build with Stand Coach. The first setup downloads the language model and voices into `StandCoachRuntime/`. Unity copies that folder into Windows x64 builds automatically. Share the whole build folder so the executable can find its `_Data` folder and the bundled runtime.

After a crash, your Stand gives four lines of feedback: a rotating opening, the run result, the cause of the crash, and a practice tip. The local language model picks a tip based on the run. The results card has **Replay feedback** and a captions toggle. If the opening audio takes more than 5.5 seconds, the first line appears as a caption.

D4C speaks with **Onyx** and Killer Queen speaks with **George**. See [Stand Coach third-party notices](Tools/StandCoach-ThirdParty.txt) for the bundled components.

### Edit the game-over screen in Unity

1. In the Project window, open `Assets/ActiveMotion3D/Art/UI/Resources/RunResults.prefab` in Prefab Mode.
2. Expand **Results frame**. **Score and actions** contains the headline, score, stats, and navigation buttons. **Stand Coach** contains the portrait, captions, and playback controls.
3. Use **Rect Transform** to move or resize an element, **Text** to edit fonts and labels, and **Image** to edit colors. The root **Run Results View** exposes the portrait background color and the two model references.
4. Save the prefab, then play a run from `AM3D_MainMenu` to see the populated screen. The sample score and Stand name in Prefab Mode are replaced by the actual run data.

## Project files

- [Gameplay scripts](Assets/ActiveMotion3D/Art/Scripts)
- [Morioh background notes](Docs/MoriohDesign/HYBRID_BACKGROUND.md) for the painted sky, drifting clouds, and town skyline.
- [Menacing Road Signs](Docs/MenacingRoadSigns/README.md) for obstacle variants, tuning, and targeted Editor checks.
- [November 20 demo release checklist](Docs/Release-2026-11-20.md)
- [Browser showcase](deck_output/Steel-Ball-Run-Showcase.html) and [speaker guide](deck_output/Steel-Ball-Run-Speaker-Guide.md). Download the HTML file and open it in a browser.
- [Steel Ball Run Morioh showcase PowerPoint](deck_output/Steel-Ball-Run-Morioh-Showcase.pptx)
