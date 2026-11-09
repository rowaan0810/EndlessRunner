# Menacing Road Signs

Added to the ActiveMotion3D obstacle spawner. Each ordinary obstacle group has a 22% chance of becoming one oversized sign in a random lane. Change lanes to avoid it. The signs use the normal game speed, collision, shield, game-over, despawn and restart paths. They do not appear alongside another obstacle in the same group. Existing Human Tetris spacing applies, and Sheer Heart Attack waits for active signs to clear.

## Research before implementation

Official JoJo animation production notes describe the use of Gogogogo onomatopoeia, motion lines, theatrical presentation, and selective dramatic color shifts:
https://jojo-portal.com/special/production-note/en/02/

The official Diamond is Unbreakable page anchors the setting in Morioh:
https://jojo-portal.com/en/anime/du/

The supplied manga reference was used for the ordinary neighborhood street furniture, strong dark contours, and subdued town colors. The implementation combines those cues with exaggerated scale, cream/teal sign faces, opposing folded arrows and violet floating ゴゴゴ. The reference image itself is not distributed as game artwork.

## Assets and tuning

- Four separate variants: blue circular turn sign, red triangular dead-end sign, yellow diamond bend sign, and lavender floating U-turn sign. Exactly one appears per encounter.
- Reuses the pole mesh from the existing DesignKit RouteMarker model, with custom flat sign graphics.
- Active resource: `Assets/ActiveMotion3D/Art/Obstacles/MenacingRoadSigns/Resources/MenacingRoadSigns/MenacingRoadSignsV2.prefab`.
- All sky lettering and its runtime spawner have been removed. The builder no longer creates lettering.
- Each sign uses a root trigger BoxCollider (2.45m wide, 4m high, 0.65m deep), a kinematic Rigidbody and the standard Obstacle component hierarchy. Running into it triggers the existing PlayerCollision game-over path. A shield absorbs one collision.
- The fixed full-lane collision volume remains in place even for the floating-board variant. Two adjacent lanes stay clear.
- Tune Menacing Road Signs Chance on ObstacleSpawner; zero disables the signs. Spawn Obstacles also applies.
- Tools > Active Motion 3D > Build Menacing Road Signs generates the current design revision, then preserves authored edits.
- Tools > Active Motion 3D > Validate Menacing Road Signs reruns checks and renders the four variants.

## Verification

See `validation-v2.txt` for the latest targeted Unity Editor results and `approved-variants.png` for the actual rendered assets. Validation covers all four variants, collision setup, collision layers, physical overlap with a runner capsule, adjacent-lane clearance, the real PlayerCollision handler for shielded and unshielded impacts, resource loading, restart and pooling. The callback is invoked directly in this targeted Editor check; a full Play Mode/webcam/standalone run is not claimed.
