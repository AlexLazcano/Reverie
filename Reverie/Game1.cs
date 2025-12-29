using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Reverie.Source;

namespace Reverie;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private ParticleSystem _particleSystem;
    private Random _random = new();

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        
        // Set window size
        _graphics.PreferredBackBufferWidth = 1920;
        _graphics.PreferredBackBufferHeight = 1080;
    }

    protected override void Initialize()
    {
        _particleSystem = new ParticleSystem(maxParticles: 10000);
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _particleSystem.Initialize(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || 
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // Spawn particles at mouse position for testing
        var mouseState = Mouse.GetState();
        Vector2 mousePos = new Vector2(mouseState.X, mouseState.Y);
        
        // Spawn a few particles each frame at mouse position
        for (int i = 0; i < 5; i++)
        {
            Vector2 velocity = new Vector2(
                (_random.NextSingle() - 0.5f) * 100f,
                (_random.NextSingle() - 0.5f) * 100f
            );
            
            // Dreamy blue/purple/cyan colors
            Color color = _random.Next(3) switch
            {
                0 => new Color(100, 150, 255), // Blue
                1 => new Color(150, 100, 255), // Purple
                _ => new Color(100, 255, 255)  // Cyan
            };
            
            _particleSystem.SpawnParticle(
                position: mousePos,
                velocity: velocity,
                color: color,
                lifetime: 2f,
                size: 3f
            );
        }
        
        _particleSystem.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(10, 10, 20)); // Dark background
        
        _particleSystem.Draw(_spriteBatch);
        
        base.Draw(gameTime);
    }
}