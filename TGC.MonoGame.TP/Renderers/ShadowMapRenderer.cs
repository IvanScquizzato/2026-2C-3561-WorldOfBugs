using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.ModelsInScene;
using TGC.MonoGame.TP.Tanks;
using TGC.MonoGame.TP.Terrain;

namespace TGC.MonoGame.TP.Renderers;

/// <summary>
/// Renders the first pass from the sun's point of view. The resulting texture
/// is displayed for debugging; a later pass can compare against its depth.
/// </summary>
public sealed class ShadowMapRenderer : IDisposable
{
    private const int MapSize = 2048;
    private const float ShadowAreaSize = 8000f;
    private const float MaxCasterDistance = 5500f;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly Effect _effect;
    private readonly EffectTechnique _opaqueTechnique;
    private readonly EffectTechnique _cutoutTechnique;

    public RenderTarget2D ShadowMap { get; }
    public Matrix LightViewProjection { get; private set; } = Matrix.Identity;

    public ShadowMapRenderer(GraphicsDevice graphicsDevice, Effect effect)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _effect = effect ?? throw new ArgumentNullException(nameof(effect));
        _opaqueTechnique = effect.Techniques["Opaque"];
        _cutoutTechnique = effect.Techniques["Cutout"];

        // Color works with the project's Reach profile. Depth24 is used for
        // visibility while the color target stores the depth for later lookup.
        ShadowMap = new RenderTarget2D(
            graphicsDevice, MapSize, MapSize, false,
            SurfaceFormat.Color, DepthFormat.Depth24, 0,
            RenderTargetUsage.PreserveContents);
    }

    public void Render(
        Vector3 focus,
        Vector3 directionToSun,
        IEnumerable<IEnumerable<ModelInScene>> sceneGroups)
    {
        Vector3 sunDirection = Vector3.Normalize(directionToSun);
        Vector3 up = Math.Abs(Vector3.Dot(sunDirection, Vector3.Up)) > 0.95f
            ? Vector3.Forward
            : Vector3.Up;
        Matrix lightView = Matrix.CreateLookAt(
            focus + sunDirection * 5000f, focus, up);
        Matrix lightProjection = Matrix.CreateOrthographic(
            ShadowAreaSize, ShadowAreaSize, 1f, 12000f);
        LightViewProjection = lightView * lightProjection;

        _graphicsDevice.SetRenderTarget(ShadowMap);
        try
        {
            _graphicsDevice.Clear(Color.White);
            _graphicsDevice.BlendState = BlendState.Opaque;
            _graphicsDevice.DepthStencilState = DepthStencilState.Default;
            _graphicsDevice.RasterizerState = RasterizerState.CullNone;

            foreach (IEnumerable<ModelInScene> group in sceneGroups)
            {
                foreach (ModelInScene model in group)
                {
                    if (model._isDisabled)
                        continue;

                    if (model is SimpleTerrain terrain)
                    {
                        DrawTerrain(terrain);
                        continue;
                    }

                    if (model._model != null &&
                        Vector3.DistanceSquared(model._position, focus) <=
                        MaxCasterDistance * MaxCasterDistance)
                    {
                        DrawModel(model);
                    }
                }
            }
        }
        finally
        {
            _graphicsDevice.SetRenderTarget(null);
        }
    }

    private void DrawTerrain(SimpleTerrain terrain)
    {
        _effect.CurrentTechnique = _opaqueTechnique;
        _effect.Parameters["WorldLightViewProjection"].SetValue(
            terrain._world * LightViewProjection);
        _effect.CurrentTechnique.Passes[0].Apply();
        _graphicsDevice.SetVertexBuffer(terrain.vbTerrain);
        _graphicsDevice.DrawPrimitives(
            PrimitiveType.TriangleList, 0, terrain.vbTerrain.VertexCount / 3);
    }

    private void DrawModel(ModelInScene model)
    {
        Matrix[] bones = new Matrix[model._model.Bones.Count];
        model._model.CopyAbsoluteBoneTransformsTo(bones);
        List<Texture2D> textures = model.GetTextures();
        int textureIndex = 0;

        foreach (ModelMesh mesh in model._model.Meshes)
        {
            Matrix bone = bones[mesh.ParentBone.Index];
            Matrix world = GetMeshWorld(model, mesh, bone);
            _effect.Parameters["WorldLightViewProjection"].SetValue(
                world * LightViewProjection);

            foreach (ModelMeshPart part in mesh.MeshParts)
            {
                Texture2D texture = textureIndex < textures.Count
                    ? textures[textureIndex]
                    : null;
                _effect.CurrentTechnique = texture == null
                    ? _opaqueTechnique
                    : _cutoutTechnique;
                if (texture != null)
                    _effect.Parameters["ModelTexture"].SetValue(texture);

                _effect.CurrentTechnique.Passes[0].Apply();
                if (model is Tank tank)
                {
                    _graphicsDevice.SetVertexBuffer(tank._dynamicVertexBuffers[part]);
                    _graphicsDevice.Indices = part.IndexBuffer;
                    _graphicsDevice.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList, 0,
                        part.StartIndex, part.PrimitiveCount);
                }
                else
                {
                    _graphicsDevice.SetVertexBuffer(part.VertexBuffer);
                    _graphicsDevice.Indices = part.IndexBuffer;
                    _graphicsDevice.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList, part.VertexOffset,
                        part.StartIndex, part.PrimitiveCount);
                }

                textureIndex++;
            }
        }
    }

    private static Matrix GetMeshWorld(ModelInScene model, ModelMesh mesh, Matrix bone)
    {
        if (model is Tank tank)
        {
            if (mesh.Name == "Turret")
                return bone * Matrix.CreateRotationY(tank.TurretYaw) * tank._world;
            if (mesh.Name == "Cannon")
                return bone * Matrix.CreateRotationX(tank.CannonPitch) *
                    Matrix.CreateRotationY(tank.TurretYaw) * tank._world;
        }

        return bone * model._world;
    }

    public void Dispose()
    {
        ShadowMap.Dispose();
    }
}
