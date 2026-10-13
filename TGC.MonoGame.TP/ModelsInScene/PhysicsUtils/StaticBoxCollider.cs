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
using TGC.MonoGame.TP.ColliderRenderers;
using TGC.MonoGame.TP.ShapeRenderers;

namespace TGC.MonoGame.TP.Colliders
{
    public class StaticBoxCollider : Collider
    {
        public StaticBoxCollider(float width, float height, float depth, NumericVector3 position, Simulation simulation, BufferPool bufferPool, Effect effect, GraphicsDevice graphicsDevice) :
        base(simulation, bufferPool, effect, graphicsDevice)
        {
            this._isStatic = true;

            LoadBoxCollider(width, height, depth, position);
        }
        private void LoadBoxCollider(float width, float height, float depth, NumericVector3 position)
        {
            var shape = _simulation.Shapes.Add(new Box(width, height, depth));
            StaticHandle staticHandle = _simulation.Statics.Add(new StaticDescription(position, shape));
            _staticReference = _simulation.Statics.GetStaticReference(staticHandle);
        }
    }
}