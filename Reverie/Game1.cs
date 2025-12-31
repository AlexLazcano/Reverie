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

    private Effect _forceFieldEffect;

    private float _time = 0f;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        // Set window size
        _graphics.PreferredBackBufferWidth = 2560;
        _graphics.PreferredBackBufferHeight = 1440;
    }

    protected override void Initialize()
    {
        _particleSystem = new ParticleSystem(maxParticles: 10000, height: _graphics.PreferredBackBufferHeight,
            width: _graphics.PreferredBackBufferWidth);
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

        try
        {
            _forceFieldEffect = Content.Load<Effect>("Shaders/ForceFieldVisualization");
            Console.WriteLine("Force Field Effect loaded successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load Force Field Effect: {ex.Message}");
            _forceFieldEffect = null;
        }
    }

    private void SpawnParticleAtPoint(float x, float y, float angle, float speed = 100f, float lifetime = 5, float spread = 0f)
    {
        var randomSpread = (_random.NextSingle() - 0.5f) * MathHelper.ToRadians(spread);
        var finalAngle = angle + randomSpread;
        var velocity = new Vector2(
            (float)Math.Cos(finalAngle) * speed,
            (float)Math.Sin(finalAngle) * speed
        );

        _particleSystem.SpawnParticle(new Vector2(x, y), velocity, ColorPalette.GetRandomDreamColor(), lifetime, _random.Next(3));
    }

    override protected void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        _time += (float)gameTime.ElapsedGameTime.TotalSeconds;


        for (int i = 0; i < 5; i++)
        {
            // Dreamy blue/purple/cyan colors
            // SpawnParticleAtPoint(600, 600, _time, lifetime:30f);
            const float sharedAngle = MathF.PI * (1f / 8f);
            SpawnParticleAtPoint(0, 0, angle: sharedAngle, lifetime: 30f, spread: 45);
            SpawnParticleAtPoint(_graphics.PreferredBackBufferWidth / 2f, 0, angle: sharedAngle, lifetime: 30f, spread: 45);
            SpawnParticleAtPoint(0, _graphics.PreferredBackBufferHeight / 2f, sharedAngle, lifetime: 30f, spread: 45);
        }

        _particleSystem.Update(gameTime);
        base.Update(gameTime);
    }

    override protected void Draw(GameTime gameTime)
    {
        _time += (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_vhsEffect != null && _renderTarget != null)
        {
            // Smooth galaxy pipeline (VHS only affects particles)
            RenderParticlesToTarget();
            var vhsTarget = ApplyVHSToParticles();
            var galaxyTarget = ApplyGalaxyShader(vhsTarget);
            DrawToScreen(galaxyTarget);

            vhsTarget?.Dispose();
            galaxyTarget?.Dispose();
        }
        else
        {
            GraphicsDevice.Clear(new Color(10, 10, 20));
            _particleSystem.Draw(_spriteBatch);
        }

        base.Draw(gameTime);
    }

    private void RenderParticlesToTarget()
    {
        GraphicsDevice.SetRenderTarget(_renderTarget);
        GraphicsDevice.Clear(new Color(10, 10, 20));
        _particleSystem.Draw(_spriteBatch);
    }

    private RenderTarget2D ApplyGalaxyShader()
    {
        if (_forceFieldEffect == null)
            return null;

        var galaxyTarget = new RenderTarget2D(
            GraphicsDevice,
            _graphics.PreferredBackBufferWidth,
            _graphics.PreferredBackBufferHeight
        );

        GraphicsDevice.SetRenderTarget(galaxyTarget);
        GraphicsDevice.Clear(Color.Transparent);

        _forceFieldEffect.Parameters["Time"]?.SetValue(_time);
        _forceFieldEffect.Parameters["ScreenSize"]?.SetValue(
            new Vector2(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight)
        );
        _forceFieldEffect.Parameters["Intensity"]?.SetValue(0.60f);

        _spriteBatch.Begin(effect: _forceFieldEffect, samplerState: SamplerState.LinearClamp);
        _spriteBatch.Draw(_renderTarget, Vector2.Zero, Color.White);
        _spriteBatch.End();

        return galaxyTarget;
    }


    private RenderTarget2D ApplyVHSToParticles()
    {
        if (_vhsEffect == null)
            return _renderTarget;

        var vhsTarget = new RenderTarget2D(
            GraphicsDevice,
            _graphics.PreferredBackBufferWidth,
            _graphics.PreferredBackBufferHeight
        );

        GraphicsDevice.SetRenderTarget(vhsTarget);
        GraphicsDevice.Clear(Color.Black);

        _vhsEffect.Parameters["Time"]?.SetValue(_time);
        _vhsEffect.Parameters["NoiseAmount"]?.SetValue(0.05f);
        _vhsEffect.Parameters["ScanlineIntensity"]?.SetValue(0.02f);
        _vhsEffect.Parameters["ChromaticAberration"]?.SetValue(0.002f);
        _vhsEffect.Parameters["VignetteStrength"]?.SetValue(0.4f);

        _spriteBatch.Begin(effect: _vhsEffect, samplerState: SamplerState.LinearClamp);
        _spriteBatch.Draw(_renderTarget, Vector2.Zero, Color.White);
        _spriteBatch.End();

        return vhsTarget;
    }

    private RenderTarget2D ApplyGalaxyShader(RenderTarget2D sourceTarget)
    {
        if (_forceFieldEffect == null)
            return sourceTarget;

        var galaxyTarget = new RenderTarget2D(
            GraphicsDevice,
            _graphics.PreferredBackBufferWidth,
            _graphics.PreferredBackBufferHeight
        );

        GraphicsDevice.SetRenderTarget(galaxyTarget);
        GraphicsDevice.Clear(Color.Transparent);

        _forceFieldEffect.Parameters["Time"]?.SetValue(_time);
        _forceFieldEffect.Parameters["ScreenSize"]?.SetValue(
            new Vector2(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight)
        );
        _forceFieldEffect.Parameters["Intensity"]?.SetValue(0.60f);

        _spriteBatch.Begin(effect: _forceFieldEffect, samplerState: SamplerState.LinearClamp);
        _spriteBatch.Draw(sourceTarget, Vector2.Zero, Color.White);
        _spriteBatch.End();

        return galaxyTarget;
    }

    private void DrawToScreen(RenderTarget2D sourceTarget)
    {
        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
        _spriteBatch.Draw(sourceTarget, Vector2.Zero, Color.White);
        _spriteBatch.End();
    }
}