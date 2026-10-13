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
namespace TGC.MonoGame.TP.RuinHouse
{
    public class RuinHouse1 : ModelInScene
    {
        public RuinHouse1(ContentManager Content, String modelPath, Vector3 position, Matrix rotation, Vector3 scale, IModelInSceneRenderer renderer)
            : base(Content, modelPath, position, rotation, scale, renderer, 1f)
        {
            Vector3 halfSize = new Vector3(
                _widthLocal * 0.18f,
                _heightLocal * 0.3f,
                _depthLocal * 0.19f
            );

            Vector3 center = new Vector3(
                _midpoint.X,
                _midpoint.Y * 0.8f,
                _midpoint.Z * 0.37f
            );

            _hitbox = new BoxHitbox(
                center - halfSize,
                center + halfSize
            );
        }
        public override void CreateCollider(Simulation simulation, BufferPool bufferPool, Effect effect, GraphicsDevice graphicsDevice)
        {
            var center = new NumericVector3(_position.X, _position.Y + Height / 2f, _position.Z + 100f);
            this._collider = new StaticBoxCollider(Width - 1000, Height, Depth - 1200, center, simulation, bufferPool, effect, graphicsDevice);
        }
    }
}