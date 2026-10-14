using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.ModelsInScene
{
    public abstract class HitboxRenderer
    {
        public Effect _effect { get; set; }
        public GraphicsDevice _graphicsDevice { get; set; }
        public HitboxRenderer(Effect effect, GraphicsDevice graphicsDevice)
        {
            _effect = effect;
            _graphicsDevice = graphicsDevice;
        }
        public abstract void Draw(Hitbox hitbox, Matrix view, Matrix projection);
    }
}