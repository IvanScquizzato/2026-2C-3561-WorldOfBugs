using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.ModelsInScene;
using TGC.MonoGame.TP.Tanks;
using TGC.MonoGame.TP.Terrain;
using System.Diagnostics;
using BepuPhysics.Trees;
using TGC.MonoGame.TP.Cameras;
using System.IO.Pipes;
namespace TGC.MonoGame.TP.Renderers;

/// <summary>
/// Renders the first pass from the sun's point of view. The resulting texture
/// is displayed for debugging; a later pass can compare against its depth.
/// </summary>
public sealed class ShadowMapRenderer : IDisposable
{
    private const int CloseMapSize = 1024;
    private float CloseShadowAreaSize;
    private const int FarMapSize = 1024;
    private float FarShadowAreaSize;
    private const float MaxCasterDistanceSq = 7500f * 7500f;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly Effect _effect;
    private readonly EffectTechnique _opaqueTechnique;
    private readonly EffectTechnique _cutoutTechnique;

    public RenderTarget2D CloseShadowMap { get; }
    public RenderTarget2D FarShadowMap { get; }
    public Matrix CloseLightViewProjection { get; private set; } = Matrix.Identity;
    public Matrix FarLightViewProjection { get; private set; } = Matrix.Identity;
    public int LastDrawCalls { get; private set; }
    public int LastTriangles { get; private set; }
    public double LastCpuMs { get; private set; }
    private ShadowCasterHull _shadowCasterHull = new ShadowCasterHull();
    public ShadowMapRenderer(GraphicsDevice graphicsDevice, Effect effect, float farShadowAreaSize, float closeShadowAreaSize)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _effect = effect ?? throw new ArgumentNullException(nameof(effect));
        _opaqueTechnique = effect.Techniques["Opaque"];
        _cutoutTechnique = effect.Techniques["Cutout"];
        FarShadowAreaSize = farShadowAreaSize;
        CloseShadowAreaSize = closeShadowAreaSize;
        // Color works with the project's Reach profile. Depth24 is used for
        // visibility while the color target stores the depth for later lookup.
        CloseShadowMap = new RenderTarget2D(
            graphicsDevice, CloseMapSize, CloseMapSize, false,
            SurfaceFormat.Color, DepthFormat.Depth24, 0,
            RenderTargetUsage.DiscardContents);
        FarShadowMap = new RenderTarget2D(
            graphicsDevice, FarMapSize, FarMapSize, false,
            SurfaceFormat.Color, DepthFormat.Depth24, 0,
            RenderTargetUsage.DiscardContents);
    }

    public void Render(
        Vector3 focus,
        Vector3 directionToSun,
        IEnumerable<IEnumerable<ModelInScene>> sceneGroups,
        Camera camera)
    {
        LastDrawCalls = 0;
        LastTriangles = 0;

        Vector3 sunDirection = Vector3.Normalize(directionToSun);
        Vector3 up = Math.Abs(Vector3.Dot(sunDirection, Vector3.Up)) > 0.95f
            ? Vector3.Forward
            : Vector3.Up;
        Matrix lightView = Matrix.CreateLookAt(
            focus + sunDirection * 6000f, focus, up);


        Matrix closeLightProjection = Matrix.CreateOrthographic(
            CloseShadowAreaSize, CloseShadowAreaSize, 1f, 9500f);
        CloseLightViewProjection = lightView * closeLightProjection;


        Matrix farLightProjection = Matrix.CreateOrthographic(
            FarShadowAreaSize, FarShadowAreaSize, 1f, 9500f);
        FarLightViewProjection = lightView * farLightProjection;

        BoundingFrustum closeLightFrustum = new BoundingFrustum(CloseLightViewProjection);
        BoundingFrustum farLightFrustum = new BoundingFrustum(FarLightViewProjection);
        Stopwatch stopwatch = Stopwatch.StartNew();
        _shadowCasterHull.Build(
            camera.View,
            camera.FieldOfView,
            _graphicsDevice.Viewport.AspectRatio,
            camera.NearPlane,
            camera.FarPlane,
            directionToSun,
            MathF.Sqrt(MaxCasterDistanceSq)
        );
        try
        {
            RenderCascade(CloseShadowMap, CloseLightViewProjection, closeLightFrustum, focus, sceneGroups, directionToSun);

            RenderCascade(FarShadowMap, FarLightViewProjection, farLightFrustum, focus, sceneGroups, directionToSun);
        }
        finally
        {
            _graphicsDevice.SetRenderTarget(null);

            stopwatch.Stop();
            //Console.WriteLine($"CPU Time={stopwatch.Elapsed.TotalMilliseconds} ms");

        }
    }
    private void RenderCascade(
        RenderTarget2D target,
        Matrix lightViewProjection,
        BoundingFrustum frustum,
        Vector3 focus,
        IEnumerable<IEnumerable<ModelInScene>> sceneGroups,
        Vector3 directionToSun)
    {
        _graphicsDevice.SetRenderTarget(target);
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
                    DrawTerrain(terrain, lightViewProjection);
                    continue;
                }
                if (Vector3.DistanceSquared(model.Position, focus) > MaxCasterDistanceSq)
                    continue;

                if (!model.AABB.Intersects(frustum))
                    continue;


                if (!_shadowCasterHull.Intersects(model.AABB))
                {
                    continue;
                }


                DrawModel(model, lightViewProjection, focus, directionToSun);


            }
            //Console.WriteLine($"d");
        }
        //Console.WriteLine($"ShadowMapRenderer: DrawCalls={LastDrawCalls}, Triangles={LastTriangles}");
    }
    private void DrawTerrain(SimpleTerrain terrain, Matrix LightViewProjection)
    {
        _effect.CurrentTechnique = _opaqueTechnique;
        _effect.Parameters["WorldLightViewProjection"].SetValue(
            terrain._world * LightViewProjection);
        _effect.CurrentTechnique.Passes[0].Apply();
        _graphicsDevice.SetVertexBuffer(terrain.vbTerrain);
        _graphicsDevice.DrawPrimitives(
            PrimitiveType.TriangleList, 0, terrain.vbTerrain.VertexCount / 3);
        LastDrawCalls++;
        LastTriangles += terrain.vbTerrain.VertexCount / 3;
    }

    private void DrawModel(ModelInScene model, Matrix LightViewProjection, Vector3 focus, Vector3 directionToSun)
    {
        float distanceForShadowMap = Vector3.Cross(focus - model.Position, directionToSun).Length() / directionToSun.Length();
        Model modelo = model.GetModelForShadowMap(distanceForShadowMap);


        Matrix[] bones = new Matrix[modelo.Bones.Count];
        modelo.CopyAbsoluteBoneTransformsTo(bones);
        List<Texture2D> textures = model.GetTextures();
        int textureIndex = 0;

        foreach (ModelMesh mesh in modelo.Meshes)
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
                _effect.CurrentTechnique = model.IsAlphaCutout
                    ? _cutoutTechnique
                    : _opaqueTechnique;
                if (texture != null)
                    _effect.Parameters["ModelTexture"].SetValue(texture);

                _effect.CurrentTechnique.Passes[0].Apply();

                _graphicsDevice.SetVertexBuffer(part.VertexBuffer);
                _graphicsDevice.Indices = part.IndexBuffer;
                _graphicsDevice.DrawIndexedPrimitives(
                    PrimitiveType.TriangleList, part.VertexOffset,
                    part.StartIndex, part.PrimitiveCount);

                LastDrawCalls++;
                LastTriangles += part.PrimitiveCount;
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
        CloseShadowMap.Dispose();
        FarShadowMap.Dispose();
    }
}
