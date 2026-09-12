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
        private float _width;

        public float Width
        {
            get { return _width * _scale.X; }
            set { _width = value; }
        }
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
            _width = this.calculateWitdth();
        }
        public float calculateWitdth()
        {
            // Obtenemos las transformaciones base (huesos) para que la posición sea real
            Matrix[] transformaciones = new Matrix[_model.Bones.Count];
            _model.CopyAbsoluteBoneTransformsTo(transformaciones);

            Vector3 min = new Vector3(float.MaxValue);
            Vector3 max = new Vector3(float.MinValue);

            // Iteramos por cada Mesh y sus partes
            foreach (ModelMesh mesh in _model.Meshes)
            {
                Matrix transformacion = transformaciones[mesh.ParentBone.Index];

                foreach (ModelMeshPart meshPart in mesh.MeshParts)
                {
                    // Datos del buffer
                    int cantidadVertices = meshPart.NumVertices;
                    int stride = meshPart.VertexBuffer.VertexDeclaration.VertexStride;

                    // Arreglo para guardar SOLO las posiciones
                    Vector3[] posiciones = new Vector3[cantidadVertices];

                    // Extraemos los datos: Pedimos Vector3, saltando la cantidad de bytes dictada por el stride
                    meshPart.VertexBuffer.GetData(
                        meshPart.VertexOffset * stride, // Dónde empezar a leer en bytes
                        posiciones,                     // Dónde guardar la información
                        0,                              // Índice inicial de nuestro arreglo
                        cantidadVertices,               // Cuántos vértices leer
                        stride                          // Cuántos bytes saltar entre lecturas
                    );

                    // Analizamos cada vértice para encontrar los extremos
                    for (int i = 0; i < posiciones.Length; i++)
                    {
                        // Aplicamos la rotación/escala original de esta parte del modelo
                        Vector3 posicionReal = Vector3.Transform(posiciones[i], transformacion);

                        // Expandimos nuestra caja contenedora
                        min = Vector3.Min(min, posicionReal);
                        max = Vector3.Max(max, posicionReal);
                    }
                }
            }

            // El ancho total es la distancia entre el punto mínimo y máximo en X
            return max.X - min.X;
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