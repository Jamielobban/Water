# GPU Meadow

## Wind flow maps

Select a grass field and click **Create Swirling Wind Map**, then save the texture asset. It generates two opposing swirls blended with a prevailing breeze and assigns the map. **Flow Influence** blends between mapped wind and procedural wind. **Wind Direction** sets the underlying direction; **Direction Variation** and **Wind Pattern Size** vary it across space even without a texture. The original Wind slider controls total strength.

**Copy Wind Settings to Scene Grass / Flowers** copies these settings to other active vegetation components in the same scene. New flower layers inherit them automatically. Use the same Field Size on layers to align the map; different appearance presets still scale motion and speed, and crystals remain rigid. Save the scene to retain assignments.

Map encoding is linear RGB: R is world X direction, G is world Z direction (0..1 encodes -1..1); B encodes strength (0..1 gives 0..2). Import externally edited maps with sRGB disabled, clamp wrapping and bilinear filtering. Neutral RG with zero B makes calm regions. This adds map generation/assignment, not a vector-painting brush. Gust animation continues over the spatial direction field. No checks or previews were run.

## Valley mesh and slope painting

Open **Demo/Grass Valley.unity** for a separate valley scene. The GrassValley component generates a 129×129 mesh and MeshCollider in edit and play mode, with adjustable Size, Hill Height and Valley Width. The GPU samples the same mesh triangles for root heights and interpolated normals. Slope Alignment blends between upright plants and surface-normal alignment; Max Slope excludes steep ground. Grass, sculpted styles and flowers all follow the assigned surface.

On an existing grass object, **Create Valley Mesh and Use It** creates and assigns a new valley. Remove/disable any old manually placed ground that intersects it. New flower layers inherit its surface; assign the same Surface to existing flower layers. Save your scene. Geometry is regenerated from the saved component settings rather than stored as a separate imported mesh asset.

The brush raycasts only the assigned valley collider and draws its footprint over slopes. Paint maps remain top-down maps. Keep valley transforms at position zero, identity rotation and unit scale; match Field Size and valley Size. This implements the generated valley heightfield, not arbitrary imported mesh/terrain baking or overhangs. No checks or previews were run.

## Anime Meadow

**Anime Meadow** follows Crystal Garden in the P cycle and is available directly in the Style dropdown. It uses long pointed leaves with swept S curves, coordinated wind, cool teal shadow bands, warm green tips, and thin sunlit edge highlights. Near/middle/far geometry uses 6/3/1 segments. Existing maps, species sections, paths, and flower layers remain intact. The existing daisies and cosmos can be painted alongside it; no new flower type is required. No compilation or visual checks were run.

## Four contrasting sculpted styles

The P cycle now includes **Cel Field**, **Curled Ribbons**, **Reed Bed**, and **Crystal Garden** after the original six styles. Select them directly in the Style dropdown to skip ahead.

These use the separate SculptedMeadow shader. Cel Field uses broad angular silhouettes and hard color/light bands; Curled Ribbons makes wide twisting, hooked strips; Reed Bed adds raised brown faceted seed heads on thin stalks; Crystal Garden builds rigid polygonal prisms with pointed crowns, specular highlights and emission. Crystal plants intentionally do not sway. The three painted species retain relative size/color variation but become the selected sculpted form. Flowers continue to use their own shader.

Ribbons, reeds and crystals use 40%, 28% and 16% of eligible candidates respectively, so their larger shapes have breathing room. Painted empty areas remain empty. All retain three geometry LODs: ribbons use 8/4/2 segments; reeds and crystals reduce head/prism sides from 6 to 4 to 3. No checks or visual previews were run for these additions.

## Cycle appearance presets

In Play mode focus the Game view and press **P**, or click **Next grass style** in the HUD. The cycle is Soft Meadow → Classic Meadow → Pastel Storybook → Low Poly → Dry Prairie → Moonlit Fantasy. The current name appears on screen. You can also choose **Style** on the GPU Grass component in the Inspector, including in edit mode.

These are six built-in appearance presets affecting grass palette, blade height/width/curvature, wind speed/strength, and shading. Low Poly reduces near geometry to two segments; Moonlit Fantasy adds emissive color (bloom depends on your rendering settings). Classic Meadow is a visual approximation, not a restoration of the old implementation. The same map, path, species, and candidate positions remain while styles change. Flower layers keep their own existing appearance. Runtime P changes are temporary; choose an Inspector style outside Play mode and save the scene to retain a starting preset. No compilation or visual checks were run for this addition.

## Map presets

