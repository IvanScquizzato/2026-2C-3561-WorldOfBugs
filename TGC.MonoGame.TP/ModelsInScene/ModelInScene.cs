using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Renderers;
using System.Collections.Concurrent;
using TGC.MonoGame.TP.Forces;
using System.Linq;
using System.Runtime.Intrinsics;
namespace TGC.MonoGame.TP.ModelsInScene
{
    /// <summary>
    ///     Clase que representa un modelo en el escenario.
    /// </summary>
    public abstract class ModelInScene
    {
        public Model _model { get; set; }
        public Vector3 _position { get; set; }
        public Vector3 _velocity { get; set; } = new Vector3(0, 0, 0);
        public Vector3 _angularVelocity { get; set; } = Vector3.Zero;
        public Vector3 _scale { get; set; }
        public Matrix _rotation { get; set; }
        public Matrix _inertia { get; set; }
        public Matrix _inertiaInverse { get; set; }
        public float _angularDrag { get; set; }
        public Vector3 _forward
        {
            get { return -_rotation.Forward; }
        }
        public Vector3 _right
        {
            get { return -_rotation.Right; }
        }
        public List<Force> _fuerzasAAplicar { get; set; } = [];
        public IModelInSceneRenderer _renderer { get; set; }
        private ContentManager _content { get; set; }
        public RasterizerState RasterizerState { get; set; } = RasterizerState.CullCounterClockwise;
        private float _width;

        public float Width
        {
            get { return _width * _scale.X; }
            set { _width = value; }
        }
        private float _height;
        public float Height
        {
            get { return _height * _scale.Y; }
            set { _height = value; }
        }
        private float _depth;
        public float Depth
        {
            get { return _depth * _scale.Z; }
            set { _depth = value; }
        }
        public float _mass { get; set; }
        private static ConcurrentDictionary<Type, List<Texture2D>> _texturasPorSubclase = new();
        private static ConcurrentDictionary<Type, Model> _modeloPorSubclase = new();

        public Matrix _world
        {
            get { return Matrix.CreateScale(_scale) * _rotation * Matrix.CreateTranslation(_position); }
        }
        public ModelInScene(ContentManager Content, String modelPath, Vector3 position, Matrix rotation, Vector3 scale, IModelInSceneRenderer renderer, float angularDrag, float mass)
        {
            _content = Content;
            _position = position;
            _rotation = rotation;
            _scale = scale;
            _renderer = renderer;
            _angularDrag = angularDrag;
            _mass = mass;
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
            this.calculateDimentions();
            this.calculateInertia();
        }
        public void calculateDimentions()
        {
            // Obtenemos las transformaciones base (huesos) para que la posición sea real
            Matrix[] transformaciones = new Matrix[_model.Bones.Count];
            _model.CopyAbsoluteBoneTransformsTo(transformaciones);

            Vector3 min = new Vector3(float.MaxValue);
            Vector3 max = new Vector3(float.MinValue);
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            float minZ = float.MaxValue;
            float maxZ = float.MinValue;
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

                        minX = MathF.Min(minX, posicionReal.X);
                        minY = MathF.Min(minY, posicionReal.Y);
                        minZ = MathF.Min(minZ, posicionReal.Z);

                        maxX = MathF.Max(maxX, posicionReal.X);
                        maxY = MathF.Max(maxY, posicionReal.Y);
                        maxZ = MathF.Max(maxZ, posicionReal.Z);

                        // Expandimos nuestra caja contenedora
                        //min = Vector3.Min(min, posicionReal);
                        //max = Vector3.Max(max, posicionReal);
                    }
                }

            }
            _width = maxX - minX;
            _height = maxY - minY;
            _depth = maxZ - minZ;
        }

        private void calculateInertia()
        {
            if (_mass <= 0 || Width <= 0 || Height <= 0 || Depth <= 0)
            {
                _inertia = Matrix.Identity;
                return;
            }

            float m11 = (1f / 12f) * _mass * (MathF.Pow(Height, 2) + MathF.Pow(Depth, 2));
            float m22 = (1f / 12f) * _mass * (MathF.Pow(Width, 2) + MathF.Pow(Depth, 2));
            float m33 = (1f / 12f) * _mass * (MathF.Pow(Width, 2) + MathF.Pow(Height, 2));

            _inertia = new Matrix(
                m11: m11, m12: 0f, m13: 0f, m14: 0f,
                m21: 0f, m22: m22, m23: 0f, m24: 0f,
                m31: 0f, m32: 0f, m33: m33, m34: 0f,
                m41: 0f, m42: 0f, m43: 0f, m44: 1f
            );
            _inertiaInverse = Matrix.Invert(_inertia);
        }
        public void aplicarFuerzas(float time)
        {
            List<Force> forces = _fuerzasAAplicar;
            Vector3 fuerzaTotal = Vector3.Zero;
            foreach (Force forceVector in forces)
            {
                fuerzaTotal += forceVector._vector;
            }
            Vector3 aceleracionTotal = fuerzaTotal / _mass;

            _velocity += aceleracionTotal * time;

            _position += _velocity * time;

            Vector3 torqueNeto = Vector3.Zero;
            foreach (Force force in forces)
            {
                torqueNeto += Vector3.Cross(force._applicationPoint, force._vector);
            }
            Matrix worldToLocalRotation = Matrix.Invert(_rotation);

            Vector3 localTorque = Vector3.TransformNormal(torqueNeto, worldToLocalRotation);

            Vector3 localAngularAcceleration = Vector3.Transform(localTorque, _inertiaInverse);

            Vector3 aceleracionAngular = Vector3.TransformNormal(localAngularAcceleration, _rotation);

            _angularVelocity += aceleracionAngular * time;

            float angularDamping = MathF.Exp(-_angularDrag * time);
            _angularVelocity *= angularDamping;

            float anguloDeRotacion = _angularVelocity.Length() * time;
            if (anguloDeRotacion > 0.0001f)
            {
                Vector3 eje = Vector3.Normalize(_angularVelocity);
                _rotation = Matrix.CreateFromAxisAngle(eje, anguloDeRotacion) * _rotation;
            }
            _fuerzasAAplicar.Clear();
        }
        public void agregarFuerzaAAplicar(Force force)
        {
            _fuerzasAAplicar.Add(force);
        }
        public virtual void Draw(Matrix view, Matrix projection)
        {
            _renderer.Draw(this, view, projection);
        }
    }
}