using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Renderers;
using TGC.MonoGame.TP.ModelsInScene;
using Microsoft.Xna.Framework.Content;
using System.Collections.Generic;
using BepuPhysics.Collidables;
using System.Runtime.CompilerServices;
using XnaVector3 = Microsoft.Xna.Framework.Vector3;
using BepuVector3 = System.Numerics.Vector3;
using BepuPhysics;
using BepuUtilities.Memory;
using TGC.MonoGame.TP.Colliders;
using NumericVector3 = System.Numerics.Vector3;
using TGC.MonoGame.TP.Missile;
using System;

namespace TGC.MonoGame.TP.Terrain
{
    public class SimpleTerrain : ModelInScene
    {
        public readonly Texture2D colorMapTexture;
        public readonly Texture2D terrainTexture;
        public readonly Texture2D terrainTexture2;

        public VertexBuffer vbTerrain;
        public List<Triangle> triangles { get; set; } = new List<Triangle>();
        public TerrainRenderer _terrainRenderer { get; set; }

        // Variables para guardar la escala pura del terreno
        private float _scaleXZ;
        private float _scaleY;

        public SimpleTerrain(ContentManager Content, Texture2D heightMap, Texture2D colorMap,
            Texture2D diffuseMap, Texture2D diffuseMap2, TerrainRenderer renderer)
            : base(Content, null, Vector3.Zero, Matrix.Identity, Vector3.One, null, 0)
        {
            _terrainRenderer = renderer;

            _scaleXZ = 100f;
            _scaleY = 4f;

            // cargo el heightmap
            LoadHeightmap(heightMap);

            colorMapTexture = colorMap;
            terrainTexture = diffuseMap;
            terrainTexture2 = diffuseMap2;
        }

        public int[,] HeightmapData { get; private set; }
        public Vector3 Center { get; private set; }

        public void LoadHeightmap(Texture2D heightmap)
        {
            var center = Vector3.Zero; // El centro base
            float tx_scale = 1;

            HeightmapData = LoadHeightMap(heightmap);
            var width = HeightmapData.GetLength(0);
            var length = HeightmapData.GetLength(1);

            float min_h = 256;
            float max_h = 0;
            for (var i = 0; i < width; i++)
            {
                for (var j = 0; j < length; j++)
                {
                    if (HeightmapData[i, j] > max_h) max_h = HeightmapData[i, j];
                    if (HeightmapData[i, j] < min_h) min_h = HeightmapData[i, j];
                }
            }

            var totalVertices = 2 * 3 * (HeightmapData.GetLength(0) - 1) * (HeightmapData.GetLength(1) - 1);
            var dataIdx = 0;
            var data = new VertexPositionNormalTexture[totalVertices];

            center.X = (center.X * _scaleXZ) - (width / 2f * _scaleXZ);
            center.Y = center.Y * _scaleY;
            center.Z = (center.Z * _scaleXZ) - (length / 2f * _scaleXZ);
            Center = center;

            var n = new Vector3[width, length];
            for (var i = 0; i < width - 1; i++)
            {
                for (var j = 0; j < length - 1; j++)
                {
                    var v1 = new Vector3(center.X + (i * _scaleXZ), center.Y + (HeightmapData[i, j] * _scaleY), center.Z + (j * _scaleXZ));
                    var v2 = new Vector3(center.X + (i * _scaleXZ), center.Y + (HeightmapData[i, j + 1] * _scaleY), center.Z + ((j + 1) * _scaleXZ));
                    var v3 = new Vector3(center.X + ((i + 1) * _scaleXZ), center.Y + (HeightmapData[i + 1, j] * _scaleY), center.Z + (j * _scaleXZ));
                    n[i, j] = Vector3.Normalize(Vector3.Cross(v2 - v1, v3 - v1));
                }
            }

            for (var i = 0; i < width - 1; i++)
            {
                for (var j = 0; j < length - 1; j++)
                {
                    var v1 = new Vector3(center.X + (i * _scaleXZ), center.Y + (HeightmapData[i, j] * _scaleY), center.Z + (j * _scaleXZ));
                    var v2 = new Vector3(center.X + (i * _scaleXZ), center.Y + (HeightmapData[i, j + 1] * _scaleY), center.Z + ((j + 1) * _scaleXZ));
                    var v3 = new Vector3(center.X + ((i + 1) * _scaleXZ), center.Y + (HeightmapData[i + 1, j] * _scaleY), center.Z + (j * _scaleXZ));
                    var v4 = new Vector3(center.X + ((i + 1) * _scaleXZ), center.Y + (HeightmapData[i + 1, j + 1] * _scaleY), center.Z + ((j + 1) * _scaleXZ));

                    var t1 = new Vector2(i / (float)width, j / (float)length) * tx_scale;
                    var t2 = new Vector2(i / (float)width, (j + 1) / (float)length) * tx_scale;
                    var t3 = new Vector2((i + 1) / (float)width, j / (float)length) * tx_scale;
                    var t4 = new Vector2((i + 1) / (float)width, (j + 1) / (float)length) * tx_scale;

                    data[dataIdx] = new VertexPositionNormalTexture(v1, n[i, j], t1);
                    data[dataIdx + 1] = new VertexPositionNormalTexture(v2, n[i, j + 1], t2);
                    data[dataIdx + 2] = new VertexPositionNormalTexture(v4, n[i + 1, j + 1], t4);
                    data[dataIdx + 3] = new VertexPositionNormalTexture(v1, n[i, j], t1);
                    data[dataIdx + 4] = new VertexPositionNormalTexture(v4, n[i + 1, j + 1], t4);
                    data[dataIdx + 5] = new VertexPositionNormalTexture(v3, n[i + 1, j], t3);
                    dataIdx += 6;
                }
            }

            vbTerrain = new VertexBuffer(this._terrainRenderer._graphicsDevice, VertexPositionNormalTexture.VertexDeclaration, totalVertices, BufferUsage.WriteOnly);
            vbTerrain.SetData(data);
            LoadTriangles(data);
        }
        private void LoadTriangles(VertexPositionNormalTexture[] data)
        {
            for (var i = 0; i < data.Length / 3; i++)
            {
                XnaVector3 v1 = data[i * 3].Position;
                XnaVector3 v2 = data[i * 3 + 1].Position;
                XnaVector3 v3 = data[i * 3 + 2].Position;
                triangles.Add(new Triangle(
                    new BepuVector3(v1.X, v1.Y, v1.Z),
                    new BepuVector3(v3.X, v3.Y, v3.Z),
                    new BepuVector3(v2.X, v2.Y, v2.Z)
                ));
            }
        }
        protected int[,] LoadHeightMap(Texture2D texture)
        {
            var width = texture.Width;
            var height = texture.Height;
            var rawData = new Color[width * height];
            texture.GetData(rawData);
            var heightmap = new int[width, height];

            for (var i = 0; i < width; i++)
            {
                for (var j = 0; j < height; j++)
                {
                    var pixel = rawData[(j * texture.Width) + i];
                    var intensity = (pixel.R * 0.299f) + (pixel.G * 0.587f) + (pixel.B * 0.114f);
                    heightmap[i, j] = (int)intensity;
                }
            }
            return heightmap;
        }

