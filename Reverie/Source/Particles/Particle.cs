using System;
using Microsoft.Xna.Framework;

namespace Reverie.Source;


public static class ColorPalette
{
    // Primary colors
    public readonly static Color DeepMidnightBlue = new Color(10, 10, 20);
    public static readonly Color EtherealCyan = new Color(100, 255, 255);
    public static readonly Color CosmicPurple = new Color(150, 100, 255);
    public static readonly Color OceanBlue = new Color(100, 150, 255);
    
    // Accent colors
    public static readonly Color PaleLavender = new Color(200, 180, 255);
    public static readonly Color DeepIndigo = new Color(45, 27, 105);
    public static readonly Color MistyWhite = new Color(230, 240, 255);
    public static readonly Color MarbledAzure = new Color(64, 128, 200);
    
    private static readonly Random _random = new();
    
    // Get a random dreamy color
    public static Color GetRandomDreamColor()
    {
        return _random.Next(4) switch
        {
            0 => EtherealCyan,
            1 => CosmicPurple,
            2 => OceanBlue,
            _ => PaleLavender
        };
    }
    
    // Lerp between two colors based on particle lifetime or position
    public static Color LerpColors(Color start, Color end, float amount)
    {
        return Color.Lerp(start, end, amount);
    }
}
public record struct Particle
{
    public Vector2 Position;

    public Vector2 Velocity;

    public Color Color;

    public float Lifetime;

    public float MaxLifetime;
    public float Size;
    public bool IsAlive => Lifetime > 0f;

    public void Update(float deltaTime)
    {
        Position += Velocity * deltaTime;
        Lifetime -= deltaTime;
    }

    public void ApplyForce(Vector2 force)
    {
        Velocity += force;
    }
}