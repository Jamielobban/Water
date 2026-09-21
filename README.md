# Real-Time Water System

A configurable real-time water rendering system built in Unity, designed around
reusable water profiles, artist-facing controls, interaction and multiple
rendering styles.

The project explores water as a complete rendering system rather than a single
material, combining configurable wave behaviour, depth-based colour and
absorption, layered surface detail, foam, refraction, planar reflections,
caustics, underwater rendering and more heavily stylized art directions.

<p align="center">
  <img src="Documentation/Images/SurfaceShading.jpg" width="100%">
</p>

## Overview

The goal of this project was to build a flexible water system that could support
different environments and art directions without requiring a completely
separate implementation for each one.

Surface behaviour is separated from visual appearance through reusable water
profiles and configurable materials.

This allows the same underlying system to produce calmer water, darker or
murkier environments, reflective ocean surfaces and much more heavily stylized
results.

A major focus of the project was also exposing the important controls directly
inside Unity so that the water can be iterated visually without modifying
shader code.

---

## Features

- Configurable multi-wave surface displacement
- Reusable water profiles
- Adjustable wave direction, amplitude and wavelength
- Wave steepness and speed controls
- Surface shape and shape variation
- Crest sharpness controls
- Shallow and deep water colour
- Per-channel RGB light absorption
- Configurable colour depth distance
- Layered primary and secondary surface normals
- Micro-ripple detail
- Surface currents
- Flow-map support
- Distance-based normal fading
- Refraction
- Planar reflections
- Configurable reflection quality and update rate
- Surface smoothness and lighting controls
- Sun highlights and micro-sparkle
- Breaking-crest foam
- Shoreline and contact foam
- Object intersection foam
- Ripple and wake foam
- Foam breakup and distortion
- Caustics
- Underwater rendering
- Runtime interaction and ripples
- Buoyancy / floating-object support
- Naturalistic and stylized rendering controls
- Artist-facing Unity controls

---

## Water Profiles

Surface behaviour is controlled through reusable water profiles independently
from the visual material.

Different configurations can create significantly different types of water,
from calmer surfaces to darker or murkier environments.

<p align="center">
  <img src="Documentation/Images/Calm.jpg" width="49%">
  <img src="Documentation/Images/Murky.jpg" width="49%">
</p>

The profile exposes several layers of wave control including:

- Direction
- Amplitude
- Wavelength
- Steepness
- Wave speed
- Shape variation
- Shape scale
- Crest sharpness

It also contains interaction and underwater settings such as ripple strength,
ripple lifetime, underwater colour, density and distortion.

<p align="center">
  <img src="Documentation/Images/Inspector.jpg" width="420">
</p>

Separating these settings from the rendering material allows the physical
character of the surface and its artistic appearance to be adjusted
independently.

---

## Surface Shading

The surface combines larger wave displacement with several scales of smaller
surface detail.

<p align="center">
  <img src="Documentation/Images/SurfaceShading.jpg" width="100%">
</p>

Primary and cross / micro normal layers can be configured independently,
allowing smaller ripples and surface breakup to exist on top of the larger wave
motion.

Normal detail can also fade with distance to give greater control over how the
surface reads across large bodies of water.

---

## Depth, Colour and Absorption

The appearance of the water changes with depth using separate shallow and deep
colour controls together with per-channel RGB absorption.

<p align="center">
  <img src="Documentation/Images/Depth.jpg" width="100%">
</p>

This allows shallow regions to remain brighter and clearer while deeper areas
gradually become denser and take on a different colour.

The system exposes:

- Shallow colour
- Deep colour
- RGB absorption per metre
- Colour depth distance
- Contact edge fading

This makes the transition between shallow shoreline water and deeper areas
configurable for different environments and visual styles.

---

## Normals and Current

Surface detail can be controlled independently from the larger wave
displacement.

The material exposes:

- Primary normal map
- Cross / micro normal map
- Independent normal tiling
- Normal strength
- Micro-ripple strength
- Current direction
- Current speed
- Optional flow-map support
- Flow-map origin and scale
- Distance-based normal fading

This allows both large-scale directional movement and smaller surface detail to
be art-directed separately.

---

## Foam and Intersections

Foam is treated as several independent effects rather than a single surface
mask.

<p align="center">
  <img src="Documentation/Images/Foam%26Intersection.jpg" width="100%">
</p>

The system supports:

- Breaking-crest foam
- Shore / contact foam
- Foam breakup noise
- Foam colour
- Optional unlit foam rendering
- Foam tiling
- Foam distortion
- Contact foam depth
- Contact foam strength
- Crest foam threshold
- Crest foam strength
- Object foam depth
- Object ring strength
- Shore wash foam
- Shore wash speed
- Ripple and wake foam

This means wave crests, shorelines, intersecting geometry and runtime
interactions can all contribute different types of foam without relying on one
shared effect.

---

## Refraction, Reflection and Lighting

The system supports both refraction through the water surface and planar
reflections of the surrounding environment.

<p align="center">
  <img src="Documentation/Images/Refraction%26Planar.jpg" width="100%">
</p>

The material exposes controls for:

- Refraction
- Refraction strength
- Surface smoothness
- Reflection strength
- Planar reflection blending
- Sun highlight colour
- Sun highlight strength
- Micro-sparkle contribution