        public new float Height(float x, float z)
        {
            int width = HeightmapData.GetLength(0);
            int length = HeightmapData.GetLength(1);

            if (width < 2 || length < 2)
                return 0f;

            // Convertir coordenadas del mundo a coordenadas del heightmap.
            // Usamos el mismo origen que LoadHeightmap().
            float posX = (x - Center.X) / _scaleXZ;
            float posZ = (z - Center.Z) / _scaleXZ;

            // Limitar las coordenadas antes de interpolar.
            posX = MathHelper.Clamp(posX, 0f, width - 1f);
            posZ = MathHelper.Clamp(posZ, 0f, length - 1f);

            int x0 = Math.Min((int)MathF.Floor(posX), width - 2);
            int z0 = Math.Min((int)MathF.Floor(posZ), length - 2);

            float fx = posX - x0;
            float fz = posZ - z0;

            float h00 = HeightmapData[x0, z0];
            float h10 = HeightmapData[x0 + 1, z0];
            float h01 = HeightmapData[x0, z0 + 1];
            float h11 = HeightmapData[x0 + 1, z0 + 1];

            // Interpolar sobre los mismos dos triángulos de la malla.
            float height;

            if (fz >= fx)
            {
                // Triángulo v1, v2, v4
                height =
                    h00 * (1f - fz) +
                    h01 * (fz - fx) +
                    h11 * fx;
            }
            else
            {
                // Triángulo v1, v4, v3
                height =
                    h00 * (1f - fx) +
                    h11 * fz +
                    h10 * (fx - fz);
            }

            return Center.Y + height * _scaleY;
        }

        public override void Draw(Matrix view, Matrix projection)
        {
            _terrainRenderer.Draw(this, view, projection);
        }
        public override void CreateCollider(Simulation simulation, BufferPool bufferPool, Effect effect, GraphicsDevice graphicsDevice)
        {
            _collider = new TerrainCollider(triangles, NumericVector3.Zero, simulation, bufferPool, effect, graphicsDevice);
        }
        public override bool ShouldCollideWithMissile(Missil missil)
        {
            float terrainHeight = Height(
            missil._position.X,
            missil._position.Z
            );

            const float groundTolerance = 30f;

            if (missil._position.Y <= terrainHeight + groundTolerance)
            {
                return true;
            }
            return false;
        }
    }

}