using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.ModelsInScene;
using TGC.MonoGame.TP.Tanks;
using TGC.MonoGame.TP.Terrain;

namespace TGC.MonoGame.TP.Renderers;

internal sealed class PointLightShadowRenderer : IDisposable
{
    private const int FaceSize = 256;
    private static readonly (CubeMapFace Face, Vector3 Direction, Vector3 Up)[] Faces =
    {
        (CubeMapFace.PositiveX, Vector3.Right, Vector3.Down),
        (CubeMapFace.NegativeX, Vector3.Left, Vector3.Down),
        (CubeMapFace.PositiveY, Vector3.Up, Vector3.Backward),
        (CubeMapFace.NegativeY, Vector3.Down, Vector3.Forward),
        (CubeMapFace.PositiveZ, Vector3.Backward, Vector3.Down),
        (CubeMapFace.NegativeZ, Vector3.Forward, Vector3.Down)
    };

    private readonly GraphicsDevice _graphicsDevice;
    private readonly Effect _effect;
    private readonly EffectTechnique _opaque;
    private readonly EffectTechnique _cutout;
    private readonly TextureCube _clearMap;
    private readonly CachedShadow[] _cache;
    private readonly TextureCube[] _maps;
    private readonly string[] _mapParameters;
    private readonly List<Caster> _casters = new();

    public PointLightShadowRenderer(GraphicsDevice graphicsDevice, Effect effect, int maxLights)
    {
        _graphicsDevice = graphicsDevice;
        _effect = effect;
        _opaque = effect.Techniques["Opaque"];
        _cutout = effect.Techniques["Cutout"];
        _cache = new CachedShadow[maxLights];
        _maps = new TextureCube[maxLights];
        _mapParameters = new string[maxLights];
        _clearMap = new TextureCube(graphicsDevice, 1, false, SurfaceFormat.Color);
        foreach (var face in Faces)
            _clearMap.SetData(face.Face, new[] { Color.White });
        for (int i = 0; i < maxLights; i++)
            _mapParameters[i] = "PointLightShadowMap" + i;
        Array.Fill(_maps, _clearMap);
    }

    public void Update(Lamp[] lamps, int count, SimpleTerrain terrain,
        IEnumerable<IEnumerable<ModelInScene>> sceneGroups, Matrix cameraViewProjection)
    {
        Array.Fill(_maps, _clearMap);
        var camera = new BoundingFrustum(cameraViewProjection);
        Matrix terrainWorld = terrain?._world ?? Matrix.Identity;

        for (int i = 0; i < count; i++)
        {
            Lamp lamp = lamps[i];
            var influence = new BoundingSphere(lamp.LightPosition, lamp.LightRadius);
            if (influence.Radius <= 0f || !camera.Intersects(influence))
                continue;

            CollectCasters(lamp, influence, sceneGroups);
            CachedShadow slot = _cache[i] ??= new CachedShadow(_graphicsDevice);
            bool terrainChanged = slot.Origin != influence.Center || slot.Radius != influence.Radius ||
                slot.Heightmap != terrain?.HeightmapData || slot.TerrainWorld != terrainWorld;

            if (terrainChanged)
                slot.UpdateTerrainPatch(_graphicsDevice, terrain, influence);

            if (terrainChanged || !slot.HasMap || !SameCasters(slot.Casters))
            {
                DrawCube(slot, influence, terrainWorld);
                slot.Origin = influence.Center;
                slot.Radius = influence.Radius;
                slot.Heightmap = terrain?.HeightmapData;
                slot.TerrainWorld = terrainWorld;
                slot.Casters.Clear();
                slot.Casters.AddRange(_casters);
                slot.HasMap = true;
            }
            _maps[i] = slot.Map;
        }
    }

    public void ApplyMaps(Effect effect)
    {
        effect.Parameters["PointShadowTexelSize"].SetValue(1f / FaceSize);
        for (int i = 0; i < _maps.Length; i++)
            effect.Parameters[_mapParameters[i]].SetValue(_maps[i]);
    }

    private void CollectCasters(Lamp lamp, BoundingSphere influence, IEnumerable<IEnumerable<ModelInScene>> groups)
    {
        _casters.Clear();
        foreach (var group in groups)
        foreach (var model in group)
        {
            if (model == lamp || model._isDisabled || !model.ShouldOcclude || model._model == null)
                continue;

            BoundingBox bounds = model.AABB;
            if (influence.Intersects(bounds))
            {
                Tank tank = model as Tank;
                _casters.Add(new Caster(model, bounds, model._world, model.IsAlphaCutout,
                    tank?.TurretYaw ?? 0f, tank?.CannonPitch ?? 0f));
            }
        }
    }

    private bool SameCasters(List<Caster> cached)
    {
        if (cached.Count != _casters.Count)
            return false;
        for (int i = 0; i < cached.Count; i++)
            if (cached[i] != _casters[i])
                return false;
        return true;
    }

