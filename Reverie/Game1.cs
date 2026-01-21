using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Reverie.Source;
using Reverie.Source.Audio;

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

    private float _audioLevel = 0f;

    private AudioCaptureService _audioCapture;

    // Toggle frequency bands on/off for testing
    private const bool EnableBass = true;
    private const bool EnableMid = true;
    private const bool EnableTreble = true;

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

        _audioCapture = new AudioCaptureService();
        _audioCapture.Start();

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

    private void SpawnParticleAtPoint(float x, float y, float angle, float speed = 100f, float lifetime = 5f, float spread = 0f, FrequencyBand frequencyBand = FrequencyBand.Bass)
    {
        var randomSpread = (_random.NextSingle() - 0.5f) * MathHelper.ToRadians(spread);
        var finalAngle = angle + randomSpread;

        var velocity = new Vector2(
            (float)Math.Cos(finalAngle) * speed,
            (float)Math.Sin(finalAngle) * speed
        );

        var baseColor = ColorPalette.GetColorForFrequency(frequencyBand);

        _particleSystem.SpawnParticle(
            new Vector2(x, y),
            velocity,
            baseColor,
            lifetime,
            _random.Next(3),
            frequencyBand
        );
    }

    override protected void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        _time += (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_audioCapture != null)
        {
            _audioLevel = _audioCapture.CurrentLevel;
            _particleSystem.AudioLevel = _audioLevel;
            _particleSystem.Bass = _audioCapture.Bass;
            _particleSystem.Mid = _audioCapture.Mid;
            _particleSystem.Treble = _audioCapture.Treble;
            _particleSystem.BeatIntensity = _audioCapture.BeatIntensity;
        }

        const float lifetime = 10f;
        for (int i = 0; i < 5; i++)
        {
            float screenWidth = _graphics.PreferredBackBufferWidth;
            float screenHeight = _graphics.PreferredBackBufferHeight;
            var center = new Vector2(screenWidth / 2f, screenHeight / 2f);
            float spawnRadius = Math.Min(screenWidth, screenHeight) * 0.45f;
            float angleOffset = MathHelper.ToRadians(60f);  // Slight offset from pointing at center

            // Bass at 0 degrees, Mid at 120, Treble at 240
            if (EnableBass)
            {
                float baseAngle = MathHelper.ToRadians(0f);
                var spawnPos = center + new Vector2(MathF.Cos(baseAngle), MathF.Sin(baseAngle)) * spawnRadius;
                float towardCenter = baseAngle + MathF.PI + angleOffset;
                SpawnParticleAtPoint(spawnPos.X, spawnPos.Y, angle: towardCenter, lifetime: lifetime, spread: 12, frequencyBand: FrequencyBand.Bass);
            }
            if (EnableMid)
            {
                float baseAngle = MathHelper.ToRadians(120f);
                var spawnPos = center + new Vector2(MathF.Cos(baseAngle), MathF.Sin(baseAngle)) * spawnRadius;
                float towardCenter = baseAngle + MathF.PI + angleOffset;
                SpawnParticleAtPoint(spawnPos.X, spawnPos.Y, angle: towardCenter, lifetime: lifetime, spread: 12, frequencyBand: FrequencyBand.Mid);
            }
            if (EnableTreble)
            {
                float baseAngle = MathHelper.ToRadians(240f);
                var spawnPos = center + new Vector2(MathF.Cos(baseAngle), MathF.Sin(baseAngle)) * spawnRadius;
                float towardCenter = baseAngle + MathF.PI + angleOffset;
                SpawnParticleAtPoint(spawnPos.X, spawnPos.Y, angle: towardCenter, lifetime: lifetime, spread: 12, frequencyBand: FrequencyBand.Treble);
            }
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

    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        _audioCapture?.Stop();
        _audioCapture?.Dispose();
        base.OnExiting(sender, args);
    }
}