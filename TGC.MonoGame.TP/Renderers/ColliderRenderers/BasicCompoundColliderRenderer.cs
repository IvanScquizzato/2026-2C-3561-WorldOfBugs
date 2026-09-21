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
    public class BasicCompoundColliderRenderer : ColliderRenderer
    {
        public BasicChildColliderRenderer childRenderer;
        public BasicCompoundColliderRenderer(GraphicsDevice graphicsDevice, Effect effect)
        {
            childRenderer = new BasicChildColliderRenderer(
                new List<ShapeRenderer>{
                    new BoxRenderer(graphicsDevice,effect),
                    new CylinderRenderer(graphicsDevice,effect)}
            );
        }
        public void Draw(Collider collider, Matrix view, Matrix projection)
        {
            var bodyReference = collider._bodyReference;
            var simulation = collider._simulation;

            TypedIndex shapeIndex = bodyReference.Collidable.Shape;
            ref Compound compoundShape = ref simulation.Shapes.GetShape<Compound>(shapeIndex.Index);

            var bepuPose = bodyReference.Pose;
            Vector3 physicsPos = new Vector3(bepuPose.Position.X, bepuPose.Position.Y, bepuPose.Position.Z);
            Quaternion physicsRot = new Quaternion(bepuPose.Orientation.X, bepuPose.Orientation.Y, bepuPose.Orientation.Z, bepuPose.Orientation.W);

            for (var i = 0; i < compoundShape.Children.Length; i++)
            {
                childRenderer.Draw(bepuPose, compoundShape.Children[i], view, projection, simulation);
            }
        }
    }
}