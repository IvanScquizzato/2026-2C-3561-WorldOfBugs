using System;
using System.Numerics;
using System.Runtime.CompilerServices;

using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;

using BepuUtilities;
using NumericVector3 = System.Numerics.Vector3;
using Vector3 = Microsoft.Xna.Framework.Vector3;
namespace TGC.MonoGame.Utils
{

    public static class UtilsClass
    {
        public static NumericVector3 ToNumericVector(Vector3 vector3)
        {
            return new NumericVector3(vector3.X, vector3.Y, vector3.Z);
        }
    }
}