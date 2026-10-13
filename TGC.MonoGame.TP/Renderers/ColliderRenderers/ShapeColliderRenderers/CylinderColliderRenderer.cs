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
    public class CylinderColliderRenderer : ShapeColliderRenderer
    {

        public CylinderColliderRenderer(GraphicsDevice graphicsDevice, Effect effect)
        {
            _shapeRenderer = new CylinderRenderer(graphicsDevice, effect);
        }

        protected override Matrix GetWorldMatrix(Collider collider)
        {
            Matrix worldMatrix = collider.GetWorldMatrixNotScaled();
            ref Cylinder cylinderShape = ref collider._simulation.Shapes.GetShape<Cylinder>(collider.GetShape().Index);
            var physicsScale = new Vector3(cylinderShape.Radius * 2f, cylinderShape.Length, cylinderShape.Radius * 2f);
            return Matrix.CreateScale(physicsScale) * worldMatrix;
        }
        public override int GetShapeId() { return Cylinder.Id; }
    }
}