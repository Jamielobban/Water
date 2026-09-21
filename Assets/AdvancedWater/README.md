# Advanced Water for Unity 6 / URP 17

A configurable lake and ocean surface with generated starter textures, six appearance profiles, buoyancy, wake emitters, optional planar reflection and a native RenderGraph underwater pass. Built for this project's Unity 6000.3.23f1 / URP 17.3.0 setup.

## Try the demo

1. Open `Assets/AdvancedWater/Demo/Water Demo.unity` (or **Tools > Advanced Water > Open Demo Scene**).
2. Enter Play mode. The demo temporarily uses its own URP pipeline and restores the previous pipeline on exit; your saved project pipeline assets are not changed.
3. Hold **right mouse + WASD** to fly; **Q/E** move down/up, **Shift** accelerates. Fly below the surface to see the underwater effect.
4. **Left click** creates a ripple, **P** cycles nineteen presets, **H** hides the instructions. The boat and crates demonstrate wakes and buoyancy.

The procedural textures are intentional starter assets, not production art. Replace them with your supplied maps to establish the final look.

## Put water in your own scene

1. Drag `Prefabs/Lake.prefab` or `Prefabs/Ocean.prefab` into your scene, or use **GameObject > Advanced Water > Lake** / **Ocean**. The prefab's optional planar-reflection component starts disabled.
2. Position the surface at the desired water level. Keep it horizontal (no X/Z rotation) and use unit scale; change **Water Mesh > Size** instead of scaling.
3. Choose a **Water Profile**. Duplicate a profile **and its material** when you want independent wave and appearance settings.
4. Enable **Depth Texture** and **Opaque Texture** on your active URP asset. The PC asset in this project already has both enabled; the mobile asset does not.
5. For underwater effects, use **Tools > Advanced Water > Install Support on Current Renderer**. This explicitly adds `WaterUnderwaterFeature` to the current pipeline's default Universal Renderer and enables depth/opaque textures. For cameras using a different renderer index, add the feature to that renderer as well. Assign `Shaders/Underwater.shader` and the caustics texture to the feature.
6. Add **Water Planar Reflection** if wanted. Keep all water on the built-in **Water** layer (4), which the reflection camera excludes. It is optional and disabled automatically below High quality.

The core surface works without the underwater feature. It still requires depth and opaque textures even if the refraction distortion toggle is off, because clear-water transmission samples opaque scene color.

## Controls and texture inputs

**Water Profile:** four directional Gerstner waves, amplitude in meters, wavelength in meters, steepness, wave speed, quality, ripple strength/lifetime, and underwater fog/color/waterline settings. The surface inspector expands the shared profile for convenience; edits affect every surface using it.

**Water Profile:** the Wave Shape controls prevent every Gerstner wave from being a scaled copy of the same sine curve. Shape Variation bends wave fronts and changes amplitude over broad regions, Shape Scale controls the size of those regions, and Crest Sharpness creates narrower crests with broader troughs. Rendering and CPU buoyancy use the same equations.

**Material:** shallow/deep colors, per-channel absorption, color depth range, two normals, micro-detail, current, flow map, refraction distortion, smoothness, environment/planar reflection, sunlight highlights, foam, caustics, scattering and optional color banding. Texture tiling is world-space repeats per meter; larger values make texture features smaller. The separate Tiling/Offset fields on texture slots are not used.

Both water shaders now use custom material inspectors. Foam is split into independently tunable surface pattern, breaking crest, shore/contact, moving shore-break, and interaction layers where supported, rather than being controlled by one global coverage value.

| Texture | Channels / import settings |
| --- | --- |
| Normal A / B | Tangent-space normal maps; import as **Normal map**, Repeat, mipmaps on. |
| Foam | R = open-water crest foam coverage; white = foam. Disable sRGB, Repeat, mipmaps on. |
| Contact Foam | R = shoreline/object intersection breakup, sampled independently from crest foam. Disable sRGB, Repeat, mipmaps on. |
| Noise | R = distortion/breakup noise; disable sRGB, Repeat. |
| Caustics | R = light intensity on a black background; disable sRGB, Repeat. Assign the underwater feature's texture too. |
| Optional flow map | RG mapped from 0..1 to -1..1 gives world X/Z direction; B = speed multiplier. Disable sRGB, Clamp. Set Flow Bounds to the map's world X/Z origin and size. Enable Use Flow Map. |

In the stylized shader, **Brush Noise** introduces irregular world-space palette patches so color bands do not look digitally perfect. Brush Scale controls the size of those patches and Brush Strength controls their visibility. It is unrelated to foam. Set Brush Strength to zero for clean cel shading.

The flow map advects the primary normal layer with two blended phases and directs foam. The secondary normal layer remains a cross-current detail layer. It does not redirect geometry waves or physics currents.

