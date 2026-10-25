using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.ModelsInScene
{
    public class CompoundHitboxRenderer
    {
        public CompoundHitboxRenderer() { }

        public static void Draw(ModelInScene model, CompoundHitbox compoundHitbox, Matrix world, Matrix view, Matrix projection)
        {
            foreach (Hitbox hitbox in compoundHitbox.hitboxes)
            {
                hitbox.Draw(model, world, view, projection);
            }
        }

    }
}