This allows the reflective and transmissive qualities of the surface to be
balanced depending on the required art direction.

### Planar Reflections

Planar reflections are handled through a dedicated component.

<p align="center">
  <img src="Documentation/Images/WaterPlanar.jpg" width="520">
</p>

The component exposes controls for:

- Source camera
- Reflected layers
- Reflection resolution
- Update frequency
- Maximum reflection distance
- Clipping offset

This makes the quality and runtime cost of planar reflections configurable
independently from the rest of the water system.

---

## Caustics

The material includes configurable caustic projection for submerged surfaces.

Controls include:

- Caustics toggle
- Caustics texture
- Caustics tiling
- Caustics strength
- Maximum caustics depth

This allows the effect to gradually disappear as depth increases rather than
being applied uniformly throughout the entire water volume.

---

## Interaction and Buoyancy

The system is not limited to purely visual rendering.

Objects can interact with the water surface and floating objects can respond to
the water.

<p align="center">
  <img src="Documentation/Images/Bouyancy-Objects.jpg" width="100%">
</p>

The water profile also exposes runtime interaction controls including ripple
strength and ripple lifetime, allowing disturbances to become part of the
surface behaviour.

---

# Art Direction

The system was deliberately designed so that it would not be tied to one
specific rendering style.

Alongside the more naturalistic water setup, I experimented with several
alternative visual treatments while retaining the same underlying water
environment and systems.

## Stylized Water

### Stylized Surface

<p align="center">
  <img src="Documentation/Images/Stylized1.jpg" width="100%">
</p>

A more graphic interpretation of the water using stronger colour separation
and simplified surface shading.

This pushes the underlying wave shapes further into the visual design rather
than relying primarily on physically driven surface detail.

---

### Graphic / Fantasy Water

<p align="center">
  <img src="Documentation/Images/Stylized2.png.jpg" width="100%">
</p>

A much softer treatment using simplified colour transitions and graphic
highlights.

Instead of realistic specular detail, the surface can be pushed toward a more
illustrative or fantasy-oriented appearance.

---

### Pixel Water

<p align="center">
  <img src="Documentation/Images/Stylized-3-Pixel.jpg" width="100%">
</p>

A pixel-art-inspired interpretation that converts the water's surface detail
into larger quantized graphic patterns.

The underlying water movement is retained while the shading itself is
deliberately transformed into a lower-resolution visual language.

---

### Layered Paper Water

<p align="center">
  <img src="Documentation/Images/Stylized-Paper.jpg" width="100%">
</p>

An experimental interpretation that separates the surface into clearly defined
wave bands.

The result creates a layered, almost paper-cut or contour-like appearance while
still using the underlying wave system to drive the surface.

---

## Art-Direction Controls

Additional shader controls allow the water to move gradually between smoother,
more naturalistic rendering and much more stylized results.

These include:

- Backlit wave scattering
- Adjustable colour banding
- Smooth-to-stepped colour transitions
- Stylized crest coverage

This allows the visual language of the surface to change without replacing the
underlying water implementation.

---

## Artist Controls

One of the main goals of the project was to keep the system configurable from
inside Unity.

Surface behaviour and visual appearance are deliberately separated.

The `WaterSurface` component references a reusable water profile containing
wave configuration, quality, interaction and underwater settings.

<p align="center">
  <img src="Documentation/Images/Inspector.jpg" width="420">
</p>

The material handles the visual side of the system.

Its controls are grouped into several areas:

### Colour and Absorption

- Shallow colour
- Deep colour
- RGB absorption
- Colour depth distance
- Contact edge fading

### Normals and Current

- Primary normal
- Cross / micro normal
- Normal tiling
- Normal strength
- Micro-ripple strength
- Current direction and speed
- Flow-map support
- Normal distance fading

### Refraction, Reflection and Light

- Refraction
- Refraction strength
- Smoothness
- Reflection strength
- Planar reflection blend
- Sun highlight
- Sun strength
- Micro sparkle

### Independent Foam Layers

- Breaking-crest foam
- Shore / contact foam
- Foam breakup
- Contact foam
- Crest foam
- Object foam
- Shore wash
- Ripple and wake foam

### Caustics

- Caustics texture
- Tiling
- Strength
- Maximum depth

### Art Direction

- Backlit wave scattering
- Colour banding
- Stylized crest coverage

<p align="center">
  <img src="Documentation/Images/Shader.jpg" width="520">
</p>

The separation between water behaviour and visual rendering means the same
surface configuration can be pushed toward different artistic results without
rebuilding the system.

---

## Technical Focus

This project explores several areas of real-time rendering and Technical Art:

- Procedural water surface generation
- Multi-wave displacement
- Layered normal animation
- Depth-based shading
- Light absorption
- Surface currents
- Flow maps
- Refraction
- Planar reflections
- Dynamic surface lighting
- Shoreline detection
- Geometry intersection effects
- Independent foam systems
- Caustic projection
- Underwater rendering
- Runtime water interaction
- Buoyancy
- Artist-facing controls
- Reusable water profiles
- Stylized rendering techniques

---

## Built With

`Unity` `C#` `HLSL` `Real-Time Rendering`

---

## Status

This is an ongoing real-time rendering project and is being expanded as part of
my Technical Art portfolio.

---

## Author

**Jamie Lobban**

[Portfolio](https://jamielobban.squarespace.com/)
