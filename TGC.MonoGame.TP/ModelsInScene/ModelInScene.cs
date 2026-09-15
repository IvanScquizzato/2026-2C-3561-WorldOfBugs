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
        public string _modelPath { get; private set; }
        public Vector3 _position { get; set; }
        public Vector3 _scale { get; set; }
        public Matrix _rotation { get; set; }
        public IModelInSceneRenderer _renderer { get; set; }
        private ContentManager _content { get; set; }
        public RasterizerState RasterizerState { get; set; } = RasterizerState.CullCounterClockwise;

        private static ConcurrentDictionary<string, List<Texture2D>> _texturasPorPath = new();
        private static ConcurrentDictionary<string, Model> _modeloPorPath = new();
        private static readonly object _lockObj = new object();
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
            _modelPath = modelPath;
            this.cargarModelo(Content, modelPath);
        }
        public List<Texture2D> GetTextures()
        {
            // Buscamos por el path del modelo
            if (_modelPath != null && _texturasPorPath.TryGetValue(_modelPath, out var texturas))
            {
                return texturas;
            }
            return new List<Texture2D>();
        }
        private void cargarModelo(ContentManager Content, String modelPath)
        {
            if (modelPath == null)
            {
                return;
            }

            lock (_lockObj)
            {
                if (!_modeloPorPath.TryGetValue(modelPath, out var model))
                {
                    _model = Content.Load<Model>(modelPath);
                    _modeloPorPath[modelPath] = _model;
                    var texturasExtraidas = new List<Texture2D>();
                    foreach (var mesh in this._model.Meshes)
                    {
                        foreach (var meshPart in mesh.MeshParts)
                        {
                            if (meshPart.Effect is BasicEffect basicEffect)
                            {
                                texturasExtraidas.Add(basicEffect.Texture);
                            }
                            else
                            {
                                texturasExtraidas.Add(null);
                            }

                            meshPart.Effect = _renderer._effect;
                        }
                    }
                    _texturasPorPath[modelPath] = texturasExtraidas;
                }

                else
                {
                    _model = _modeloPorPath[modelPath];
                }
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