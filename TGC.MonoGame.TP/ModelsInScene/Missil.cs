using System;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using TGC.MonoGame.TP.ModelsInScene;
using TGC.MonoGame.TP.Renderers;
using TGC.MonoGame.TP.Materials;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
namespace TGC.MonoGame.TP.Missile
{
    public class Missil : ModelInScene
    {
        private static readonly Dictionary<Texture, Texture> _normalMaps =
        new Dictionary<Texture, Texture>();

        public override Dictionary<Texture, Texture> _normalMapsOfTexture
        => _normalMaps;
        public override Dictionary<string, string> _namesNormalMapsOfNameTexture { get; set; } = new Dictionary<string, string>
{
    { "Models\\missile\\textures\\rocket2_body_BaseColor_0", "Models\\missile\\textures\\rocket2_body_Normal" }
};

        public override Dictionary<string, IMaterial> _materialOfNameTexture { get; set; } = new Dictionary<string, IMaterial>
{
    { "Models\\missile\\textures\\rocket2_body_BaseColor_0", Metal }
};
        public Vector3 Direction { get; private set; }

        public float Speed { get; set; } = 1500f;

        private float maxLifetimeSecs = 6f;
        private float lifeTime = 0f;
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
            lifeTime += deltaTime;
            if (lifeTime >= maxLifetimeSecs)
            {
                _isDisabled = true;
                Destroy();
            }

        }
        public void CollidedWithObject(ModelInScene model)
        {
            // si toca dos cosas en el mismo frame la explosion suena una sola vez
            if (!_isDisabled)
            {
                TGCGame.ExplosionSound.Play();
                Destroy();
            }
            _isDisabled = true;
        }
    }
}