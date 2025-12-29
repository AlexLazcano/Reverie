using Microsoft.Xna.Framework;

namespace Reverie.Source;

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
}