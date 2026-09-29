using System;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using TGC.MonoGame.TP.ModelsInScene;
using TGC.MonoGame.TP.Renderers;

namespace TGC.MonoGame.TP.Missile
{
    public class Missil : ModelInScene
    {
        public Vector3 Direction { get; private set; }

        public float Speed { get; set; } = 1500f;

        public bool IsAlive { get; set; } = true;

        public Missil(
            ContentManager content,
            string modelPath,
            Vector3 position,
            Matrix rotation,
            Vector3 scale,
            IModelInSceneRenderer renderer,
            Vector3 direction
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
            Direction = Vector3.Normalize(direction);
            
            Matrix modelCorrection =
                Matrix.CreateRotationX(-MathHelper.PiOver2);

            _rotation =
                modelCorrection *
                rotation;
        }

        public void Update(GameTime gameTime)
        {
            float deltaTime =
                (float)gameTime.ElapsedGameTime.TotalSeconds;

            _position +=
                Direction *
                Speed *
                deltaTime;
        }
    }
}