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
using TGC.MonoGame.TP.Colliders;
using NumericVector = System.Numerics.Vector3;
using TGC.MonoGame.TP.Materials;
using TGC.MonoGame.TP.Missile;

namespace TGC.MonoGame.TP.Fences
{
    public class Fence : ModelInScene
    {
        private static readonly Dictionary<Texture, Texture> _normalMaps =
        new Dictionary<Texture, Texture>();
        public override Dictionary<Texture, Texture> _normalMapsOfTexture
        => _normalMaps;
        public override Dictionary<string, string> _namesNormalMapsOfNameTexture { get; set; } = new Dictionary<string, string>
        {
            { "Models\\Rocks\\textures\\stone_fence_old_low_rock_BaseColor_0", "Models\\Rocks\\textures\\stone_fence_old_low_rock_Normal" }
        };
        public override Dictionary<string, IMaterial> _materialOfNameTexture { get; set; } = new Dictionary<string, IMaterial>
        {
            { "Models\\Rocks\\textures\\stone_fence_old_low_rock_BaseColor_0", Rock }
        };
        private float _health = 100f;
        public Fence(ContentManager Content, String modelPath, Vector3 position, Matrix rotation, Vector3 scale, IModelInSceneRenderer renderer)
            : base(Content, modelPath, position, rotation, scale, renderer, 1f)
        {
            // Resistencia al choque del tanque
            strength = 3000000f;
        }
        public override void CreateHitbox(Effect effect, GraphicsDevice graphicsDevice)
        {
            Vector3 halfSize = new Vector3(
                _widthLocal * 0.49f,
                _heightLocal * 0.3f,
                _depthLocal * 0.3f
            );
            Vector3 center = new Vector3(
                _midpoint.X,
                _midpoint.Y * 0.8f,
                _midpoint.Z * -0.13f
            );
            _hitbox = new BoxHitbox(
                center - halfSize,
                center + halfSize,
                effect,
                graphicsDevice
            );
        }
        public override void CreateCollider(Simulation simulation, BufferPool bufferPool, Effect effect, GraphicsDevice graphicsDevice)
        {
            var center = new NumericVector(_position.X, _position.Y + Height / 2f, _position.Z);
            this._collider = new StaticBoxCollider(Width, Height, Depth - 100f, center, simulation, bufferPool, effect, graphicsDevice);
        }
        // 1. Choque físico del tanque
        public override void CollidedWith(ModelInScene model, Vector3 contactPointOffset, Vector3 thisPos, float impactImpulse, float destructiveImpactImpulse, Vector3 normal)
        {
            if (destructiveImpactImpulse > strength)
            {
                Fracture(-normal * 5000f);
            }
        }
        // 2. Choque de proyectil del tanque
        public override void CollidedWithMissil(Missil missil)
        {
            _health -= 100f;
            if (_health <= 0f)
            {
                Fracture(missil.Direction * 8000f);
            }
        }
        public void Fracture(Vector3 impactDirection)
        {
            if (_isDisabled) return;
            Destroy();
            TGCGame.ExplosionSound?.Play();
            TGCGame.Instance?.SpawnFenceDebris(_position, impactDirection);
        }
        public override void Destroy()
        {
            if (_collider != null && TGCGame.PhysicsToGameObjects.ContainsKey(_collider.GetCollidableReference()))
            {
                TGCGame.PhysicsToGameObjects.Remove(_collider.GetCollidableReference());
            }
            base.Destroy();
        }
    }
}