## Presets

- **Lake:** small slow waves, reduced normals, no crest foam.
- **Calm Ocean:** rolling directional waves, reflective deep water.
- **Rough Ocean:** larger/steeper waves, broader shore foam, more whitecaps.
- **Tropical:** clear turquoise shallows, low absorption, caustics.
- **Murky:** short visibility, muted colors and caustics.
- **Stylized:** saturated stepped water shading, simplified highlights and reflections, plus persistent pure-white graphic foam across wave crests.

The supplied texture set is assigned by character rather than reused uniformly:

| Preset | Surface normals | Crest foam | Contact foam | Caustics |
| --- | --- | --- | --- | --- |
| Lake | SmoothWaves + SharpWaves | Foam1 (crest strength is disabled) | Intersection_Foam | Caustics_2 |
| Calm Ocean | SmoothWaves + SharpWaves | Foam1 | Intersection_Foam | Caustics_2 |
| Rough Ocean | SharpWaves + RoughWaves | FoamSea | Intersection_Foam | Caustics_1 |
| Tropical | SmoothWaves + SharpWaves | Foam1 | Intersection_Foam | Caustics_1 |
| Murky | SmoothWaves + RoughWaves | Foam1 | Intersection_Foam | Caustics_2 |
| Stylized | SharpWaves + RoughWaves | WindwakerFoam | Foam2 | Caustics_2 |

All presets use IntersectionNoise for slow foam breakup. WaterfallFoam is intentionally unassigned because its vertical streaks are designed for falling water rather than a horizontal lake or ocean surface.

### Dedicated graphic styles

Thirteen additional profiles use `Shaders/StylizedWater.shader`, which replaces realistic water lighting with hard color bands, graphic foam, stepped reflections, and style-specific surface marks:

- **Cel Lagoon:** clean turquoise cel shading with foam limited to shoreline contact.
- **Wind Waker:** broad rolling waves with a separate cream-and-purple graphic foam network drifting across the entire surface; it does not depend on wave crests.
- **Painterly Sea:** subtle animated brush-like palette variation with warm shoreline foam and no crest foam.
- **Graphic Ink:** dark wave contours, high contrast bands, and occasional hard-cut whitecaps.
- **Arcane Water:** violet-blue water with emissive edges and accents; foam stays at shoreline contact.
- **Anime Ocean:** three-band blue palette, broad sharp crests, drifting elongated white marks, and moving shoreline foam.
- **Low-Poly:** opaque turquoise facets with flat per-triangle lighting and a coarser generated grid. The profile overrides grid resolution to 48; switching profiles restores the mesh component's chosen resolution. Normal-map and ripple shading do not perturb the flat faces; interaction foam remains supported.
- **Cozy Pastel:** gentle mint-to-lavender depth shading, slow small waves, warm cream shoreline foam, and softly pulsing four-point stars.

The three illustrated presets use procedural surface artwork, with no new external texture dependencies. **Illustration Tiling**, **Illustration Strength**, and **Illustration Drift** tune anime streaks and cozy stars; **Sparkle Density** applies to Cozy Pastel. These marks are independent of the Foam toggle. Low-Poly uses actual mesh triangles; adjust the profile's **Mesh Resolution Override** for facet size (zero uses the Water Mesh setting). Custom meshes must supply their own coarse topology. Physics still samples the smooth wave surface, so coarse triangles approximate the buoyancy height.

**Tools > Advanced Water > Add Illustrated Presets** creates missing illustrated materials and profiles while preserving existing edits. The supplied demo includes all three in its P-key cycle.

These are independent materials and profiles, so changing one does not affect the realistic presets or the original Stylized preset. The demo's **P** key cycles through all nineteen profiles.

The stylized surface-pattern layer uses two rotated scales, low-frequency distortion, and derivative-smoothed thresholds to reduce obvious repetition and distant shimmer. Moving shore-break bands follow shallow vertical depth. Ripple and wake foam is optional and remains disabled or very weak on styles where a painted ring would be distracting.

Presets control appearance and wave motion independently of the mesh. You can use any profile with either lake or ocean geometry.

## Interaction and floating objects

- Add **Water Buoyancy** to a Rigidbody. Set local float points around the hull and tune Float Depth and Water Drag. Cyan gizmos show the sample points. The component preserves your Rigidbody settings and uses acceleration forces independent of mass.
- Add **Water Interactor** to moving objects at the waterline. Local Offset selects the emitter point; moving across the surface leaves normal-driven expanding ripples without a painted foam outline. Entering the surface emits a stronger disturbance. Assign an optional ParticleSystem for visible spray.
- Call `surface.AddRipple(worldPosition, amplitude, speed, width)` for events such as footsteps or impacts.
- Call `surface.Sample(worldPosition, out height, out normal)` for AI, floating props or your own buoyancy. The overload taking a time supports deterministic sampling.

