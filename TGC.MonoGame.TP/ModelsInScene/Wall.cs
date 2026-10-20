
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.ModelsInScene;
using TGC.MonoGame.TP.Renderers;
using TGC.MonoGame.TP.Colliders;
using BepuPhysics;
using BepuUtilities.Memory;
using NumericVector3 = System.Numerics.Vector3;

namespace TGC.MonoGame.TP.Walls
{
    public class Wall : ModelInScene
    {
        public Wall(
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
            strength = 6000000f;
        }

        public override void CreateHitbox(
            Effect effect,
            GraphicsDevice graphicsDevice
        )
        {
            // Dimensiones originales del modelo
            Vector3 halfSize = new Vector3(
                _widthLocal / 2f,
                _heightLocal / 2f,
                _depthLocal / 2f
            );

            // Hitbox rectangular en coordenadas locales
            _hitbox = new BoxHitbox(
                _midpoint - halfSize,
                _midpoint + halfSize,
                effect,
                graphicsDevice
            );
        }

        public override void CreateCollider(
            Simulation simulation,
            BufferPool bufferPool,
            Effect effect,
            GraphicsDevice graphicsDevice
        )
        {
            // Centro del modelo transformado al mundo
            Vector3 center = Vector3.Transform(
                _midpoint,
                _world
            );

            NumericVector3 colliderPosition = new NumericVector3(
                center.X,
                center.Y,
                center.Z
            );

            // Dimensiones reales con la escala aplicada
            float width = MathF.Max(MathF.Abs(Width), 0.01f);
            float height = MathF.Max(MathF.Abs(Height), 0.01f);
            float depth = MathF.Max(MathF.Abs(Depth), 0.01f);

            _collider = new StaticBoxCollider(
                width,
                height,
                depth,
                colliderPosition,
                simulation,
                bufferPool,
                effect,
                graphicsDevice
            );
        }
    }
}
