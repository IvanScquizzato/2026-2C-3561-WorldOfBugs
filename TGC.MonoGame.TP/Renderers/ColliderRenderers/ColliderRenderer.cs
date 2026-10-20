using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Renderers;
using System.Collections.Concurrent;
using BepuPhysics;
using TGC.MonoGame.TP.Tanks;
using TGC.MonoGame.TP.Physics.Bepu;
using NumericVector3 = System.Numerics.Vector3;
using TGC.MonoGame.Utils;
using System.Diagnostics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using TGC.MonoGame.TP.Colliders;
using System.Linq;
namespace TGC.MonoGame.TP.ColliderRenderers
{
    public class ColliderRenderer
    {
        GraphicsDevice _graphicsDevice;
        Effect _effect;
        private List<ShapeColliderRenderer> _shapeColliderRenderers;
        public ColliderRenderer(GraphicsDevice graphicsDevice, Effect effect)
        {
            _graphicsDevice = graphicsDevice;
            _effect = effect;
            _shapeColliderRenderers = new List<ShapeColliderRenderer>
            {
                new BoxColliderRenderer(_graphicsDevice, _effect),
                new CylinderColliderRenderer(_graphicsDevice, _effect),
                new SphereColliderRenderer(_graphicsDevice, effect)
            };
        }
        public void Draw(Collider collider, Matrix view, Matrix projection)
        {

            Simulation simulation = collider._simulation;

            int shapeIndex = collider.GetShape().Type;

            if (shapeIndex == Compound.Id)
            {
                for (var i = 0; i < collider._childColliders.Count; i++)
                {
                    Collider childCollider = collider._childColliders[i];
                    _shapeColliderRenderers.Find(sr => sr.GetShapeId() == childCollider.GetShape().Type).Draw(childCollider, view, projection);
                }
            }
            else
            {
                _shapeColliderRenderers.Find(sr => sr.GetShapeId() == collider.GetShape().Type).Draw(collider, view, projection);
            }

        }
    }
}