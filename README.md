# Real-Time Water System

A configurable real-time water system built in Unity, designed around reusable
water profiles, artist-facing controls and multiple rendering styles.

The project explores water as a complete rendering system rather than a single
shader, including configurable wave behaviour, depth and absorption, foam,
refraction, planar reflections, underwater rendering, interaction and
alternative stylized art directions.

<p align="center">
  <img src="Documentation/Images/hero.jpg" width="100%">
</p>

## Overview

The goal of this project was to build a flexible water system that could support
different environments and art directions without requiring a separate setup
for every scene.

Wave behaviour and simulation settings are separated from the visual material,
allowing the same system to be configured for calm water, deeper or murkier
water, more reflective surfaces, or heavily stylized rendering.

The system also exposes its main controls directly in Unity so that visual
iteration can happen without modifying shader code.

## Features

- Configurable multi-wave surface displacement
- Adjustable wave direction, amplitude, wavelength and steepness
- Surface shape and crest controls
- Shallow/deep colour and absorption
- Multiple normal layers and micro-surface detail
- Depth-based rendering
- Refraction
- Planar reflections
- Independent foam layers
- Shoreline and geometry intersection foam
- Caustics
- Underwater rendering
- Runtime interaction and ripples
- Flow-map support
- Buoyancy / water interaction support
- Multiple stylized rendering modes
- Reusable water profiles and artist-facing controls

---

## Water Profiles

Different surface behaviours can be created by changing the water profile
without rebuilding the material.

<p align="center">
  <img src="Documentation/Images/calm.jpg" width="49%">
  <img src="Documentation/Images/murky.jpg" width="49%">
</p>

The profile controls the physical character of the surface, including multiple
wave layers, direction, amplitude, wavelength, steepness, speed and crest
behaviour.

It also contains interaction and underwater settings, allowing the same system
to be reused across significantly different water setups.

<p align="center">
  <img src="Documentation/Images/water-surface-inspector.jpg" width="420">
</p>

---

## Surface Shading

The surface combines large-scale wave displacement with multiple layers of
normal detail to create movement at different scales.

<p align="center">
  <img src="Documentation/Images/surface-shading.jpg" width="100%">
</p>

Primary, cross and micro-normal layers can be adjusted independently, while
distance fading helps control surface detail across larger bodies of water.

---

## Depth, Colour and Absorption

Water colour changes with depth using separate shallow and deep colour controls
together with RGB absorption.

<p align="center">
  <img src="Documentation/Images/depth.jpg" width="100%">
</p>

This allows shallow areas to remain clear while deeper areas gradually lose
transmitted light and take on a denser water colour.

The material exposes depth distance and contact-edge controls so the transition
can be adapted to different scales and environments.

---

## Foam and Intersections

Foam is used both at shorelines and around intersecting geometry.

<p align="center">
  <img src="Documentation/Images/foam-intersection.jpg" width="100%">
</p>

The system separates foam behaviour from the underlying surface shading,
allowing intersection effects to remain readable across different water
materials.

---

## Refraction and Reflections

The water supports both refraction through the surface and planar scene
reflections.

<p align="center">
  <img src="Documentation/Images/refraction-planar.jpg" width="100%">
</p>

Planar reflections use a dedicated component with controls for reflection
resolution, layer filtering, update frequency, maximum distance and clipping.

<p align="center">
  <img src="Documentation/Images/planar-reflection-inspector.jpg" width="520">
</p>

This makes reflection quality independently configurable depending on the
performance requirements of the scene.

---

## Interaction and Buoyancy

The water system can interact with objects placed within the surface.

<p align="center">
  <img src="Documentation/Images/buoyancy.jpg" width="100%">
</p>

The water profile also exposes ripple strength and lifetime controls for
runtime interaction.

---

# Art Direction

The rendering system was designed so that the water is not tied to a single
visual style.

Alongside the more naturalistic material, I experimented with several
alternative treatments using the same water environment and underlying
systems.

## Stylized Water

### Posterized Water

<p align="center">
  <img src="Documentation/Images/stylized-posterized.jpg" width="100%">
</p>

A more graphic treatment based around simplified colour separation and
stronger surface shapes.

### Sparkle / Fantasy Water

<p align="center">
  <img src="Documentation/Images/stylized-sparkle.jpg" width="100%">
</p>

A deliberately simplified surface using soft colour transitions and graphic
specular highlights.

### Pixel Water

<p align="center">
  <img src="Documentation/Images/stylized-pixel.jpg" width="100%">
</p>

A pixel-art-inspired interpretation that quantizes the surface detail into
large graphic shapes while retaining the underlying movement of the water.

### Layered / Paper Water

<p align="center">
  <img src="Documentation/Images/stylized-paper.jpg" width="100%">
</p>

A more experimental treatment that converts the water surface into separated
graphic wave bands, producing a layered illustrative appearance.

---

## Artist Controls

The system separates surface behaviour from material appearance.

The `WaterSurface` component references a reusable water profile containing
wave, quality, interaction and underwater settings.

<p align="center">
  <img src="Documentation/Images/water-surface-inspector.jpg" width="420">
</p>

The material then exposes the visual rendering controls separately, including:

- Shallow and deep colour
- RGB absorption
- Surface normals
- Micro ripples
- Flow maps
- Refraction and reflection
- Foam layers
- Caustics
- Art-direction controls

<p align="center">
  <img src="Documentation/Images/material-inspector.jpg" width="520">
</p>

This separation makes it possible to change the artistic appearance of the
water without rebuilding its underlying surface behaviour.

---

## Technical Focus

This project explores several areas of real-time rendering and Technical Art:

- Water surface generation
- Layered surface normals
- Depth-based shading
- Light absorption
- Refraction
- Planar reflections
- Intersection effects
- Runtime interaction
- Underwater rendering
- Artist-facing shader controls
- Reusable rendering profiles
- Stylized rendering techniques

## Built With

`Unity` `C#` `HLSL` `Real-Time Rendering`

## Status

This is an ongoing rendering project and is being expanded as part of my
Technical Art portfolio.

## Author

**Jamie Lobban**

[Portfolio](https://jamielobban.squarespace.com/)
