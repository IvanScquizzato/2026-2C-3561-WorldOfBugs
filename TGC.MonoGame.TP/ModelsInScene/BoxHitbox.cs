using Microsoft.Xna.Framework;

namespace TGC.MonoGame.TP.ModelsInScene
{
    public class BoxHitbox : Hitbox
    {
        public Vector3 Min { get; private set; }
        public Vector3 Max { get; private set; }

        public BoxHitbox(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }

        public override bool ContainsPoint(
            Vector3 worldPoint,
            Matrix world
        )
        {
            Matrix inverseWorld = Matrix.Invert(world);

            Vector3 localPoint =
                Vector3.Transform(worldPoint, inverseWorld);

            return
                localPoint.X >= Min.X &&
                localPoint.X <= Max.X &&
                localPoint.Y >= Min.Y &&
                localPoint.Y <= Max.Y &&
                localPoint.Z >= Min.Z &&
                localPoint.Z <= Max.Z;
        }

        public Vector3[] GetWorldCorners(Matrix world)
        {
            Vector3[] corners =
            {
                new Vector3(Min.X, Min.Y, Min.Z),
                new Vector3(Max.X, Min.Y, Min.Z),
                new Vector3(Max.X, Max.Y, Min.Z),
                new Vector3(Min.X, Max.Y, Min.Z),

                new Vector3(Min.X, Min.Y, Max.Z),
                new Vector3(Max.X, Min.Y, Max.Z),
                new Vector3(Max.X, Max.Y, Max.Z),
                new Vector3(Min.X, Max.Y, Max.Z)
            };

            for (int i = 0; i < corners.Length; i++)
            {
                corners[i] =
                    Vector3.Transform(corners[i], world);
            }

            return corners;
        }
    }
}