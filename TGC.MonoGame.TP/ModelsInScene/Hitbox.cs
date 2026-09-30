using Microsoft.Xna.Framework;

namespace TGC.MonoGame.TP.ModelsInScene
{
    public abstract class Hitbox
    {
        public abstract bool ContainsPoint(
            Vector3 worldPoint,
            Matrix world
        );
    }
}