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

namespace TGC.MonoGame.TP.ColliderRenderers
{
    public abstract class ShapeColliderRenderer
    {
        protected ShapeRenderer _shapeRenderer;

        public void Draw(Collider collider, Matrix view, Matrix projection)
        {
            if (collider.GetShape().Type != GetShapeId())
            {
                return;
            }
            Matrix worldMatrix = GetWorldMatrix(collider);

            _shapeRenderer.Draw(worldMatrix, view, projection);
        }
        protected abstract Matrix GetWorldMatrix(Collider collider);
        public abstract int GetShapeId();

    }
}