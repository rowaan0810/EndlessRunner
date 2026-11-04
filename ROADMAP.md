# ActiveMotion3D game roadmap

The game uses only `Assets/ActiveMotion3D/Scenes/AM3D_MainMenu.unity` and
`Assets/ActiveMotion3D/Scenes/AM3D_ActiveMotionBase.unity`. Character abilities
(shield, magnet, 2× score, smash) are out of scope. The original runner and
`AM3D_MainMenu_F` remain in the workspace but are not part of this roadmap.

## 1. Isolate the selected game — done

- Give the gameplay Quit button its own `ActiveMotion3D.EndlessRunner.UI.ExitGameButton`
  script and scene reference. The original runtime is left intact.

## 2. Complete controls and game flow — next

- Add pause/resume from keyboard and an on-screen button. Freeze the runner,
  track, spawns, score/time, and activity timers together; resume without a
  speed or countdown jump.
- Add a pause menu with Resume, Restart, Settings, Return to Main Menu, and Quit.
  Make restart and menu transitions restore normal time and release the webcam.
- Add settings for master/SFX volume and mute, saved between launches.
- Add webcam controls: show camera/tracking status and actionable errors; allow
  choosing an available camera and recalibrating without restarting the app.
  Reuse the existing calibration flow and show when it is ready.
- Add a concise controls/help panel for keyboard and webcam gestures, including
  punch targets and Human Tetris poses. It should be reachable before play and
  from pause.
- Check focus loss, camera disconnection, and failure to detect a person so
  play does not continue silently with unusable controls. Provide a clear way
  to retry, switch input mode, or return to the menu.
- Keep UI navigation usable by mouse and keyboard; review labels and feedback
  for both input modes.

## 3. Visual and UX pass — next main task after section 2

- Audit the actual menu, gameplay, HUD, and game-over scenes against current
  artwork. The old `NewDesign/Visual_Polish_Checklist.txt` is a wishlist, not a
  reliable inventory of missing assets.
- Establish consistent fonts, colors, button states, and layout across both
  scenes. Check readability and common aspect ratios.
- Finish HUD and feedback art: coin indicator, speed/score presentation,
  tracking and calibration cues, punch/pose success and failure, and the new
  pause/settings/help screens.
- Review road, coin, obstacle, and background models/textures/effects for visual
  consistency and performance. Add an app icon and loading treatment if needed.
- Exclude character ability icons and effects.

## 4. Release preparation — last

- Reconcile the existing uncommitted scenes, assets, `.meta` files, packages,
  recovery items, and Git LFS project-settings change before staging a release.
- Confirm the target platform and delivery format, then create a standalone
  build using the two selected scenes.
- Verify keyboard and webcam flows in the build, record webcam setup and known
  limits, and update the project documentation with observed results.
- Review redistribution rights for included third-party assets before sharing.
