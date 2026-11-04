# Painted / 3D hybrid background

Branch: `codex/morioh-hybrid-background`.

This replaces the single panorama cloud with a painted lavender sky, independent illustrated cloud cards, two painted wooded hill layers and a detailed low-poly town silhouette. The assets and installer are ready locally. The new background has not been applied to a scene by Codex; installation remains an explicit Unity Editor operation.

## Install

1. Exit Play Mode and save any pending Full scene edits. Let Unity import/compile; use **Assets → Refresh** if the new menu has not appeared.
2. Run **Tools → Morioh → Background → 1 - Build Hybrid Background Assets**.
3. Run **Tools → Morioh → Background → 2 - Apply To Full Scene**.
4. Open `Assets/ActiveMotion3D/Scenes/AM3D_Morioh_Full.unity`. Inspect the composition, then enter Play Mode to see the slow cloud drift.

These are the new **Background** commands; the older **Design Kit** creation command is not required again. The apply command makes recovery copies of the Full scene, live theme config, installed design theme and matching sky/ground materials before changing them. Recovery files go to `Assets/_Recovery/MoriohHybrid_<timestamp>/`. It replaces only the known `Morioh_Horizon` background root, sets the painted sky, switches the existing camera to Skybox clear mode and matches the fog color/density to the sky horizon and ground backdrop. Camera position, FOV and clipping planes are preserved. It updates only background/fog fields in both theme configs so the older Apply Config command will retain the hybrid background later.

If the Full scene has unsaved changes, the installer stops so you can save your current work first. Completed background assets are preserved on a second Build attempt. An interrupted build is moved, with its GUIDs, to `Assets/_Recovery/MoriohHybridIncomplete_<timestamp>` before a fresh build. No automatic installation runs on import or entering Play.

## Composition and motion

- **Painted sky:** an original cloud-free lavender painting with subtle brushwork and a warm low horizon. The sky shader mirrors the painting horizontally to avoid an image-edge seam.
- **Distant wisps:** 18 cards at radius 450, drifting at 0.035 degrees per second.
- **Near wisps:** 12 cards at radius 400, drifting at 0.08 degrees per second. Faster near motion gives the layers depth. Only the cards in the camera's field of view are visible; all 30 are distributed around the sky, not crowded into one view.
- **Cloud variation:** 12 distinct hand-painted slender shapes in a 4×3 transparent atlas. Individual card widths/elevations vary. There is no baked cloud in the sky painting, so clouds can move independently.
- **Painted hills:** two circular layers at different radii, heights, tints and panorama offsets. The illustration has visible forest and ridge detail. Mirrored repetition keeps the ring continuous; the alpha-tested ridge lets the sky/clouds show above it and correctly occludes clouds behind the hills.
- **Town:** two rings of varied houses/apartments with hipped and flat roofs, window bands, rooftop structures and antennas. A clock spire, low station roof, radio mast and water tower break the skyline. Five material groups keep the town compact despite its many buildings; the ground backdrop has a separate sixth material and stays below the track when the town elevation changes.
- **Continuity:** all of this belongs to one background prefab outside TrackManager. The existing horizontal camera follower keeps it centered. The track continues recycling independently.

Clouds use unlit transparent materials and hills/town use unlit distance materials, so fog does not erase their artwork. A ground backdrop beneath the track matches the horizon color and covers the distance beyond the recycled pool. No new background object has a Collider or Rigidbody.

## Inspector controls

The generated profile is:

`Assets/ActiveMotion3D/Art/Environment/Morioh/HybridBackground/Generated/MoriohBackgroundProfile.asset`

| Control | Where |
|---|---|
| Cloud speed and direction | `Cloud Bands → Drift Degrees Per Second`; negative values reverse direction |
| Layer opacity/tint and radius | `Cloud Bands → Tint / Radius` |
| Each cloud's shape, visibility, position and width | `Cloud Bands → Clouds → Sprite Index / Visible / Azimuth / Elevation / Width` |
| Whether drift continues while paused | `Animate While Paused`, enabled by default; this changes only background animation |
| Hill radius, top/bottom height and tint | `Painted Hills` fields |
| Town apparent size and elevation | `Town Scale / Town Vertical Offset` |
| Atlas layout and cell padding | `Atlas Columns / Rows / Inset` |
| Matching fog color and density | `Horizon Color / Fog Density`, then apply to the Full scene again |
| Sky exposure, painting height/rotation, tint and horizon blend | Generated `Materials/PaintedLavenderSky.mat` |
| Cloud saturation and faint-edge cleanup | Generated `Materials/IllustratedClouds.mat` |
| Painted hill panorama repetition and offsets | Generated `Materials/FarPaintedHills.mat` and `NearPaintedHills.mat` |
| Town silhouette colors | Generated `Materials/TownNear.mat`, `TownMid.mat`, `Roof.mat`, `Window.mat`, `Trim.mat` |

