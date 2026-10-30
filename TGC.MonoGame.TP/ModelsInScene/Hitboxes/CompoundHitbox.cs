using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.ModelsInScene
{
    public class CompoundHitbox : Hitbox
    {
        public List<Hitbox> hitboxes { get; set; } = new List<Hitbox>();
        private Vector3 lastWorldPoint = Vector3.Zero;
        private Matrix lastWorld = Matrix.Identity;
        private Hitbox lastHitbox = null;

        public CompoundHitbox()
        {
        }

        public override bool CheckContainsPoint(
            Vector3 worldPoint,
            Matrix world
        )
        {
            lastWorldPoint = worldPoint;
            lastWorld = world;
            Hitbox hitbox = hitboxes.Find(b => b.CheckContainsPoint(worldPoint, world));
            lastHitbox = hitbox;
            return hitbox != null;
        }

        public override Hitbox GetHitboxInContact(Vector3 worldPoint, Matrix world)
        {
            if (lastWorldPoint == worldPoint && lastWorld == world && lastHitbox != null)
            {
                return lastHitbox;
            }
            else
            {
                return hitboxes.Find(b => b.CheckContainsPoint(worldPoint, world));
            }
        }
        public override void Draw(ModelInScene model, Matrix world, Matrix view, Matrix projection)
        {
            CompoundHitboxRenderer.Draw(model, this, world, view, projection);
        }
        public BoxHitbox CreateSubHitbox(
        float halfWidth, float halfHeight, float halfDepth,
        float xCenter, float yCenter, float zCenter,
        Effect effect, GraphicsDevice graphicsDevice)
        {
            Vector3 halfSize = new Vector3(
                halfWidth,
                halfHeight,
                halfDepth
            );

            Vector3 center = new Vector3(
                xCenter,
                yCenter,
                zCenter
            );
            BoxHitbox newBoxHitbox = new BoxHitbox(center - halfSize, center + halfSize, effect, graphicsDevice);
            hitboxes.Add(newBoxHitbox);
            return newBoxHitbox;
        }
        public void CreateSubHitboxSymmetricalToYZ(
        float halfWidth, float halfHeight, float halfDepth,
        float xCenter, float yCenter, float zCenter,
        Effect effect, GraphicsDevice graphicsDevice)
        {
            CreateSubHitbox(halfWidth, halfHeight, halfDepth, xCenter, yCenter, zCenter, effect, graphicsDevice);
            CreateSubHitbox(halfWidth, halfHeight, halfDepth, -xCenter, yCenter, zCenter, effect, graphicsDevice);
        }
    }
}