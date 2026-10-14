using Microsoft.Xna.Framework;
using BepuPhysics;
using System.Collections.Generic;
using TGC.MonoGame.TP.Tanks;
using NumericVector3 = System.Numerics.Vector3;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.ColliderRenderers;

namespace TGC.MonoGame.TP.Colliders
{
    public class TankCollider : Collider
    {
        private Tank _tank;
        public TankCollider(Tank tank, Simulation simulation, BufferPool bufferPool, Effect effect, GraphicsDevice graphicsDevice) :
        base(simulation, bufferPool, effect, graphicsDevice)
        {
            _tank = tank;
            CreateTankCollider();
        }

        public void CreateTankCollider()
        {
            var tankBodyShape = new Box(_tank.Width * 0.58f, _tank.Height * 0.3f, _tank.Depth * 0.5f);
            var tankCenterShape = new Box(_tank.Width * 0.9f, _tank.Height * 0.1f, _tank.Depth * 0.57f);
            var tankUpperShape = new Box(_tank.Width * 0.9f, _tank.Height * 0.18f, _tank.Depth * 0.51f);


            float totalMass = _tank._mass;
            float bodyMass = totalMass * 0.60f;   // 60% al cuerpo principal
            float centerMass = totalMass * 0.25f; // 25% al centro
            float upperMass = totalMass * 0.15f;  // 15% a la torreta

            using var compoundBuilder = new CompoundBuilder(_bufferPool, _simulation.Shapes, 3);

            compoundBuilder.Add(tankBodyShape, new RigidPose(new NumericVector3(0, -_tank.Height * 0.1f, 0)), bodyMass);
            compoundBuilder.Add(tankCenterShape, new RigidPose(new NumericVector3(0, -_tank.Height * 0.1f, -_tank.Depth * 0.03f)), centerMass);
            compoundBuilder.Add(tankUpperShape, new RigidPose(new NumericVector3(0, _tank.Height * 0.04f, -_tank.Depth * 0.08f)), upperMass);

            compoundBuilder.BuildDynamicCompound(out var compoundChildren, out var compoundInertia, out var centerOfMass);


            float offsetCentroDeMasa = _tank.Height * 0.35f; // Ajusta este valor a gusto. Mientras mayor sea, más in-volcable será.
            var artificialOffset = new NumericVector3(0, offsetCentroDeMasa, 0);

            for (int i = 0; i < compoundChildren.Length; ++i)
            {
                compoundChildren[i].LocalPose.Position += artificialOffset;
            }

            centerOfMass -= artificialOffset;


            var compoundShape = new Compound(compoundChildren);
            var compoundIndex = _simulation.Shapes.Add(compoundShape);

            // 7. Compensar la posición inicial
            var basePosition = new NumericVector3(_tank._position.X, _tank._position.Y, _tank._position.Z);
            var bodyPose = new RigidPose(basePosition + centerOfMass);

            // 8. Crear el cuerpo dinámico
            var tankBoxHandle = _simulation.Bodies.Add(BodyDescription.CreateDynamic(
                bodyPose,
                compoundInertia,
                new CollidableDescription(compoundIndex, 0.1f),
                new BodyActivityDescription(0.01f)
            ));

            _bodyReference = _simulation.Bodies.GetBodyReference(tankBoxHandle);
            _centerOfMass = centerOfMass;
            var tankCollidableRef = new CollidableReference(CollidableMobility.Dynamic, tankBoxHandle);
        }
    }
}