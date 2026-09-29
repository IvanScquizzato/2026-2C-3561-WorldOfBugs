using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using TGC.MonoGame.TP.ModelsInScene;
using TGC.MonoGame.TP.Renderers;

namespace TGC.MonoGame.TP.Monument
{
    public class Obelisc : ModelInScene
    {
        public Obelisc(
            ContentManager content,
            string modelPath,
            Vector3 position,
            Matrix rotation,
            Vector3 scale,
            IModelInSceneRenderer renderer
        )
            : base(
                content,
                modelPath,
                position,
                rotation,
                scale,
                renderer,
                1f
            )
        {
            Vector3 start = new Vector3(
                _midpoint.X,
                _midpoint.Y - _heightLocal * 0.5f,
                _midpoint.Z
            );

            Vector3 end = new Vector3(
                _midpoint.X,
                _midpoint.Y + _heightLocal * 0.5f,
                _midpoint.Z
            );

            float radius =
                MathF.Max(_widthLocal, _depthLocal) * 0.4f;

            _hitbox = new CapsuleHitbox(
                start,
                end,
                radius
            );
        }
    }
}