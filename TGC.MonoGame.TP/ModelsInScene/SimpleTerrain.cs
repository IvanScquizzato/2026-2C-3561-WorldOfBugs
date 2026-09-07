using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Renderers;
using TGC.MonoGame.TP.ModelsInScene;
using Microsoft.Xna.Framework.Content;

namespace TGC.MonoGame.TP.Terrain
{
    public class SimpleTerrain : ModelInScene
    {
        public readonly Texture2D colorMapTexture;
        public readonly Texture2D terrainTexture;
        public readonly Texture2D terrainTexture2;
        public VertexBuffer vbTerrain;
        public TerrainRenderer _terrainRenderer { get; set; }

        // Variables para guardar la escala pura del terreno
        private float _scaleXZ;
        private float _scaleY;

        public SimpleTerrain(ContentManager Content, Texture2D heightMap, Texture2D colorMap,
            Texture2D diffuseMap, Texture2D diffuseMap2, TerrainRenderer renderer)
            : base(Content, null, Vector3.Zero, Matrix.Identity, Vector3.One, null)
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

        public float Height(float x, float z)
        {
            var width = HeightmapData.GetLength(0);
            var length = HeightmapData.GetLength(1);

            var pos_i = (x / _scaleXZ) + (width / 2.0f);
            var pos_j = (z / _scaleXZ) + (length / 2.0f);
            var pi = (int)pos_i;
            var fracc_i = pos_i - pi;
            var pj = (int)pos_j;
            var fracc_j = pos_j - pj;

            if (pi < 0) pi = 0;
            else if (pi >= width) pi = width - 1;
            if (pj < 0) pj = 0;
            else if (pj >= length) pj = length - 1;

            var pi1 = pi + 1;
            var pj1 = pj + 1;
            if (pi1 >= width) pi1 = width - 1;
            if (pj1 >= length) pj1 = length - 1;

            var h0 = HeightmapData[pi, pj];
            var h1 = HeightmapData[pi1, pj];
            var h2 = HeightmapData[pi, pj1];
            var h3 = HeightmapData[pi1, pj1];
            var h = (((h0 * (1 - fracc_i)) + (h1 * fracc_i)) * (1 - fracc_j)) + (((h2 * (1 - fracc_i)) + (h3 * fracc_i)) * fracc_j);

            return h * _scaleY;
        }

        public override void Draw(Matrix view, Matrix projection)
        {
            _terrainRenderer.Draw(this, view, projection);
        }
    }
}