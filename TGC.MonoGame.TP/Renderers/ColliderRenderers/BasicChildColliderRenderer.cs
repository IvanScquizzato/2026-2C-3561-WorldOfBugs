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
using System.Linq;
namespace TGC.MonoGame.TP.ColliderRenderers
{
    public class BasicChildColliderRenderer
    {
        private List<ShapeRenderer> _shapeRenderers = new List<ShapeRenderer>();
        public BasicChildColliderRenderer(List<ShapeRenderer> shapeRenderers)
        {
            _shapeRenderers = shapeRenderers;
        }
        public void Draw(RigidPose parentPose, CompoundChild child, Matrix view, Matrix projection, Simulation simulation)
        {
            Vector3 physicsPos = new Vector3(parentPose.Position.X, parentPose.Position.Y, parentPose.Position.Z);
            Quaternion physicsRot = new Quaternion(parentPose.Orientation.X, parentPose.Orientation.Y, parentPose.Orientation.Z, parentPose.Orientation.W);

            Vector3 localPos = new Vector3(child.LocalPose.Position.X, child.LocalPose.Position.Y, child.LocalPose.Position.Z);
            Quaternion localRot = new Quaternion(child.LocalPose.Orientation.X, child.LocalPose.Orientation.Y, child.LocalPose.Orientation.Z, child.LocalPose.Orientation.W);

            Vector3 realPhysicsPos = physicsPos + Vector3.Transform(localPos, physicsRot);

            Matrix debugWorldMatrixNotScaled =
                                      Matrix.CreateFromQuaternion(localRot) *
                                      Matrix.CreateFromQuaternion(physicsRot) *
                                      Matrix.CreateTranslation(realPhysicsPos);

            foreach (ShapeRenderer shapeRenderer in _shapeRenderers)
            {
                shapeRenderer.Draw(debugWorldMatrixNotScaled, view, projection, child, simulation);
            }
        }
    }
}