# Morioh environment and activity artwork

Prepared on branch `codex/morioh-design-complete`, after the gesture rollback. The new gesture feature remains removed.

The source models, textures, shaders and authoring commands are present. **The new design has not been assembled or applied in Unity yet.** No scene file was edited during this work. The two menu commands below perform that final installation.

## Install in Unity

1. Exit Play Mode. Save any scene work you want to retain. Let Unity import the new assets and compile scripts; use **Assets → Refresh** if necessary.
2. Run **Tools → Morioh → Design Kit → 1 - Create Editable Art Assets**. This imports the 32 source models, creates Built-in materials, visual prefabs, four layout assets, the activity art asset and `MoriohDesignTheme.asset`. It does not open or save a scene. The selected theme asset exposes the sky, horizon, module, obstacle and activity references in the Inspector.
3. Review the four assets in `Assets/ActiveMotion3D/Art/Environment/Morioh/DesignKit/Generated/Layouts/`. Their placement arrays expose every module instance's prefab, position, rotation and scale. You can also inspect the generated building, prop and obstacle prefabs directly.
4. Run **Tools → Morioh → Design Kit → 2 - Apply To Full Scene**. This checks the existing 20-unit track and 3.2-unit lanes, creates recovery copies of the Full scene and current theme config, then updates the Full scene's visual children and atmosphere. It reports the exact recovery folder in the Console.
5. Open `Assets/ActiveMotion3D/Scenes/AM3D_Morioh_Full.unity` if it is not already open. Inspect the four street layouts, cloud sky, horizon and obstacle assets. No build-settings step is needed.

The builder refuses to overwrite an existing generated kit on a second run. If a build stops partway, inspect the reported error and the `Generated` folder before retrying; it does not silently replace partially authored assets. A missing imported material produces a specific error before the scene installation.

## What repeats, and why

The existing eight `TrackSegment_00` through `TrackSegment_07` roots retain their TrackSegment components, original child objects, colliders and serialized references. The theme installer replaces only the Morioh visual child of each root.

| Segment | Visual module | Distinctive layout |
|---|---|---|
| 00, 04 | ResidentialLane | Narrow hipped-roof and parapet houses, balconies, shutters, rain pipes, air conditioners, bicycles, flowering trees |
| 01, 05 | ShoppingCorner | Low convenience store with a return facade, general store, taller radio shop, side street, crossing, vending machine |
| 02, 06 | CafeAndBakery | Cafe tables, bakery displays, machiya-style bookshop with noren panels and books, planted seating |
| 03, 07 | ClockPlaza | Open clock plaza, circular paving, station kiosk, benches and trees on one side; taller buildings opposite |

Each module is 20 units. The visual sequence repeats every 80 units inside the existing 160-unit pool. TrackManager keeps recycling the same roots; no new spawning or recycling implementation was introduced. The lavender cloud sky and continuous ring of distant hills/town are separate from the segment pool, so there is no horizon boundary at each recycle. A distant ground backdrop sits below the existing track and matches the horizon/fog color to cover the area beyond the pool.

## Inspector editing

| Edit | Asset / control |
|---|---|
| Whole module layout, model swaps, setbacks, positions, rotations, scales | `Generated/Layouts/*.asset`, then **Rebuild Modules From Edited Layouts** |
| Individual model scale or nested rendering setup | `Generated/Models/*.prefab` |
| Shared wall, roof, glass, road, foliage, trim and sign appearance | `Generated/Materials/*.mat` |
| Muted lavender sky colors, cream cloud tint, sky exposure, cloud rotation and gradient height | `Generated/Materials/LavenderCloudSky.mat` |
| Fog, ambient colors, sun color/intensity, horizon offset/scale, visual references | `Generated/MoriohDesignTheme.asset`, then **2 - Apply To Full Scene** |
| Pose waiting/ready materials, six instruction strings, typography, backing size, status position and colors | `Generated/Activities/MoriohActivityArt.asset` |
| Target idle/yellow active colors, emission, left/right/hit labels and effect references | Same activity art asset |
| Target material slots, label transform and upright presentation | `Generated/Activities/Morioh_HighSideTarget_Visual.prefab` |
| Burst duration, drift, expansion, text and particle settings | `Generated/Activities/PunchHit.prefab` and `CoinPickup.prefab` |
| Jump/duck/dodge proportions and surface graphics | Corresponding generated model prefab; X is normalized to one unit before spawner scaling |

The OBJ geometry itself is a modeled asset: its vertices are not Inspector parameters. Prefab transforms, layout placement, material properties and all newly added runtime behavior controls are Inspector-editable. Detailed mesh changes can be made in a modeling tool, or regenerated from the included source generator. Rebuilding a layout replaces its generated module prefab, so keep layout edits in the layout asset; direct module-prefab changes are overwritten by that command. Regenerating OBJ sources likewise overwrites only the new source kit.

After editing layout assets, the rebuild command updates the four prefab assets. Existing Full scene prefab instances receive the new contents. Theme-reference/atmosphere edits require **2 - Apply To Full Scene** again, which makes another recovery copy. Runtime state effects read the shared activity art asset.

