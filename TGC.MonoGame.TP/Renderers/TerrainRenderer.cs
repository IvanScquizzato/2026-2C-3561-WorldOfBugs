using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TGC.MonoGame.TP.ModelsInScene;
using TGC.MonoGame.TP.Terrain;
namespace TGC.MonoGame.TP.Renderers
{
    public class TerrainRenderer
    {
        public GraphicsDevice _graphicsDevice { get; set; }
        public Effect _effect { get; set; }

        public TerrainRenderer(GraphicsDevice graphicsDevice, Effect effect)
        {
            _graphicsDevice = graphicsDevice;
            _effect = effect;
        }

        public void Draw(SimpleTerrain terrain, Matrix view, Matrix projection)
        {
            var graphicsDevice = _effect.GraphicsDevice;

            _effect.Parameters["texColorMap"].SetValue(terrain.colorMapTexture);
            _effect.Parameters["texDiffuseMap"].SetValue(terrain.terrainTexture);
            _effect.Parameters["texDiffuseMap2"].SetValue(terrain.terrainTexture2);
            _effect.Parameters["World"].SetValue(terrain._world);
            _effect.Parameters["View"].SetValue(view);
            _effect.Parameters["Projection"].SetValue(projection);

            graphicsDevice.SetVertexBuffer(terrain.vbTerrain);

            // Render con shader
            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, terrain.vbTerrain.VertexCount / 3);
            }
        }
    }
}