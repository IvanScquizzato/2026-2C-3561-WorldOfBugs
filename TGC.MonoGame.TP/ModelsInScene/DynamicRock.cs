using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using BepuPhysics;
using BepuUtilities.Memory;
using NumericVector3 = System.Numerics.Vector3;
using TGC.MonoGame.TP.Colliders;
using TGC.MonoGame.TP.Materials;
using TGC.MonoGame.TP.Renderers;

namespace TGC.MonoGame.TP.ModelsInScene
{
    public class DynamicRock : ModelInScene
    {
        private static readonly Dictionary<Texture, Texture> _normalMaps = new Dictionary<Texture, Texture>();

        public override Dictionary<Texture, Texture> _normalMapsOfTexture => _normalMaps;

        public override Dictionary<string, string> _namesNormalMapsOfNameTexture { get; set; } = new Dictionary<string, string>
        {
            { "Models\\RocksMedium\\source\\Rocks_medium\\Rocks_medium_diffuse_0", "Models\\RocksMedium\\textures\\Rocks_medium_normal" }
        };

        public override Dictionary<string, IMaterial> _materialOfNameTexture { get; set; } = new Dictionary<string, IMaterial>
        {
            { "Models\\RocksMedium\\source\\Rocks_medium\\Rocks_medium_diffuse_0", Rock }
        };

        private float _lifetime = 0f;
        private const float MaxLifetime = 15f; // Desaparecen tras 15 segundos para no sobrecargar el juego

        public DynamicRock(ContentManager content, string modelPath, Vector3 position, Matrix rotation, Vector3 scale, IModelInSceneRenderer renderer, float mass)
            : base(content, modelPath, position, rotation, scale, renderer, mass)
        {
        }

        public override void CreateCollider(Simulation simulation, BufferPool bufferPool, Effect effect, GraphicsDevice graphicsDevice)
        {
            // Dimensiones proporcionales a la escala de la piedra
            float w = MathF.Max(Width * 0.6f, 15f);
            float h = MathF.Max(Height * 0.6f, 15f);
            float d = MathF.Max(Depth * 0.6f, 15f);

            var pos = new NumericVector3(_position.X, _position.Y, _position.Z);
            _collider = new DynamicBoxCollider(w, h, d, _mass, pos, simulation, bufferPool, effect, graphicsDevice);
        }

        public void Update(GameTime gameTime)
        {
            if (_isDisabled) return;

            // Sincroniza la posición y rotación del modelo con la simulación física de Bepu
            updateBodyPosition();

            _lifetime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_lifetime >= MaxLifetime)
            {
                Destroy();
            }
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