CPU height queries invert the Gerstner horizontal displacement rather than treating displaced coordinates as wave parameters. Steepness is limited per wave to keep the horizontal mapping non-folding. Wakes modify shading only, so they do not change buoyancy height or displace the mesh. Buoyancy does not replace boat steering, lift or hull hydrodynamics.

## Quality and geometry

| Quality | Surface features | Maximum live ripple slots |
| --- | --- | --- |
| Low | Two normal layers; no refraction distortion, caustics or planar reflection | 4 |
| Medium | Adds refraction distortion and caustics | 12 |
| High | Adds micro normal detail and optional planar reflection | 32 |
| Cinematic | High features; reflection refresh every frame | 32 |

Choose mesh resolution and reflection resolution separately. The default lake is a uniform grid. The ocean is one continuous grid with increasingly spaced vertices toward the edges, following a camera at runtime. This concentrates geometry nearby without cracks between LOD tiles; it is not hardware tessellation or an infinite FFT ocean. Increase Size to exceed your visible horizon and camera far distance. At very large world coordinates, use your project's floating-origin strategy.

Water materials also have independent toggles for foam, caustics, flow maps and refraction distortion. Ripple loops are skipped when no ripples are alive. Low quality is a relative preset, not a claim of mobile performance: opaque/depth copies still have a cost. Profile on target hardware. Each planar reflector renders the scene again; use one for the main visible water body and reduce its resolution/update rate as needed.

## Known boundaries

- Designed for desktop, non-XR URP Universal Renderer with RenderGraph enabled. The planar and underwater modules skip stereo cameras. XR, 2D renderer, compatibility-mode underwater rendering and mobile performance are not validated.
- Surfaces are horizontal. Lake containment uses the renderer's rectangular X/Z bounds, including displacement padding; arbitrary shoreline masks, caves and stacked water volumes need a more specific volume system.
- Transparency/refraction sees opaque geometry only. Transparent objects are not in URP's opaque texture, and intersecting transparent water surfaces are subject to transparent sorting limitations.
- Contact foam and shore wash use scene depth. Wash is an animated visual band, not physically shoaling/breaking geometry. Crest foam is a wave-height heuristic. Neither is a fluid simulation.
- Planar reflections assume a flat mean water plane, so they approximate reflections on large waves. Only the configured source camera receives that planar image; other cameras use environment reflection. Mirrors are excluded from themselves by the water layer.
- Underwater fog uses near-plane wave height and an approximate upward exit intersection. It does not trace refraction through wave volumes; transparent objects do not supply their own depth. The nearest water surface is selected per camera.
- Caustics are animated texture projection, not ray-traced light focusing. Above water they are part of transmitted opaque scene color. Underwater they are a screen-space projection.
- Directional sunlight and environment reflections are supported. Additional point/spot-light highlights, SSR, volumetric light shafts, spectral wave simulation, terrain-driven breaking waves and persistent fluid simulations are outside this implementation.
- Existing custom meshes can be used instead of Water Mesh, but need enough vertices and expanded bounds to contain displaced waves. Do not add a static collider to a displaced water mesh expecting it to follow shader waves; use `Sample`.
- Material preview thumbnails are not a water simulation. The `WaterSurface` component supplies wave/time/interaction values.

## Files and validation

Runtime and shaders are independent of the demo. `Editor/WaterSetup.cs` provides setup menus and a batch validation entry point, `AdvancedWater.Editor.WaterSetup.BuildAndValidate`. Starter generation preserves existing profiles and materials rather than resetting art edits. Demo generation leaves `Assets/Scenes/SampleScene.unity` untouched.

Validation output is written to `Logs/AdvancedWaterValidation.txt`, the Unity log to `Logs/AdvancedWaterUnity.log`, and GPU preview captures to `Logs/WaterPreviews`. The numerical check evaluates 100 displaced points per profile and checks height inversion, finite upward normals and normalization. GPU captures exercise all presets and quality levels, flow maps, ocean geometry, planar reflection, underwater and waterline passes. The batch entry point then enters Play mode for four seconds to check the actual buoyancy bodies, moving boat, ripple emissions and runtime rendering, and exits Unity. These checks do not establish performance on every platform.

**Tools > Advanced Water > Regenerate Starter Textures** deliberately replaces the five generated starter PNGs. Use it only if you want to reset those files; normal starter-asset generation preserves existing art edits.

### Print and retro styles

Three more independent presets are included in the demo's **P** cycle:

