# Real-Time Water System

A configurable real-time water system built in Unity, designed around reusable
water profiles, artist-facing controls and multiple rendering styles.

The project explores water as a complete rendering system rather than a single
shader, including configurable wave behaviour, depth and absorption, foam,
refraction, planar reflections, underwater rendering, interaction and
alternative stylized art directions.

<p align="center">
  <img src="Documentation/Images/SurfaceShading.jpg" width="100%">
</p>

## Overview

The goal of this project was to build a flexible water system that could support
different environments and art directions without requiring a separate setup
for every scene.

Wave behaviour and simulation settings are separated from the visual material,
allowing the same system to be configured for calm water, deeper or murkier
water, more reflective surfaces, or heavily stylized rendering.

The main controls are exposed directly in Unity so that visual iteration can
happen without modifying shader code.

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
- Buoyancy and water interaction
- Multiple stylized rendering modes
- Reusable water profiles and artist-facing controls

---

## Water Profiles

The water profile controls the behaviour of the surface independently from its
visual material.

Different configurations can produce significantly different results, from
calmer clear water to darker or murkier environments.

<p align="center">
  <img src="Documentation/Images/Calm.jpg" width="49%">
  <img src="Documentation/Images/Murky.jpg" width="49%">
</p>

The profile exposes multiple wave layers alongside controls for direction,
amplitude, wavelength, steepness, speed, shape and crest behaviour.

It also contains interaction and underwater settings.

<p align="center">
  <img src="Documentation/Images/Inspector.jpg" width="420">
</p>

---

## Surface Shading

The surface combines large-scale displacement with multiple layers of normal
detail to create movement at different scales.

<p align="center">
  <img src="Documentation/Images/SurfaceShading.jpg" width="100%">
</p>

Primary, cross and micro-normal layers can be adjusted independently, allowing
the amount and scale of surface detail to be controlled separately from the
underlying waves.

---

## Depth, Colour and Absorption

Water appearance changes according to depth using configurable shallow and deep
colours together with RGB absorption.

<p align="center">
  <img src="Documentation/Images/Depth.jpg" width="100%">
</p>

This allows shallow areas to remain clearer while deeper areas gradually become
denser and take on a different colour.

---

## Foam and Intersections

Foam is used both along shorelines and around geometry intersecting the water
surface.

<p align="center">
  <img src="Documentation/Images/Foam%26Intersection.jpg" width="100%">
</p>

The effect helps visually connect objects and terrain to the water while
remaining independently configurable from the main surface shading.

---

## Refraction and Planar Reflections

The system supports refraction through the water surface as well as planar
scene reflections.

<p align="center">
  <img src="Documentation/Images/Refraction%26Planar.jpg" width="100%">
</p>

Planar reflections use a dedicated component with controls for resolution,
reflected layers, update frequency, maximum distance and clipping.

<p align="center">
  <img src="Documentation/Images/WaterPlanar.jpg" width="520">
</p>

This allows reflection quality and update cost to be configured independently
depending on the needs of the scene.

---

## Interaction and Buoyancy

Objects can interact with the water surface rather than the water being purely
visual.

<p align="center">
  <img src="Documentation/Images/Bouyancy-Objects.jpg" width="100%">
</p>

The water profile also exposes interaction controls such as ripple strength and
ripple lifetime.

---

# Art Direction

The water system is not tied to a single rendering style.

Alongside the more naturalistic setup, I used the same system as a base for
several more heavily art-directed interpretations.

## Stylized Water

### Stylized Surface

<p align="center">
  <img src="Documentation/Images/Stylized1.jpg" width="100%">
</p>

A more graphic interpretation of the water using stronger colour separation
and simplified surface shading.

### Graphic / Fantasy Style

<p align="center">
  <img src="Documentation/Images/Stylized2.png.jpg" width="100%">
</p>

A simplified treatment using a softer colour palette and graphic highlights
instead of a more physically driven surface response.

### Pixel Water

<p align="center">
  <img src="Documentation/Images/Stylized-3-Pixel.jpg" width="100%">
</p>

A pixel-art-inspired treatment that converts surface detail into large,
quantized graphic patterns while retaining the motion and shape of the water.

### Layered Paper Style

<p align="center">
  <img src="Documentation/Images/Stylized-Paper.jpg" width="100%">
</p>

An experimental treatment that separates the water into graphic wave bands,
creating a layered illustrative appearance.

---

## Artist Controls

Surface behaviour and visual appearance are deliberately separated.

The `WaterSurface` component references a reusable profile containing the wave,
quality, interaction and underwater settings.

<p align="center">
  <img src="Documentation/Images/Inspector.jpg" width="420">
</p>

The material exposes the rendering side of the system, including:

- Shallow and deep colour
- RGB absorption
- Primary, cross and micro normals
- Flow maps
- Refraction and reflections
- Foam layers
- Caustics
- Art-direction controls

<p align="center">
  <img src="Documentation/Images/Shader.jpg" width="520">
</p>

This separation allows the appearance of the water to be changed without
rebuilding its underlying surface behaviour.

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
- Reusable water profiles
- Stylized rendering techniques

## Built With

`Unity` `C#` `HLSL` `Real-Time Rendering`

## Status

This is an ongoing rendering project and is being expanded as part of my
Technical Art portfolio.

## Author

**Jamie Lobban**

[Portfolio](https://jamielobban.squarespace.com/)
