using System;
using BepuPhysics;
using BepuUtilities.Memory;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.ColliderRenderers;
using TGC.MonoGame.TP.Colliders;
using TGC.MonoGame.TP.ModelsInScene;
using TGC.MonoGame.TP.Renderers;
using NumericVector = System.Numerics.Vector3;

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
        public override void CreateCollider(Simulation simulation, BufferPool bufferPool, Effect effect, GraphicsDevice graphicsDevice)
        {
            var center = new NumericVector(this._position.X, this._position.Y + this.Height / 2f, this._position.Z);
            this._collider = new StaticBoxCollider(Width - 90f, Height, Depth - 90f, center, simulation, bufferPool, effect, graphicsDevice);
        }
    }
}