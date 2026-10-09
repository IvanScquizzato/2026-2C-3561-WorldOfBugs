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
using TGC.MonoGame.TP.Forces;

namespace TGC.MonoGame.TP.TankRaycasts
{
    public class TankRaycast
    {
        public Vector3 _positionLocal { get; set; }
        public Vector3 _directionLocal;
        public Simulation _simulation;
        public TankRaycast(Vector3 positionLocal, Vector3 directionLocal, Simulation simulation)
        {
            _positionLocal = positionLocal;
            _directionLocal = directionLocal;
            _simulation = simulation;
        }

        private float distanceToGround(Tank tank)
        {
            var position = UtilsClass.ToNumericVector(Vector3.Transform(_positionLocal, tank._world));
            var direction = UtilsClass.ToNumericVector(DirectionGlobal(tank));
            var backoff = -direction * tank.Height * 0.3f;

            var hitHandler = new ClosestHitHandler();

            hitHandler.TankToIgnore = tank._collider._bodyReference.Handle;
            _simulation.RayCast(position + backoff, direction, tank.desiredHeight, ref hitHandler);
            if (hitHandler.Hit)
            {
                return hitHandler.T;
            }
            return float.MaxValue; // No se detectó colisión
        }
        public Vector3 PositionGlobalRelativeToCenter(Tank tank)
        {
            return Vector3.Transform(_positionLocal, Matrix.CreateScale(tank._scale) * tank._rotation);
        }
        public Vector3 DirectionGlobal(Tank tank)
        {
            return Vector3.Normalize(Vector3.Transform(_directionLocal, Matrix.CreateScale(tank._scale) * tank._rotation));
        }
        public Force CalculateSuspensionForce(Tank tank)
        {
            float desiredHeight = tank.desiredHeight;
            float distanceToGround = this.distanceToGround(tank);

            if (distanceToGround >= desiredHeight)
                return null;

            float suspension = tank.suspension;
            float cushioning = tank.cushioning;

            Vector3 velocityOfCenterMass = tank._velocity;
            Vector3 angularVelocity = tank._angularVelocity;
            Vector3 worldPosition = this.PositionGlobalRelativeToCenter(tank);
            Vector3 normal = -this.DirectionGlobal(tank);

            Vector3 forceVector = MathF.Max(0, suspension * (desiredHeight - distanceToGround) - cushioning * Vector3.Dot(velocityOfCenterMass + Vector3.Cross(angularVelocity, worldPosition), normal)) * normal;
            if (forceVector.Length() == 0f) return null;

            return new Force(forceVector, worldPosition);
        }
    }
}
