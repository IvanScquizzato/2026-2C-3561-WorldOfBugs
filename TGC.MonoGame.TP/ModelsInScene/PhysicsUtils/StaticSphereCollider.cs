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
    public class StaticSphereCollider : Collider
    {
        public StaticSphereCollider(float radius, NumericVector3 position, Simulation simulation, BufferPool bufferPool, Effect effect, GraphicsDevice graphicsDevice) :
        base(simulation, bufferPool, effect, graphicsDevice)
        {
            this._isStatic = true;
            LoadBoxCollider(radius, position);
        }
        private void LoadBoxCollider(float radius, NumericVector3 position)
        {
            var shape = _simulation.Shapes.Add(new Sphere(radius));
            StaticHandle staticHandle = _simulation.Statics.Add(new StaticDescription(position, shape));
            _staticReference = _simulation.Statics.GetStaticReference(staticHandle);
        }
    }
}