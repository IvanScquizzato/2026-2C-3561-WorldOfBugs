using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using NumericVector3 = System.Numerics.Vector3;

namespace TGC.MonoGame.TP.Colliders
{
    /// <summary>
    /// Colisionador de caja dinámico para objetos que reaccionan a la física (escombros, piedras, etc.)
    /// </summary>
    public class DynamicBoxCollider : Collider
    {
        public DynamicBoxCollider(float width, float height, float depth, float mass, NumericVector3 position, Simulation simulation, BufferPool bufferPool, Effect effect, GraphicsDevice graphicsDevice) :
            base(simulation, bufferPool, effect, graphicsDevice)
        {
            LoadDynamicBoxCollider(width, height, depth, mass, position);
        }

        private void LoadDynamicBoxCollider(float width, float height, float depth, float mass, NumericVector3 position)
        {
            var boxShape = new Box(width, height, depth);
            var shapeIndex = _simulation.Shapes.Add(boxShape);
            var inertia = boxShape.ComputeInertia(mass);

            var bodyHandle = _simulation.Bodies.Add(BodyDescription.CreateDynamic(
                new RigidPose(position),
                inertia,
                new CollidableDescription(shapeIndex, 0.1f),
                new BodyActivityDescription(0.01f)
            ));
            _bodyReference = _simulation.Bodies.GetBodyReference(bodyHandle);
        }
    }
}