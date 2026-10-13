using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using TGC.MonoGame.TP.ModelsInScene;
using TGC.MonoGame.TP.Renderers;
using TGC.MonoGame.TP.Colliders;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using BepuPhysics;
using NumericVector3 = System.Numerics.Vector3;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.Trees
{
    public class Tree : ModelInScene
    {
        public Tree(
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
                _midpoint.Y + _heightLocal * 0.35f,
                _midpoint.Z
            );

            float radius = _widthLocal * 0.08f;

            _hitbox = new CapsuleHitbox(
                start,
                end,
                radius
            );
        }
        public override void CreateCollider(Simulation simulation, BufferPool _bufferPool, Effect effect, GraphicsDevice graphicsDevice)
        {
            var center = new NumericVector3(this._position.X, this._position.Y + this.Height / 2f, this._position.Z);
            this._collider = new StaticBoxCollider(20f, Height, 20f, center, simulation, _bufferPool, effect, graphicsDevice);
        }
    }
}