Existing cloud position/width/speed, band tint/radius, hill geometry controls and town transforms update during Play. In Edit Mode, use **Tools → Morioh → Background → Rebuild From Profile** to bake profile changes into the prefab preview. Rebuild is also required if you add/remove cards or change atlas rows, columns or inset. It preserves existing mesh/prefab GUIDs. Shared material edits appear directly. Do not regenerate all assets merely to change a speed or tint.

The illustrated mesh/model geometry is a static asset; vertex-level edits require a modeling tool or the included geometry source. The runtime/design parameters above are Inspector-editable.

## Exact source files

Under `Assets/ActiveMotion3D/Art/Environment/Morioh/HybridBackground/`:

- `Textures/PaintedLavenderSky.png` — 1774×887, opaque painted sky.
- `Textures/AnimeCloudAtlas.png` — 1774×887, real alpha, twelve cloud shapes in 4×3 cells.
- `Textures/PaintedWoodedHills.png` — 2172×724, real alpha above the wooded ridge.
- `PaintedSky.shader` — Built-in skybox shader.
- `IllustratedCloud.shader` — Built-in transparent unlit cloud shader.
- `PaintedHills.shader` — Built-in alpha-tested unlit hill shader.
- `Models/TownSilhouette.obj`, `Palette.mtl`, `manifest.json` — original town mesh and material manifest; 15,138 triangles across five groups.

New runtime sources:

- `Assets/ActiveMotion3D/Art/Scripts/Environment/MoriohBackgroundProfile.cs` — cloud/hill/town/atmosphere settings.
- `Assets/ActiveMotion3D/Art/Scripts/Environment/MoriohBackgroundController.cs` — continuous visual drift and layout application.

New authoring source:

- `Assets/Editor/MoriohHybridBackgroundBuilder.cs` — Build, Apply and Rebuild commands.
- `Tools/Morioh/generate_hybrid_town.py` — reproducible original town geometry.

The existing `Tools/Morioh/generate_design_kit.py` now loads Pillow only when generating textures, so the town generator can reuse its mesh helpers without installing image tooling. Existing runtime gameplay scripts were not changed for this background.

Generated output includes the profile, 10 materials, 12 cloud sprite mesh assets, one hill-ring mesh, one ground mesh, `TownSilhouette.prefab` and `Morioh_HybridBackground.prefab`. All generated output is under the hybrid folder's `Generated/` directory. The sky/horizon/material/profile source assets have paired `.meta` files with stable GUIDs and explicit 2D texture metadata.

## Artwork provenance and downloads

The three paintings were created using the built-in **image_gen** tool and copied into the project. Their complete prompts and transparency settings are recorded in `Docs/MoriohDesign/HYBRID_ART_PROMPTS.json`. The original generated outputs remain in the Codex generated-images folder; project assets do not depend on that folder.

No online asset pack, font, shader package, URP package, Blender installation or player download is needed. The town uses original geometry and the existing project's `Morioh/Horizon Ink` shader. PNGs are imported as ordinary 2D textures with alpha from input, mipmaps and uncompressed color/alpha to preserve the painting edges. The images add texture memory; compression can be tuned later for the intended platform after appearance is approved.

## Verification and remaining review

Runtime and Editor code compile against the project's Unity 6000.4.7f1 assemblies using Unity's bundled compiler without launching the Editor. Runtime has the existing eight obsolete-API warnings; the Editor assembly compiles without warnings or errors. All three images were visually inspected; alpha checks confirm genuine transparent pixels in the cloud/hill paintings, and the sky is opaque. Town vertex/UV indices, finite coordinates, material names and triangle counts were checked. Cloud motion uses continuous angular positions around a ring, so wrapping does not teleport a visible sprite.

Full prefab assembly, shader rendering and the installed composition are still pending. The already-running Editor automatically imported the new source images/shaders while files were being prepared; Codex did not operate the Editor or run the installation commands. After installation, review from the actual game camera: several small clouds should be visible, drift should be noticeable over 10–30 seconds, the wooded ridges should be visible behind the town, and the old single panorama cloud should be gone. Check one recycle boundary when gameplay review resumes. No gameplay, webcam or build test was run for this background work.

The existing installed Design Kit scene/config/import changes were already present when this work began; they were preserved. No scene was saved by Codex during this delivery. The original ActiveMotionBase scene, track/controller systems, lane width, obstacle colliders, controls, pose recognition, animations, UI and build settings were not edited by this work.
