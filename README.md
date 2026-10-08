# EndlessRunner — Cyberpunk Pose-Controlled Runner

A simplified endless runner inspired by Subway Surfers, built in Unity 6 as a TEEP project.
Control the game with your **body via webcam** using MediaPipe pose detection, or use the keyboard.

## Features
- **3-Lane Running**: Dodge obstacles by switching lanes, jumping, and ducking
- **Webcam Hand Gestures**: Use your body to play — lean to switch lanes, raise your right index finger to jump, and open your palm to duck.
- **Easy Mode**: Merged with webcam mode for seamless play without exaggerated movements.
- **Cyberpunk Environment**: Procedurally generated neon city with fog and night lighting
- **Multiple Characters**: Support for multiple 3D characters (David, Ty, Big Vegas) swappable from the Main Menu.
- **Keyboard Controls**: Full arrow key / WASD support

## Controls (Keyboard)
| Key | Action |
|-----|--------|
| ←/A | Move left lane |
| →/D | Move right lane |
| ↑/W/Space | Jump |
| ↓/S | Duck/Roll |
| R | Restart (after game over) |
| Escape | Quit |

## Quick Start
1. Open the project in **Unity 6** (6000.4.7f1 or later)
2. Import the required third-party assets (see below)
3. Open `Assets/Scenes/MainScene` (or any empty scene)
4. Create an empty GameObject and add the `SceneSetup` component
5. Press Play — the scene auto-builds and the game starts

## Required Third-Party Assets (not included in repo)

These assets are free but cannot be redistributed. Download and import them manually:

| Asset | Source | Instructions |
|-------|--------|--------------|
| **Demo City by Versatile Studio** | [Unity Asset Store](https://assetstore.unity.com/packages/3d/environments/demo-city-by-versatile-studio-mobile-friendly-269772) (Free) | Add to My Assets → Package Manager → Import |
| **Characters (David, Ty, Big Vegas)** | [Mixamo](https://www.mixamo.com/) (Free) | Download characters + Running, Jumping, Stand To Roll animations as FBX. Extract Textures & Materials, then place in `Assets/Art/Character/` |

After importing the Mixamo assets and extracting their textures, run **Tools → Endless Runner → Setup 3D Character** from the Unity menu bar.

## Project Structure
```
Assets/
├── Scripts/
│   ├── Core/              # GameManager, GameSpeed
│   ├── Input/             # IGameInput, KeyboardInput, PoseInput, EasyModeInput, InputManager
│   │   └── PoseDetection/ # PoseController, MediaPipeManager
│   ├── Player/            # PlayerController, PlayerCollision, PlayerAnimator
│   ├── Track/             # TrackManager, TrackSegment
│   ├── Obstacles/         # ObstacleSpawner, Obstacle
│   ├── Collectibles/      # Coin
│   ├── UI/                # GameHUD, MainMenu, GameOverScreen
│   └── SceneSetup.cs      # Auto-creates scene hierarchy
├── Editor/                # SetupCharacterEditor (Mixamo automation)
├── Art/Character/          # Mixamo FBX files + Animator Controller (gitignored)
├── Scenes/
└── MediaPipeUnity/         # MediaPipe Unity Plugin
```

## Tech Stack
- **Engine**: Unity 6 (URP)
- **Pose Detection**: MediaPipe Unity Plugin
- **Character**: Mixamo (David)
- **Environment**: Demo City by Versatile Studio
- **Language**: C#

## Development Timeline
- **Week 1**: Core runner mechanics, keyboard input, 3-lane movement
- **Week 2**: Obstacles, coins, scoring, UI, full game loop
- **Week 3**: MediaPipe pose control, Easy Mode, input refinement
- **Design Phase**: 3D character integration, Cyberpunk environment, visual polish
