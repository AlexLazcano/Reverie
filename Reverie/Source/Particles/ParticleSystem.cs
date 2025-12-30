using System;
using System.Collections.Generic;

namespace Reverie.Source;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class ParticleSystem(int maxParticles = 5000, int height = 1440, int width = 2560)
{
    private List<Particle> particles = new();

    private Texture2D particleTexture;

    private Vector2 screenCenter = new(width / 2f, height / 2f);


    private Texture2D CreateGlowTexture(GraphicsDevice graphicsDevice, int size = 64)
    {
        var texture = new Texture2D(graphicsDevice, size, size);
        var data = new Color[size * size];

        var center = new Vector2(size / 2f, size / 2f);

        var radius = size / 2f;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var pos = new Vector2(x, y);
                var distance = Vector2.Distance(pos, center);

                // Smooth falloff for glow
                var intensity = 1f - Math.Clamp(distance / radius, 0f, 1f);
                intensity = (float)Math.Pow(intensity, 2); // Squared for softer glow

                data[y * size + x] = Color.White * intensity;
            }
        }

        texture.SetData(data);
        return texture;
    }

    public void Initialize(GraphicsDevice graphicsDevice)
    {
        // Create a simple 1x1 white pixel texture for particles
        particleTexture = CreateGlowTexture(graphicsDevice, 32);
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
        for (var i = particles.Count - 1; i >= 0; i--)
        {
            var particle = particles[i];
            // var force = ParticleForces.CalculateVortexAttraction(
            //     particle.Position,
            //     screenCenter,
            //     150f,
            //     250f
            // );

            // particle.ApplyForce(force);
            
            var curlForce = ParticleForces.CurlNoise(particle.Position, deltaTime);

            particle.ApplyForce(curlForce);
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

            spriteBatch.Draw(
                particleTexture,
                particle.Position,
                null,
                color,
                0f,
                new Vector2(particleTexture.Width / 2f, particleTexture.Height / 2f), // Center origin
                particle.Size * 0.2f, // Scale down since texture is larger
                SpriteEffects.None,
                0f
            );
        }

        spriteBatch.End();
    }

    public int ParticleCount => particles.Count;
}