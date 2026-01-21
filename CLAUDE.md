# Reverie

A visual effects and particle system demo built with MonoGame.

## Tech Stack

- **Framework**: MonoGame 3.8 (DesktopGL)
- **Language**: C# / .NET 9.0
- **Audio**: NAudio 2.2.1
- **Shaders**: HLSL (.fx files)

## Project Structure

```
Reverie/
├── Game1.cs              # Main game class, rendering pipeline
├── Program.cs            # Entry point
├── Source/
│   └── Particles/        # Particle system implementation
│       ├── Particle.cs
│       ├── ParticleSystem.cs
│       ├── ParticleForces.cs
│       └── PerlinNoise.cs
└── Content/
    ├── Content.mgcb      # MonoGame Content Builder config
    └── Shaders/
        ├── VHSEffect.fx              # VHS post-processing shader
        └── ForceFieldVisualization.fx # Nebula/galaxy shader
```

## Build & Run

```bash
dotnet build
dotnet run --project Reverie
```

## Key Systems

### Particle System
- Located in `Source/Particles/`
- Supports up to 10,000 particles
- Uses additive blending with glow textures
- Includes vortex attraction and force field effects

### Shader Pipeline
Multi-pass rendering in `Game1.Draw()`:
1. Render particles to offscreen target
2. Apply VHS effect (chromatic aberration, scanlines, noise)
3. Apply galaxy/nebula shader
4. Composite to screen

### Shader Parameters
**VHSEffect**: `Time`, `NoiseAmount`, `ScanlineIntensity`, `ChromaticAberration`, `VignetteStrength`

**ForceFieldVisualization**: `Time`, `ScreenSize`, `Intensity`

## Code Conventions

- File-scoped namespaces (`namespace Reverie;`)
- Primary constructors for classes
- Private fields with underscore prefix (`_graphics`, `_spriteBatch`)
- MonoGame standard lifecycle: `Initialize()` → `LoadContent()` → `Update()` → `Draw()`
