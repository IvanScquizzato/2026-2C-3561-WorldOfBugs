using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.ModelsInScene;

namespace TGC.MonoGame.TP.Renderers;

internal sealed class LampBloomRenderer : IDisposable
{
    private const float BlurSpread = 1.65f;
    private const float BloomStrength = 1.1f;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly PBRRenderer _models;
    private readonly Effect _effect;
    private readonly List<Lamp> _visibleLamps = new();
    private readonly BlendState _writeAlpha = new() { ColorWriteChannels = ColorWriteChannels.Alpha };

    private RenderTarget2D _scene;
    private RenderTarget2D _horizontal;
    private RenderTarget2D _vertical;

    public LampBloomRenderer(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Texture2D pixel,
        PBRRenderer models, Effect effect)
    {
        _graphicsDevice = graphicsDevice;
        _spriteBatch = spriteBatch;
        _pixel = pixel;
        _models = models;
        _effect = effect;
    }

    public bool BeginScene(IEnumerable<ModelInScene> props, Matrix view, Matrix projection)
    {
        _visibleLamps.Clear();
        var frustum = new BoundingFrustum(view * projection);
        foreach (var prop in props)
        {
            if (prop is Lamp lamp && lamp.EffectiveLightIntensity > 0f && frustum.Intersects(lamp.AABB))
                _visibleLamps.Add(lamp);
        }

        if (_visibleLamps.Count == 0)
            return false;

        var presentation = _graphicsDevice.PresentationParameters;
        EnsureTargets(presentation.BackBufferWidth, presentation.BackBufferHeight);
        _graphicsDevice.SetRenderTarget(_scene);
        return true;
    }

    public void Composite(Matrix view, Matrix projection)
    {
        var blend = _graphicsDevice.BlendState;
        var depth = _graphicsDevice.DepthStencilState;
        var rasterizer = _graphicsDevice.RasterizerState;
        var sampler0 = _graphicsDevice.SamplerStates[0];
        var sampler1 = _graphicsDevice.SamplerStates[1];
        var texture0 = _graphicsDevice.Textures[0];
        var texture1 = _graphicsDevice.Textures[1];
        bool backBufferBound = false;

        try
        {
            // The scene keeps its colors and depth. Alpha stores the visible glass mask.
            _spriteBatch.Begin(SpriteSortMode.Deferred, _writeAlpha, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone);
            _spriteBatch.Draw(_pixel, _graphicsDevice.Viewport.Bounds, Color.Transparent);
            _spriteBatch.End();

            _graphicsDevice.BlendState = _writeAlpha;
            _graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
            _graphicsDevice.RasterizerState = RasterizerState.CullNone;
            foreach (Lamp lamp in _visibleLamps)
                _models.DrawLampBloomMask(lamp, view, projection);

            _effect.CurrentTechnique = _effect.Techniques["BlurGlow"];
            _effect.Parameters["UseEmissionMask"].SetValue(1f);
            _effect.Parameters["BlurStep"].SetValue(new Vector2(BlurSpread / _horizontal.Width, 0f));
            _graphicsDevice.SetRenderTarget(_horizontal);
            DrawFullscreen(_scene);

            _effect.Parameters["UseEmissionMask"].SetValue(0f);
            _effect.Parameters["BlurStep"].SetValue(new Vector2(0f, BlurSpread / _vertical.Height));
            _graphicsDevice.SetRenderTarget(_vertical);
            DrawFullscreen(_horizontal);

            _graphicsDevice.SetRenderTarget(null);
            backBufferBound = true;
            _effect.CurrentTechnique = _effect.Techniques["CompositeGlow"];
            _effect.Parameters["BloomTexture"].SetValue(_vertical);
            _effect.Parameters["BloomStrength"].SetValue(BloomStrength);
            DrawFullscreen(_scene);
        }
        finally
        {
            if (!backBufferBound)
                _graphicsDevice.SetRenderTarget(null);
            _graphicsDevice.BlendState = blend;
            _graphicsDevice.DepthStencilState = depth;
            _graphicsDevice.RasterizerState = rasterizer;
            _graphicsDevice.SamplerStates[0] = sampler0;
            _graphicsDevice.SamplerStates[1] = sampler1;
            _graphicsDevice.Textures[0] = texture0;
            _graphicsDevice.Textures[1] = texture1;
        }
    }

    private void DrawFullscreen(Texture2D source)
    {
        Viewport viewport = _graphicsDevice.Viewport;
        _effect.Parameters["SourceTexture"].SetValue(source);
        _effect.Parameters["MatrixTransform"].SetValue(Matrix.CreateOrthographicOffCenter(
            0, viewport.Width, viewport.Height, 0, 0, -1));
        _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, _effect);
        _spriteBatch.Draw(source, viewport.Bounds, Color.White);
        _spriteBatch.End();
    }

    private void EnsureTargets(int width, int height)
    {
        if (_scene != null && _scene.Width == width && _scene.Height == height)
            return;

        _scene?.Dispose();
        _horizontal?.Dispose();
        _vertical?.Dispose();

        _scene = new RenderTarget2D(_graphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24);
        int halfWidth = Math.Max(1, (width + 1) / 2);
        int halfHeight = Math.Max(1, (height + 1) / 2);
        _horizontal = new RenderTarget2D(_graphicsDevice, halfWidth, halfHeight, false, SurfaceFormat.Color, DepthFormat.None);
        _vertical = new RenderTarget2D(_graphicsDevice, halfWidth, halfHeight, false, SurfaceFormat.Color, DepthFormat.None);
    }

    public void Dispose()
    {
        _scene?.Dispose();
        _horizontal?.Dispose();
        _vertical?.Dispose();
        _writeAlpha.Dispose();
    }
}
