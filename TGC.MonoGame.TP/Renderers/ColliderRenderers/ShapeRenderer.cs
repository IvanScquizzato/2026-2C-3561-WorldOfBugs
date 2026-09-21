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
namespace TGC.MonoGame.TP.ShapeRenderers
{
    public abstract class ShapeRenderer
    {
        protected VertexBuffer vertexBuffer;
        protected IndexBuffer indexBuffer;
        protected int _type;
        protected GraphicsDevice _graphicsDevice;
        protected Effect _effect;
        public ShapeRenderer(GraphicsDevice graphicsDevice, Effect effect, int type)
        {
            _graphicsDevice = graphicsDevice;
            _effect = effect;
            _type = type;
            Load();
        }
        public void Draw(Matrix worldNotScaled, Matrix view, Matrix projection, CompoundChild child, Simulation simulation)
        {
            if (_type == child.ShapeIndex.Type)
            {
                Matrix world = GetWorldOfShape(worldNotScaled, child.ShapeIndex, simulation);
                DrawWithWorld(world, view, projection);
            }
        }
        protected abstract Matrix GetWorldOfShape(Matrix worldNotScaled, TypedIndex shapeIndex, Simulation simulation);
        protected abstract void DrawWithWorld(Matrix world, Matrix view, Matrix projection);
        protected virtual void DrawWithWorldAndLines(Matrix world, Matrix view, Matrix projection, int lines)
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
        protected abstract void Load();
    }
}