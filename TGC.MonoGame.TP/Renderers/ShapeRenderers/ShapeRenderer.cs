using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Renderers;
using System.Collections.Concurrent;
using BepuPhysics;
using TGC.MonoGame.TP.Tanks;
using TGC.MonoGame.TP.Physics.Bepu;
using NumericVector3 = System.Numerics.Vector3;
using TGC.MonoGame.Utils;
using System.Diagnostics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using TGC.MonoGame.TP.Colliders;
using TGC.MonoGame.TP.ShapeRenderers;
namespace TGC.MonoGame.TP.ShapeRenderers
{
    public abstract class ShapeRenderer
    {
        protected VertexBuffer vertexBuffer;
        protected IndexBuffer indexBuffer;

        protected GraphicsDevice _graphicsDevice;
        protected Effect _effect;

        public ShapeRenderer(GraphicsDevice graphicsDevice, Effect effect)
        {
            _graphicsDevice = graphicsDevice;
            _effect = effect;
            Load();
        }
        protected abstract void Load();
        public abstract void Draw(Matrix world, Matrix view, Matrix projection);
        protected void DrawWithLines(Matrix world, Matrix view, Matrix projection, int lines)
        {
            _effect.Parameters["World"].SetValue(world);
            _effect.Parameters["View"].SetValue(view);
            _effect.Parameters["Projection"].SetValue(projection);
            _effect.Parameters["DiffuseColor"].SetValue(Color.LimeGreen.ToVector3());
            _effect.CurrentTechnique = _effect.Techniques["SolidColorDrawing"];

            _graphicsDevice.SetVertexBuffer(vertexBuffer);
            _graphicsDevice.Indices = indexBuffer;

            foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                // Si usas la función de abajo, genera 16 segmentos (16*3 = 48 líneas)
                _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, lines);
            }
        }
    }
}