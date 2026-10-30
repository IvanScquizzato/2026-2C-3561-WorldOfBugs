using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.ModelsInScene
{
    public abstract class Hitbox
    {
        private bool isDisabled = false;
        public bool ContainsPoint(
            Vector3 worldPoint,
            Matrix world
        )
        {
            if (isDisabled)
            {
                return false;
            }
            return CheckContainsPoint(worldPoint, world);
        }
        public abstract bool CheckContainsPoint(Vector3 worldPoint, Matrix world);
        public virtual Hitbox GetHitboxInContact(Vector3 worldPoint, Matrix world)
        {
            return this;
        }

        public abstract void Draw(ModelInScene model, Matrix world, Matrix view, Matrix projection);
        public void Remove()
        {
            isDisabled = true;
        }
    }
}