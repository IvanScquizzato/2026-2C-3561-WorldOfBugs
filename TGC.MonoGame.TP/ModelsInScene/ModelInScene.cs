using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Renderers;
namespace TGC.MonoGame.TP.ModelsInScene
{
    /// <summary>
    ///     Clase que representa un modelo en el escenario.
    /// </summary>
    public abstract class ModelInScene
    {
        public Model _model { get; set; }
        public Vector3 _position { get; set; }
        public Vector3 _scale { get; set; }
        public Matrix _rotation { get; set; }
        public IModelInSceneRenderer _renderer { get; set; }
        private ContentManager _content { get; set; }
        public RasterizerState RasterizerState { get; set; } = RasterizerState.CullCounterClockwise;
        public List<Texture2D> _textures = new List<Texture2D>();
        public Matrix _world
        {
            get { return Matrix.CreateScale(_scale) * _rotation * Matrix.CreateTranslation(_position); }
        }
        public ModelInScene(ContentManager Content, String modelPath, Vector3 position, Matrix rotation, Vector3 scale, IModelInSceneRenderer renderer)
        {
            _content = Content;
            _position = position;
            _rotation = rotation;
            _scale = scale;
            _renderer = renderer;
            if (modelPath != "")
            {
                _model = Content.Load<Model>(modelPath);
                this.cargarModelo();
            }

        }
        private void cargarModelo()
        {
            foreach (var mesh in this._model.Meshes)
                foreach (var meshPart in mesh.MeshParts)
                {
                    var basicEffect = (BasicEffect)meshPart.Effect;
                    _textures.Add(basicEffect.Texture);
                    meshPart.Effect = _renderer._effect;
                }
        }

        public virtual void Draw(Matrix view, Matrix projection)
        {
            _renderer.Draw(this, view, projection);
        }
    }
}