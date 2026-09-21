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
using TGC.MonoGame.TP.ColliderRenderers;
namespace TGC.MonoGame.TP.Colliders
{
    public abstract class Collider
    {
        public BodyReference _bodyReference;
        public Vector3 _centerOfMass;
        public Simulation _simulation;
        protected Dictionary<CollidableReference, object> _physicsToGameObjects;
        protected BufferPool _bufferPool;
        protected Effect _effect;
        protected GraphicsDevice _graphicsDevice;
        protected ColliderRenderer _colliderRenderer;
        public Collider(Simulation simulation, Dictionary<CollidableReference, object> physicsToGameObjects, BufferPool bufferPool, Effect effect, GraphicsDevice graphicsDevice, ColliderRenderer colliderRenderer)
        {
            _simulation = simulation;
            _physicsToGameObjects = physicsToGameObjects;
            _bufferPool = bufferPool;
            _effect = effect;
            _graphicsDevice = graphicsDevice;
            _colliderRenderer = colliderRenderer;
        }
        public void Draw(Matrix view, Matrix projection)
        {
            _colliderRenderer.Draw(this, view, projection);
        }
    }
}