    private void DrawCube(CachedShadow slot, BoundingSphere influence, Matrix terrainWorld)
    {
        var blend = _graphicsDevice.BlendState;
        var depth = _graphicsDevice.DepthStencilState;
        var rasterizer = _graphicsDevice.RasterizerState;
        try
        {
            _graphicsDevice.BlendState = BlendState.Opaque;
            _graphicsDevice.DepthStencilState = DepthStencilState.Default;
            _graphicsDevice.RasterizerState = RasterizerState.CullNone;
            _effect.Parameters["LightOrigin"].SetValue(influence.Center);
            _effect.Parameters["InverseLightRadius"].SetValue(1f / influence.Radius);
            Matrix projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver2, 1f,
                MathF.Min(1f, 0.1f * influence.Radius), influence.Radius);

            foreach (var face in Faces)
            {
                _graphicsDevice.SetRenderTarget(slot.Map, face.Face);
                _graphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.White, 1f, 0);
                Matrix view = Matrix.CreateLookAt(influence.Center, influence.Center + face.Direction, face.Up);
                Matrix viewProjection = view * projection;
                _effect.Parameters["LightViewProjection"].SetValue(viewProjection);

                if (slot.TerrainPatch != null)
                {
                    _effect.Parameters["World"].SetValue(terrainWorld);
                    _opaque.Passes[0].Apply();
                    _graphicsDevice.SetVertexBuffer(slot.TerrainPatch);
                    _graphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, slot.TerrainPatch.VertexCount / 3);
                }

                var frustum = new BoundingFrustum(viewProjection);
                foreach (Caster caster in _casters)
                    if (frustum.Intersects(caster.Bounds))
                        DrawCaster(caster, influence.Center);
            }
        }
        finally
        {
            _graphicsDevice.SetRenderTarget(null);
            _graphicsDevice.BlendState = blend;
            _graphicsDevice.DepthStencilState = depth;
            _graphicsDevice.RasterizerState = rasterizer;
        }
    }

    private void DrawCaster(Caster caster, Vector3 lightOrigin)
    {
        ModelInScene instance = caster.Instance;
        Model model = instance.GetModelForShadowMap(Vector3.Distance(lightOrigin, instance.Position));
        var bones = new Matrix[model.Bones.Count];
        model.CopyAbsoluteBoneTransformsTo(bones);
        var textures = instance.GetTextures();
        int partIndex = 0;

        foreach (ModelMesh mesh in model.Meshes)
        {
            Matrix world = ShadowMapRenderer.GetMeshWorld(instance, mesh, bones[mesh.ParentBone.Index]);
            _effect.Parameters["World"].SetValue(world);

            foreach (ModelMeshPart part in mesh.MeshParts)
            {
                Texture2D texture = partIndex < textures.Count ? textures[partIndex] : null;
                EffectTechnique technique = _opaque;
                if (caster.Cutout && texture != null)
                {
                    _effect.Parameters["ModelTexture"].SetValue(texture);
                    technique = _cutout;
                }
                technique.Passes[0].Apply();

                if (instance is Tank tank && tank._dynamicVertexBuffers.TryGetValue(part, out var dynamicBuffer))
                {
                    _graphicsDevice.SetVertexBuffer(dynamicBuffer);
                    _graphicsDevice.Indices = part.IndexBuffer;
                    _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, part.StartIndex, part.PrimitiveCount);
                }
                else
                {
                    _graphicsDevice.SetVertexBuffer(part.VertexBuffer);
                    _graphicsDevice.Indices = part.IndexBuffer;
                    _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, part.VertexOffset,
                        part.StartIndex, part.PrimitiveCount);
                }
                partIndex++;
            }
        }
    }

    public void Dispose()
    {
        foreach (var entry in _cache)
            entry?.Dispose();
        _clearMap.Dispose();
    }

    private readonly record struct Caster(ModelInScene Instance, BoundingBox Bounds, Matrix World,
        bool Cutout, float TurretYaw, float CannonPitch);

    private sealed class CachedShadow : IDisposable
    {
        public readonly RenderTargetCube Map;
        public readonly List<Caster> Casters = new();
        public Vector3 Origin;
        public float Radius;
        public int[,] Heightmap;
        public Matrix TerrainWorld;
        public VertexBuffer TerrainPatch;
        public bool HasMap;

        public CachedShadow(GraphicsDevice device)
        {
            Map = new RenderTargetCube(device, FaceSize, false, SurfaceFormat.Color, DepthFormat.Depth24,
                0, RenderTargetUsage.PreserveContents);
        }

        public void UpdateTerrainPatch(GraphicsDevice device, SimpleTerrain terrain, BoundingSphere influence)
        {
            TerrainPatch?.Dispose();
            TerrainPatch = null;
            var vertices = terrain?.BuildPointShadowPatch(influence);
            if (vertices is not { Length: > 0 })
                return;

            TerrainPatch = new VertexBuffer(device, VertexPositionTexture.VertexDeclaration, vertices.Length, BufferUsage.WriteOnly);
            TerrainPatch.SetData(vertices);
        }

        public void Dispose()
        {
            TerrainPatch?.Dispose();
            Map.Dispose();
        }
    }
}