Do not run the older **Build Full Scene**, **Build Final Art**, or **Set Twilight Atmosphere** commands for this installation. They belong to the previous artwork workflow.

## Obstacles and activities

- **Jump:** cream/lavender promenade rail with an ink outline, modeled banding and Japanese/English JUMP sign. The source width is exactly 1 unit; the existing spawner expands it to 10.1 units across three lanes. Its authored height is 0.6 units and its root remains centered on the existing low barrier.
- **Duck:** shop-canopy beam, gold lower edge and DUCK label. Its normalized width is 1 and height is 1.0. The existing root placement leaves the original opening below 1.5 units; no support posts introduce unexpected lane obstacles.
- **Dodge:** full-height delivery screen with ink framing, directional marks and CHANGE LANE sign. It remains 4 units tall and expands to the existing 2.56-unit single-lane width. Existing paired spawning continues to leave the chosen lane open.
- **Pose gate:** original decorative frame, dark high-contrast backing, gold waiting silhouette, soft green ready silhouette, six existing pose diagrams, the corresponding instruction and explicit MATCH THE POSE / READY feedback. Recognition thresholds, matching logic, grace time, movement and trigger dimensions are unchanged.
- **High-side punch targets:** the existing left/right target logic uses a modeled diamond crest instead of generated cubes. It retains the existing close activation window and changes from lavender to yellow. The label changes LEFT/RIGHT to HIT. A separate visual burst plays on a successful hit; score and sound behavior remain the existing behavior.
- **Coin:** gold double-faced crest coin and a pickup sparkle/+1 effect. **Coin spawning remains disabled in the Full scene.** The visuals are wired for use if that gameplay setting is enabled later.

Visual obstacle prefabs contain no Collider or Rigidbody. ObstacleSpawner still creates/sizes the original trigger and applies X scaling at spawn time. Hand controls, pose detection, character animation, camera lens/position, canvas hierarchy and audio assignments are not reconfigured.

## Assets and downloads

No online art, asset pack, shader package, URP, Blender installation, or font download is needed to install this kit. All 32 OBJ models and the three new PNG textures are included locally. The material builder reuses existing project stucco, ceramic roof and jacaranda textures; instructions reuse the project's Bevan font. Japanese shop lettering is already baked into the sign atlas, so the game does not need a Japanese runtime font.

The new models, signs and cloud shapes are original stylized work inspired by the supplied visual direction. No franchise screenshots or extracted franchise models are used as game textures. No source files in third-party asset packs are changed.

For developer regeneration only, `Tools/Morioh/generate_design_kit.py` requires Python 3 and Pillow. Pillow was downloaded into the ignored `Temp/MoriohAuthoringDeps` directory for authoring. It is not a Unity package, game dependency or required player download. The generator uses an existing Windows Japanese font to rasterize the signs; it does not bundle that font file. `preview_design_kit.py` produces a software-rendered source-model contact sheet, not an Editor screenshot.

## Verification and remaining Editor review

Completed outside the Editor:

- Runtime and Editor sources compile against this project's Unity 6000.4.7f1 assemblies with Unity's bundled Roslyn compiler. Runtime emits the existing eight obsolete-API warnings; the Editor assembly has no errors or warnings.
- Source geometry contains 32 models, 19,440 triangles across the library, three textures and a material palette manifest. That library triangle total is not a claim about scene draw calls or frame rate.
- Model faces/indices, material names, finite coordinates, normalized obstacle widths and the 20-unit road boundaries were checked.
- Git confirms no scene, original asset pack, track controller, input/pose-detection script, animation asset or UI asset was changed by this work. The preexisting build-settings edit and GameExec folders remain separate.

Still needed in Unity after installation:

1. Confirm OBJ import/material remapping and both new shaders render correctly in Built-in. These were not imported or shader-compiled in a live Editor session during this work.
2. Review the cloud placement, distant skyline, sign orientation/readability, floor seams and four distinct modules from the actual gameplay camera. The software preview omits Unity's lighting, fog and material textures.
3. When you resume gameplay verification, check one jump, duck, single/pair dodge, all six pose displays and both punch-target states; inspect a recycle boundary. No gameplay/pose/webcam run or performance test was performed for this design delivery.

The source kit uses shared materials with instancing enabled for Standard materials. It still adds more scenery than the prior blockout, so draw calls and transparency/texture import quality need review on the intended machine. The horizon is deliberately unlit and not affected by fog so it remains visible; edit its three silhouette materials if it separates too strongly from the sky.

## Recovery

Before each installation, the apply command copies the Full scene and live theme to `Assets/_Recovery/MoriohDesign_<timestamp>/`. It does not alter `AM3D_ActiveMotionBase.unity`, the Phase 1 scene or build settings. The original street and obstacle prefabs remain available. You can open a recovery scene directly to inspect the prior setup. To restore the exact prior setup, also restore its matching theme config through Git or an explicit restoration operation; do not delete/replace scene assets while they have unsaved Editor work.

The Git branch preserves the state before this design delivery, and a dedicated commit records this work. Scene backups are created when you run the installer, because no scene has been modified yet.
