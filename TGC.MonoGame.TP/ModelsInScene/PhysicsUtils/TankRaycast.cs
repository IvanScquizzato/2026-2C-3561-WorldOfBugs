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

namespace TGC.MonoGame.TP.TankRaycasts
{
    public class TankRaycast
    {
        public Vector3 _positionLocal;
        public Vector3 _directionLocal;
        public Simulation _simulation;
        public TankRaycast(Vector3 positionLocal, Vector3 directionLocal, Simulation simulation)
        {
            _positionLocal = positionLocal;
            _directionLocal = directionLocal;
            _simulation = simulation;
        }
        public bool CalculateRaycast(Tank tank)
        {
            var position = UtilsClass.ToNumericVector(Vector3.Transform(_positionLocal, tank._world));
            var direction = UtilsClass.ToNumericVector(DirectionGlobal(tank));
            float maxDistance = tank.Height * 0.1f;

            var hitHandler = new ClosestHitHandler();

            hitHandler.TankToIgnore = tank._bodyReference.Handle;
            _simulation.RayCast(position, direction, maxDistance, ref hitHandler);
            //Debug.WriteLine($"Ray Pos: {position}, Dir: {direction}");
            if (hitHandler.Hit)
            {
                return true;
            }
            return false;
        }
        public Vector3 PositionGlobalRelativeToCenter(Tank tank)
        {
            return Vector3.Transform(_positionLocal, Matrix.CreateScale(tank._scale) * tank._rotation);
        }
        public Vector3 DirectionGlobal(Tank tank)
        {
            return Vector3.Normalize(Vector3.Transform(_directionLocal, Matrix.CreateScale(tank._scale) * tank._rotation));
        }
    }
}
