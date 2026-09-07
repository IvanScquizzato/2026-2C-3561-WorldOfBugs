using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Renderers;
using System.Collections.Concurrent;
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

        private static ConcurrentDictionary<Type, List<Texture2D>> _texturasPorSubclase = new();
        private static ConcurrentDictionary<Type, Model> _modeloPorSubclase = new();

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
            this.cargarModelo(Content, modelPath);
        }
        public List<Texture2D> GetTextures()
        {
            return _texturasPorSubclase.GetOrAdd(this.GetType(), _ => new List<Texture2D>());
        }
        private void cargarModelo(ContentManager Content, String modelPath)
        {
            if (modelPath == null)
            {
                return;
            }
            if (!_modeloPorSubclase.TryGetValue(this.GetType(), out var model))
            {
                _model = Content.Load<Model>(modelPath);
                _modeloPorSubclase[this.GetType()] = _model;
                foreach (var mesh in this._model.Meshes)
                    foreach (var meshPart in mesh.MeshParts)
                    {
                        var basicEffect = (BasicEffect)meshPart.Effect;
                        GetTextures().Add(basicEffect.Texture);
                        meshPart.Effect = _renderer._effect;
                    }
            }
            else
            {
                _model = _modeloPorSubclase[this.GetType()];
            }
        }
        /*private void cargarModelo()
        {
            foreach (var mesh in _model.Meshes)
            {
                foreach (var meshPart in mesh.MeshParts)
                {
                    Console.WriteLine(meshPart.Effect.GetType());

                    meshPart.Effect = _renderer._effect;
                }
            }
        }*/
        public virtual void Draw(Matrix view, Matrix projection)
        {
            _renderer.Draw(this, view, projection);
        }
    }
}