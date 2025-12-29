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

    private RenderTarget2D _renderTarget;

    private Effect _vhsEffect;

    private float _time = 0f;

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

        // Create render target
        _renderTarget = new RenderTarget2D(
            GraphicsDevice,
            _graphics.PreferredBackBufferWidth,
            _graphics.PreferredBackBufferHeight
        );

        try
        {
            _vhsEffect = Content.Load<Effect>("Shaders/VHSEffect");
            Console.WriteLine("VHS Effect loaded successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load VHS Effect: {ex.Message}");
            _vhsEffect = null;
        }
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();
        
        _time += (float)gameTime.ElapsedGameTime.TotalSeconds;

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
                _ => new Color(100, 255, 255) // Cyan
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
        var renderShader = true;
        _time += (float)gameTime.ElapsedGameTime.TotalSeconds;
    
        if (_vhsEffect != null && _renderTarget != null && renderShader)
        {
            // FIRST PASS: Draw particles to render target
            GraphicsDevice.SetRenderTarget(_renderTarget);
            GraphicsDevice.Clear(new Color(10, 10, 20)); // Dark background
        
            _particleSystem.Draw(_spriteBatch);
        
            // SECOND PASS: Draw render target to screen with VHS effect
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);
        
            _vhsEffect.Parameters["Time"].SetValue(_time);
            _vhsEffect.Parameters["NoiseAmount"].SetValue(0.05f);
            _vhsEffect.Parameters["ScanlineIntensity"].SetValue(0.02f);
            _vhsEffect.Parameters["ChromaticAberration"].SetValue(0.002f);
            _vhsEffect.Parameters["VignetteStrength"].SetValue(0.4f);
        
            _spriteBatch.Begin(effect: _vhsEffect, samplerState: SamplerState.LinearClamp);
            _spriteBatch.Draw(_renderTarget, Vector2.Zero, Color.White);
            _spriteBatch.End();
        }
        else
        {
            GraphicsDevice.Clear(new Color(10, 10, 20));
            _particleSystem.Draw(_spriteBatch);
        }
    
        base.Draw(gameTime);
    }
}