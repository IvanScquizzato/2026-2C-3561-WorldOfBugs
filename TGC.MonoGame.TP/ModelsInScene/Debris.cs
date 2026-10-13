using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.ModelsInScene;
using TGC.MonoGame.TP.Renderers;
using BepuPhysics;
using BepuUtilities.Memory;
using NumericVector3 = System.Numerics.Vector3;
using TGC.MonoGame.TP.Colliders;

namespace TGC.MonoGame.TP.Debri
{
    public class Debris : ModelInScene
    {
        public Debris(ContentManager Content, String modelPath, Vector3 position, Matrix rotation, Vector3 scale, IModelInSceneRenderer renderer)
            : base(Content, modelPath, position, rotation, scale, renderer, 1f)
        {
            Vector3 halfSize = new Vector3(
                _widthLocal * 0.35f,
                _heightLocal * 0.24f,
                _depthLocal * 0.3f
            );

            Vector3 center = new Vector3(
                _midpoint.X,
                _midpoint.Y * 0.8f,
                _midpoint.Z * -0.1f
            );

            _hitbox = new BoxHitbox(
                center - halfSize,
                center + halfSize
            );
        }
        public override void CreateCollider(Simulation simulation, BufferPool bufferPool, Effect effect, GraphicsDevice graphicsDevice)
        {
            var center = new NumericVector3(_position.X, _position.Y - 165f, _position.Z);
            this._collider = new StaticSphereCollider(350f, center, simulation, bufferPool, effect, graphicsDevice);
        }
    }
}