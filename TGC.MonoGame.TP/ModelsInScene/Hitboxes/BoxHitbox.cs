using System.Runtime.InteropServices.Marshalling;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.ModelsInScene
{
    public class BoxHitbox : Hitbox
    {
        private readonly Matrix _localTransform;
        public Vector3 Min { get; private set; }
        public Vector3 Max { get; private set; }
        public BoxHitboxRenderer boxHitboxRenderer { get; set; }
        public BoxHitbox(Vector3 min, Vector3 max, Effect hitboxEffect, GraphicsDevice graphicsDevice)
            : this(min, max, Matrix.Identity, hitboxEffect, graphicsDevice)
        {
        }

        public BoxHitbox(Vector3 min, Vector3 max, Matrix localTransform, Effect hitboxEffect, GraphicsDevice graphicsDevice)
        {
            Min = min;
            Max = max;
            _localTransform = localTransform;
            boxHitboxRenderer = new BoxHitboxRenderer(hitboxEffect, graphicsDevice);
        }

        public override bool CheckContainsPoint(
            Vector3 worldPoint,
            Matrix world
        )
        {
            Matrix inverseWorld = Matrix.Invert(_localTransform * world);

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
            Matrix transform = _localTransform * world;
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
                    Vector3.Transform(corners[i], transform);
            }

            return corners;
        }
        public override void Draw(ModelInScene model, Matrix world, Matrix view, Matrix projection)
        {
            boxHitboxRenderer.Draw(this, world, view, projection);
        }
    }
}