The saved **Three Grasses - Winding Path** preset has three sections across X: short turf, meadow, and golden tall grass. A bare path travels across all three sections, bending gently along Z, with feathered grass edges. At the default 100 m field size its clear center is about 2.4 m wide. The map scales with Field Size.

Select the grass object and click **New Map: Three Grasses + Winding Path** to assign a fresh editable copy. This preserves existing paint assets and the preset template; undo restores the previous assignment. Save the scene after assigning. This is a placement preset using the current rendering style, not a shader appearance preset.

## Soft stylized meadow appearance

The default grass now grows in small tufts with scattered single blades, mixed short/broad/tall silhouettes, curved tips, soft sage/lime greens, and patch-scale height and color variation. Shared world-space wind waves sweep across both grass and flowers. Unpainted fields use irregular growth patches and a soft-edged walking path instead of three botanical bands; unpainted flower layers produce sparse flower patches. Assigned paint maps still control placement density and species, including completely erased areas.

Open the meadow to use the updated appearance. To add automatic flower patches without painting, create a separate GPU Grass object, enable Flowers, set Blade Count to about 12,000, match the grass Field Size, and leave Paint Map empty. Existing painted flower layers remain controlled by their maps. The current changes were not compiled or visually checked, at the user's request.

## Flowers

Select your grass object and click **Add Paintable Flower Layer**. Save the new flower map, then on the selected flower layer enable **Scene Brush** and paint Daisy, Poppy, or Tall Cosmos. The empty flower map starts with no flowers; grass underneath is unaffected. Shift erases flowers, and Fill/Clear operate on the flower layer only. Save your scene to keep the new layer.

Flowers have procedural green stems, two leaves, petal geometry, and raised centers. Daisies are cream with golden centers (~0.95 m), poppies red with dark centers (~1.35 m), and cosmos pink/purple (~1.85 m), with seeded height variation. Wind moves the whole plant coherently. Three LODs use 12/8/4 petals, 5/2/1 stem segments, and omit leaves at the far LOD (50/32/14 triangles per plant). Shapes are stylized botanical approximations; no texture downloads are needed. The default flower layer uses 12,000 candidates, independently adjustable from grass. Existing grass maps retain their three original channels; flower layers interpret their own map channels as the three flower types. Do not share one map between grass and flowers unless that coupling is intended.

## Paint grass in the Scene view

Select the GPU Meadow object, then use **Create Empty Paint Map** in its Inspector and save the map asset. Enable **Scene Brush**, choose Short Turf / Meadow / Golden Tall, and left-drag to paint. Shift-drag erases; Alt keeps normal scene orbit. Adjust radius and strength for soft edges. Fill and Clear are undoable; Ctrl/Cmd+Z also undoes strokes. Paint maps save at the end of each stroke; save the scene to retain the map assignment.

This is a 128×128 density/type map brush, similar to vertex painting but independent of mesh tessellation. It paints only the existing flat, world-origin meadow footprint at Y=0, not arbitrary meshes or terrain slopes. Removing the map restores the original procedural meadow. Changing field size stretches the painted map; candidate count controls the maximum possible density. Runtime rendering keeps the same GPU culling and LODs.

Open `Demo/Grass Meadow.unity` and press Play. Hold RMB and use WASD to fly, Q/E to change height, Shift to accelerate. L toggles LOD colors; the HUD also adjusts wind.

240,000 candidate blades are placed once by a compute kernel across a 100 m field. Three irregular bands contain short turf, broad meadow grass, and tall golden grass. A winding path is excluded from placement.

Each rendering camera dispatches frustum and distance culling into three append buffers. GPU counters feed indirect draws without CPU readback. Near/middle/far blades use 5/2/1 segments (10/4/2 triangles); default boundaries are 18/40/85 m. Seeded boundary jitter spreads geometry transitions across 6 m, and stochastic thinning fades the final 8 m. Toggle LOD colors to inspect this; transitions are distributed, not true mesh crossfades.

Select the GPU Grass object to adjust candidate count, field size, LOD distances, wind, and debug colors. Count or size changes rebuild placement. Grass uses world coordinates centered at the origin on a flat plane; moving the component does not move the meadow. Terrain sampling and interaction are not implemented. Grass receives main-light shadows but does not cast shadows. Requires URP and compute/indirect-draw capable hardware; no CPU fallback or XR support is provided. Buffers use approximately 31 MB at the default count, excluding driver overhead.

`Tools > GPU Grass > Create Meadow Scene` regenerates the standalone scene without modifying the water scene or project rendering settings. Save any edits to the meadow elsewhere before regenerating it.
