# Exact change inventory

All paths below are relative to the Unity project root. Every new asset under `Assets/` has paired `.meta` metadata. Existing source packs and scene files are unchanged at delivery time.

## Existing files edited

| File | Purpose |
|---|---|
| `Assets/ActiveMotion3D/Art/Config/MoriohThemeConfig.cs` | Optional sky, horizon, punch visual and activity-art references/placement controls |
| `Assets/Editor/MoriohFullBuilder.cs` | Apply those optional references and sky/horizon only to the Full scene |
| `Assets/ActiveMotion3D/Art/Scripts/Obstacles/ObstacleSpawner.cs` | Optional punch visual prefab and activity style slots; pass art to pose walls and coins. No scheduling, lane, pool, collision or dimension changes |
| `Assets/ActiveMotion3D/Art/Scripts/Obstacles/HumanTetrisWall.cs` | Style the existing diagrams/frame and add waiting/ready text; recognition and trigger code untouched |
| `Assets/ActiveMotion3D/Art/Scripts/Collectibles/PunchTarget.cs` | Delegate color/label/hit presentation to an optional art adapter; retain generated-cube fallback |
| `Assets/ActiveMotion3D/Art/Scripts/Collectibles/Coin.cs` | Optional pickup effect, separate from scoring and spawn enablement |

## New code and documentation

| File | Purpose |
|---|---|
| `Assets/Editor/MoriohDesignKitBuilder.cs` | Explicit create/apply/rebuild commands, scene preflight and recovery copies |
| `Assets/ActiveMotion3D/Art/Scripts/Environment/MoriohActivityArt.cs` | Inspector activity art asset |
| `Assets/ActiveMotion3D/Art/Scripts/Environment/MoriohModuleLayout.cs` | Inspector module placement assets |
| `Assets/ActiveMotion3D/Art/Scripts/Environment/MoriohPunchVisual.cs` | Visual target state adapter with exact renderer/material-slot bindings |
| `Assets/ActiveMotion3D/Art/Scripts/Environment/MoriohBurstEffect.cs` | Configurable short visual effect lifetime, drift and expansion |
| `Assets/ActiveMotion3D/Art/Scripts/Environment/MoriohHorizonFollower.cs` | Keep the continuous horizon at the camera's horizontal origin |
| `Assets/ActiveMotion3D/Art/Environment/Morioh/DesignKit/LavenderCloudSky.shader` | Built-in gradient sky with original cloud panorama |
| `Assets/ActiveMotion3D/Art/Environment/Morioh/DesignKit/HorizonInk.shader` | Built-in two-sided unlit distance silhouettes |
| `Tools/Morioh/generate_design_kit.py` | Original geometry, palette and texture authoring source |
| `Tools/Morioh/preview_design_kit.py` | Depth-buffered source geometry preview |
| `Tools/Morioh/.gitignore` | Exclude Python bytecode |
| `Docs/MoriohDesign/INSTALL.md` | Installation, Inspector editing, validation limits and recovery |
| `Docs/MoriohDesign/FILES.md` | This inventory |
| `Docs/MoriohDesign/SourceArtPreview.png` | Eight-building contact sheet of actual OBJ geometry |

## New source assets

Folder: `Assets/ActiveMotion3D/Art/Environment/Morioh/DesignKit/Source/Models/`.

Each name in this table is an exact `.obj` filename stem:

| Category | Models |
|---|---|
| Buildings / landmark | `HippedRoofResidence`, `SplitLevelResidence`, `NijiConvenience`, `SumireCafe`, `HoshiBakery`, `YunagiRadio`, `MoriohGeneralStore`, `BooksAndRecords`, `StationKiosk`, `ClockLandmark` |
| Vegetation / roadside | `PurpleFlowerTree`, `NeighborhoodBicycle`, `VendingMachine`, `FlowerPlanter`, `PostBox`, `UtilityPole`, `StreetLamp`, `GuardRail`, `RouteMarker`, `NeighborhoodBench` |
| Street / distance | `StreetBase20m`, `OverheadWireSpan20m`, `ZebraCrossing`, `SideStreetApron`, `ClockPlazaPaving`, `ContinuousHorizon` |
| Obstacles / activities | `JumpPromenadeRail`, `DuckShopCanopy`, `DodgeDeliveryScreen`, `MoriohCrestCoin`, `HighSideCrest`, `PoseGateFrame` |

Also in `Source/Models/`: `Palette.mtl`.

In `Source/Textures/`: `NeighborhoodSigns.png` (2048×1024, 16 original signs), `LavenderCloudPanorama.png` (2048×1024, alpha), `SidewalkPavers.png` (256×256).

In `Source/`: `manifest.json` with exact model triangle counts, material palette and sign text.

## Files created by the manual Unity commands

All generated art goes under `Assets/ActiveMotion3D/Art/Environment/Morioh/DesignKit/Generated/`:

- `Models/<each of the 32 OBJ stems>.prefab`.
- `Materials/Ink.mat`, `Cream.mat`, `Mint.mat`, `Peach.mat`, `Lilac.mat`, `Roof.mat`, `Glass.mat`, `Wood.mat`, `Gold.mat`, `Leaf.mat`, `Blossom.mat`, `Road.mat`, `Paving.mat`, `White.mat`, `Red.mat`, `Metal.mat`, `FarHill.mat`, `NearHill.mat`, `Town.mat`, `DistantGround.mat`, `Sign.mat`, `PoseReady.mat`, `ComicParticles.mat`, `LavenderCloudSky.mat`.
- `Layouts/ResidentialLane.asset`, `ShoppingCorner.asset`, `CafeAndBakery.asset`, `ClockPlaza.asset`.
- `Modules/Morioh_Street_20m_ResidentialLane.prefab`, `Morioh_Street_20m_ShoppingCorner.prefab`, `Morioh_Street_20m_CafeAndBakery.prefab`, `Morioh_Street_20m_ClockPlaza.prefab`.
- `Activities/MoriohActivityArt.asset`, `Morioh_HighSideTarget_Visual.prefab`, `PunchHit.prefab`, `CoinPickup.prefab`.
- `Morioh_Horizon.prefab`, `MoriohDesignTheme.asset`.

The installer edits only `Assets/ActiveMotion3D/Art/Config/MoriohThemeConfig.asset` and `Assets/ActiveMotion3D/Scenes/AM3D_Morioh_Full.unity`, after backing both up to `Assets/_Recovery/MoriohDesign_<timestamp>/`. Creating/importing generated assets also writes their metadata and source import settings. The module-rebuild command edits the four generated module prefabs. No automatic scene application happens on import.
