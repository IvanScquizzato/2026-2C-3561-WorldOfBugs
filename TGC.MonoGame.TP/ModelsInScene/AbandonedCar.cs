using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.ModelsInScene;
using TGC.MonoGame.TP.Renderers;
namespace TGC.MonoGame.TP.Car
{
    public class AbandonedCar : ModelInScene
    {
        public AbandonedCar(ContentManager Content, String modelPath, Vector3 position, Matrix rotation, Vector3 scale, IModelInSceneRenderer renderer)
            : base(Content, modelPath, position, rotation, scale, renderer, 1f)
        {
            Vector3 halfSize = new Vector3(
                _widthLocal * 0.49f,
                _heightLocal * 0.4f,
                _depthLocal * 0.4f
            );

            Vector3 center = new Vector3(
                _midpoint.X,
                _midpoint.Y * 0.8f,
                _midpoint.Z 
            );

            _hitbox = new BoxHitbox(
                center - halfSize,
                center + halfSize
            );
        }
    }
}