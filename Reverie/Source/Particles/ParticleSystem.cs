using System.Collections.Generic;

namespace Reverie.Source;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class ParticleSystem
{
    private List<Particle> particles = new();

    private readonly int maxParticles;

    private Texture2D particleTexture;

    public ParticleSystem(int maxParticles = 5000)
    {
        this.maxParticles = maxParticles;
    }

    public void Initialize(GraphicsDevice graphicsDevice)
    {
        // Create a simple 1x1 white pixel texture for particles
        particleTexture = new Texture2D(graphicsDevice, 1, 1);
        particleTexture.SetData(new[] { Color.White });
    }

    public void SpawnParticle(Vector2 position, Vector2 velocity, Color color, float lifetime, float size = 2f)
    {
        if (particles.Count >= maxParticles)
            return;

        particles.Add(new Particle
        {
            Position = position,
            Velocity = velocity,
            Color = color,
            Lifetime = lifetime,
            MaxLifetime = lifetime,
            Size = size
        });
    }

    public void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Update all particles
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            var particle = particles[i];
            particle.Update(deltaTime);

            if (particle.IsAlive)
            {
                particles[i] = particle;
            }
            else
            {
                particles.RemoveAt(i);
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);

        foreach (var particle in particles)
        {
            // Fade alpha based on lifetime
            float alpha = particle.Lifetime / particle.MaxLifetime;
            Color color = particle.Color * alpha;

            // Outer glow (large, faint)
            spriteBatch.Draw(
                particleTexture,
                particle.Position,
                null,
                color * 0.1f,
                0f,
                Vector2.Zero,
                particle.Size * 4f,
                SpriteEffects.None,
                0f
            );
            // Middle glow
            spriteBatch.Draw(
                particleTexture,
                particle.Position,
                null,
                color * 0.3f,
                0f,
                Vector2.Zero,
                particle.Size * 2f,
                SpriteEffects.None,
                0f
            );

            // Core (bright)
            spriteBatch.Draw(
                particleTexture,
                particle.Position,
                null,
                color,
                0f,
                Vector2.Zero,
                particle.Size,
                SpriteEffects.None,
                0f
            );
        }

        spriteBatch.End();
    }

    public int ParticleCount => particles.Count;
}