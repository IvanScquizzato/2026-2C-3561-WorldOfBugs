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
        public TankCollider(Tank tank, Simulation simulation, Dictionary<CollidableReference, object> physicsToGameObjects, BufferPool bufferPool, Effect effect, GraphicsDevice graphicsDevice) :
        base(simulation, physicsToGameObjects, bufferPool, effect, graphicsDevice, new BasicCompoundColliderRenderer(graphicsDevice, effect))
        {
            _tank = tank;
            CreateTankCollider();
        }
        public void CreateTankCollider()
        {
            var tankBodyShape = new Box(_tank.Width * 0.58f, _tank.Height * 0.3f, _tank.Depth * 0.5f);
            var tankCenterShape = new Box(_tank.Width * 0.9f, _tank.Height * 0.1f, _tank.Depth * 0.57f);
            var tankUpperShape = new Box(_tank.Width * 0.9f, _tank.Height * 0.18f, _tank.Depth * 0.51f);
            var trackShape = new Box(_tank.Width * 0.17f, _tank.Height * 0.3f, _tank.Depth * 0.37f);

            float trackRadius = _tank.Height * 0.18f;
            float trackWidth = _tank.Width * 0.17f;
            var trackWheelShape = new Cylinder(trackRadius, trackWidth);

            // 2. Distribuir la masa total del tanque entre sus componentes (ejemplo aproximado por volumen)
            float totalMass = _tank._mass;
            float bodyMass = totalMass * 0.40f;   // 40% al cuerpo
            float centerMass = totalMass * 0.20f; // 20% al centro
            float upperMass = totalMass * 0.10f;  // 10% a la torreta/parte superior
            float trackMass = totalMass * 0.10f;  // 10% a cada oruga principal (x2)
            float smallTrackMass = totalMass * 0.025f; // 2.5% a cada parte pequeña de oruga (x4)

            // 3. Construir el Compound
            using var compoundBuilder = new CompoundBuilder(_bufferPool, _simulation.Shapes, 10);

            compoundBuilder.Add(tankBodyShape, new RigidPose(new NumericVector3(0, -_tank.Height * 0.2f, 0)), bodyMass);
            compoundBuilder.Add(tankCenterShape, new RigidPose(new NumericVector3(0, -_tank.Height * 0.1f, -_tank.Depth * 0.03f)), centerMass);
            compoundBuilder.Add(tankUpperShape, new RigidPose(new NumericVector3(0, _tank.Height * 0.04f, -_tank.Depth * 0.08f)), upperMass);

            // Orugas principales
            compoundBuilder.Add(trackShape, new RigidPose(new NumericVector3(-_tank.Width * 0.39f, -_tank.Height * 0.33f, 0)), trackMass);
            compoundBuilder.Add(trackShape, new RigidPose(new NumericVector3(_tank.Width * 0.39f, -_tank.Height * 0.33f, 0)), trackMass);

            var wheelRotation = System.Numerics.Quaternion.CreateFromAxisAngle(new NumericVector3(0, 0, 1), MathHelper.PiOver2);

            // Orugas traseras (Ahora son Cilindros redondeados)
            // Nota: Usamos la MISMA altura Y (-0.33f) que el centro para que encajen a la perfección.
            compoundBuilder.Add(trackWheelShape, new RigidPose(new NumericVector3(_tank.Width * 0.39f, -_tank.Height * 0.29f, -_tank.Depth * 0.22f), wheelRotation), smallTrackMass);
            compoundBuilder.Add(trackWheelShape, new RigidPose(new NumericVector3(-_tank.Width * 0.39f, -_tank.Height * 0.29f, -_tank.Depth * 0.22f), wheelRotation), smallTrackMass);

            // Orugas delanteras (Ahora son Cilindros redondeados)
            compoundBuilder.Add(trackWheelShape, new RigidPose(new NumericVector3(_tank.Width * 0.37f, -_tank.Height * 0.27f, _tank.Depth * 0.20f), wheelRotation), smallTrackMass);
            compoundBuilder.Add(trackWheelShape, new RigidPose(new NumericVector3(-_tank.Width * 0.37f, -_tank.Height * 0.27f, _tank.Depth * 0.20f), wheelRotation), smallTrackMass);

            // 4. USAR BUILD DYNAMIC COMPOUND!
            // Esto calculará la inercia perfecta combinada y te devolverá el nuevo centro de gravedad (centerOfMass)
            compoundBuilder.BuildDynamicCompound(out var compoundChildren, out var compoundInertia, out var centerOfMass);

            var compoundShape = new Compound(compoundChildren);
            var compoundIndex = _simulation.Shapes.Add(compoundShape);

            // 5. Compensar la posición inicial con el centro de masa calculado
            var basePosition = new NumericVector3(_tank._position.X, _tank._position.Y, _tank._position.Z);
            var bodyPose = new RigidPose(basePosition + centerOfMass); // ¡CRUCIAL!

            // 6. Crear el cuerpo dinámico usando la nueva inercia combinada
            var tankBoxHandle = _simulation.Bodies.Add(BodyDescription.CreateDynamic(
                bodyPose,
                compoundInertia, // Pasamos la inercia calculada por el builder, NO la del bodyShape individual
                new CollidableDescription(compoundIndex, 0.1f),
                new BodyActivityDescription(0.01f)
            ));

            _bodyReference = _simulation.Bodies.GetBodyReference(tankBoxHandle);
            _centerOfMass = centerOfMass;
            var tankCollidableRef = new CollidableReference(CollidableMobility.Dynamic, tankBoxHandle);
            _physicsToGameObjects.Add(tankCollidableRef, _tank);
        }
    }
}