- **Watercolor Lake:** teal washes, irregular pigment pooling, warm paper edges, and fine procedural grain. Tune Brush Scale/Strength, Paper Grain Tiling/Strength, and Pigment Pooling.
- **Manga Sea:** ivory and blue-black palette, diagonal crosshatching, dark crest contours, and hard-cut foam. Tune Ink Hatch Tiling/Strength and Dark Wave Lines.
- **Retro Pixel Water:** a limited teal/indigo palette, world-space color blocks, ordered dithering, and stepped surface-pattern animation. Tune Pixel Size in Meters, Pixel Animation FPS, Pixel Dither, and Illustration Tiling/Drift. Geometry and buoyancy remain smoothly animated; this material does not pixelate the rest of the scene or the water silhouette.

These styles generate their artwork in the shader and use the existing foam maps. Materials and profiles are supplied directly; Generate Starter Assets and Add Illustrated Presets preserve existing edits and recreate missing assets. This set was added without running compilation, runtime, or visual checks, at the user's request.

### Paper-cut and bioluminescent styles

- **Paper-cut Ocean:** broad blue layers with gently moving, scalloped cream edges, paper grain, and soft shadow strips under the overlapping edges. Paper Band Tiling controls layer spacing; Paper Shadow Strength/Width controls the apparent separation; Scallop Tiling/Depth and Scalloped Foam Width shape the cut edges. Illustration Drift moves the bands. These are shaded layers on the existing water mesh, not separate geometry. The Foam toggle also controls the scalloped edges.
- **Bioluminescent Water:** dark simplified water, softly pulsing cyan plankton, and emissive boat-wake and ripple trails. Adjust Plankton Glow Color/Strength, Density, Tiling, Fleck Size, Pulse Speed, and Interaction Glow Strength. Trails use existing Water Interactor ripple emissions and boat-wake segments, including their lifetime fades; enable Boat Wake on an interactor to leave a continuous stern trail. Emission works independently of the Foam toggle. The flecks include small shader halos; enabling Bloom in your camera's Volume adds a wider glow. No lighting or Volume assets are changed by this preset.

Both are included in the demo's **P** cycle as independent materials and profiles. No compilation, runtime checks, or visual validation were run for these additions, as requested.

## River with waterfall

Use **GameObject > Advanced Water > River with Waterfall** to add a winding river with an eight-meter waterfall and a wider landing pool. The command creates a reusable River and Waterfall material on first use.

Select the river and move its numbered scene handles, or edit **Points** in the inspector. Points are local positions in downstream order; each has an independent width and foam amount. Add closely spaced points around the lip and landing to round the transition. Keep the path moving downstream without doubling back or crossing itself. Change **Subdivisions** and **Across** for mesh density. Scripted point changes require `Rebuild()`.

The dedicated URP shader animates flow along the mesh, including down the waterfall, with falling-water streaks, bank foam, and point-controlled whitewater. Material controls set river/fall/foam colors, downstream speed, foam strength, and smoothness. It uses opaque water and does not require depth or opaque camera textures. This is a visual river surface; the lake/ocean profile, buoyancy, ripples, planar reflections, and underwater feature are not connected to it. Add terrain or rock geometry around the banks and behind the waterfall to suit your scene.

### Standalone river demo scene

Open `Assets/AdvancedWater/Demo/River/River Demo.unity`, or choose **Tools > Advanced Water > Open River Demo Scene**. Enter Play for the fly camera: right mouse + WASD, Q/E vertical movement, Shift for speed, R to reset, H to hide help.

This scene uses `Runtime/WaterRiverDemo.cs` to construct its river, waterfall, cliff, banks, boulders, light and camera on enable, including in edit mode. Generated children are temporary and rebuilt when reopened; edit the generator for persistent environment changes. Its dedicated material is `Demo/River/River Waterfall.mat` and shader is `Shaders/RiverWaterfall.shader` (Advanced Water/River and Waterfall). It uses the project's active URP pipeline. The ocean/lake demo is separate.

### River styles and waterfall foam

The river demo now includes five dedicated materials in `Demo/River`: Natural River, Cel Lagoon River (stepped shading and graphic ripples), Watercolor River (soft washes and grain), Graphic Ink River (two-tone patches and hatching), and Arcane River (luminous winding ribbons). They share the river-only shader with a **River Style** selector so each material can be customized independently. Press **P** or click **Next river style** during Play. In edit mode select the demo root and change **Style Index** (0–4).

**Waterfall Foam Coverage**, **Landing Foam Coverage**, and **Bank Foam Coverage** separately control whitewater. The curtain uses sparse elongated streaks instead of saturating to solid white; **Foam Strength** scales all coverage and zero removes it. **Style Accent**, **Pattern Scale**, and river/fall/foam colors tune each look. The effects are opaque stylized shading; Arcane adds bright color but needs a bloom-enabled pipeline for a glow halo.
