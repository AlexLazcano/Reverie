using Microsoft.Xna.Framework;

namespace Reverie.Source;

public static class ParticleForces
{
    public static Vector2 CalculateVortexAttraction(Vector2 particlePos, Vector2 vortexCenter, float attractionStrength, float spiralStrength)
    {
        var direction = vortexCenter - particlePos;
        var distance = direction.Length();

        if (distance < 1f) return Vector2.Zero;

        // Normalize once to avoid doing it twice
        var normalizedDirection = Vector2.Normalize(direction);

        // Attraction: pull toward center
        var attraction = normalizedDirection * (attractionStrength / distance);

        // Spiral: perpendicular force for rotation
        var perpendicular = new Vector2(-normalizedDirection.Y, normalizedDirection.X);
        var spiral = perpendicular * (spiralStrength / distance); 

        return attraction + spiral;
    }
    
    public static Vector2 CurlNoise(Vector2 position, float time, float strength = 50f)
    {
        var eps = 0.01f;
        var scale = 0.005f;
    
        // Sample noise at nearby points
        var n1 = PerlinNoise.Noise((position.X + eps) * scale, position.Y * scale, time);
        var n2 = PerlinNoise.Noise((position.X - eps) * scale, position.Y * scale, time);
        var n3 = PerlinNoise.Noise(position.X * scale, (position.Y + eps) * scale, time);
        var n4 = PerlinNoise.Noise(position.X * scale, (position.Y - eps) * scale, time);
    
        // Compute curl (creates divergence-free flow)
        var dx = (n3 - n4) / (2f * eps);
        var dy = (n2 - n1) / (2f * eps);
    
        return new Vector2(dx, dy) * strength;
    }
}