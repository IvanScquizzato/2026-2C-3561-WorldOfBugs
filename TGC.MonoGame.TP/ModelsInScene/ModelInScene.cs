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
using BepuPhysics;
using TGC.MonoGame.Utils;
using NumericVector3 = System.Numerics.Vector3;
using TGC.MonoGame.TP.Colliders;
using System.Reflection.Metadata.Ecma335;
using BepuUtilities.Memory;
namespace TGC.MonoGame.TP.ModelsInScene
{
    /// <summary>
    ///     Clase que representa un modelo en el escenario.
    /// </summary>
    public abstract class ModelInScene
    {
        public Model _model { get; set; }
        public string _modelPath { get; private set; }
        public Vector3 _position
        {
            get;
            set;
        }
        public float strength = float.MaxValue;
        public bool _isDisabled = false;
        public Vector3 _velocity
        {
            get
            {
                return _collider.GetLinearVelocity();
            }
        }
        public Vector3 _angularVelocity
        {
            get
            {
                return _collider.GetAngularVelocity();
            }
        }
        public Vector3 _scale { get; set; }
        public Matrix _rotation { get; set; }
        public float _angularDrag { get; set; }
        public Vector3 _forward
        {
            get { return -_rotation.Forward; }
        }
        public Vector3 _up
        {
            get { return _rotation.Up; }
        }
        public Vector3 _right
        {
            get { return -_rotation.Right; }
        }
        public IModelInSceneRenderer _renderer { get; set; }
        private ContentManager _content { get; set; }
        public RasterizerState RasterizerState { get; set; } = RasterizerState.CullCounterClockwise;
        public float _widthLocal { get; set; }

        public float Width
        {
            get { return _widthLocal * _scale.X; }
            set { _widthLocal = value; }
        }
        public float _heightLocal { get; set; }
        public float Height
        {
            get { return _heightLocal * _scale.Y; }
            set { _heightLocal = value; }
        }
        public float _depthLocal { get; set; }
        public float Depth
        {
            get { return _depthLocal * _scale.Z; }
            set { _depthLocal = value; }
        }
        public float _mass { get; set; }

        private static ConcurrentDictionary<string, List<Texture2D>> _texturasPorPath = new();
        private static ConcurrentDictionary<string, Model> _modeloPorPath = new();
        private static readonly object _lockObj = new object();

        protected Vector3 _midpoint
        {
            get;
            set;
        }
        public Matrix _world
        {
            get { return Matrix.CreateScale(_scale) * _rotation * Matrix.CreateTranslation(_position); }
        }
        public Collider _collider = null;

        public Hitbox _hitbox { get; protected set; }

        // Estructura para guardar nuestra flecha
        public struct DebugArrow
        {
            public Vector3 Start;
            public Vector3 End;
            public Color Color;
        }

        // Lista que limpiaremos y llenaremos en cada frame
        public List<DebugArrow> DebugForces = new List<DebugArrow>();

        public ModelInScene(ContentManager Content, String modelPath, Vector3 position, Matrix rotation, Vector3 scale, IModelInSceneRenderer renderer, float mass)
        {
            _content = Content;
            _position = position;
            _rotation = rotation;
            _scale = scale;
            _renderer = renderer;
            _mass = mass;
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
                            Console.WriteLine(mesh.Name);
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
            this.calculateDimentions();
            //this.calculateInertia();
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
            _widthLocal = maxX - minX;
            _heightLocal = maxY - minY;
            _depthLocal = maxZ - minZ;

            _midpoint = new Vector3(
            (minX + maxX) / 2f,
            (minY + maxY) / 2f,
            (minZ + maxZ) / 2f
    );
        }

        public void updateBodyPosition()
        {
            Matrix world = _collider.GetWorldMatrixNotScaled();
            world.Decompose(out Vector3 scale, out Quaternion rotation, out Vector3 translation);

            _rotation = Matrix.CreateFromQuaternion(rotation);

            _position = translation + Correction();
        }
        public void agregarFuerzaAngularAAplicar(Vector3 angularImpulse, float seconds)
        {
            _collider.ApplyAngularImpulse(UtilsClass.ToNumericVector(angularImpulse * seconds));
        }
        public void agregarFuerzaAAplicar(Force force, float seconds)
        {
            NumericVector3 numericForceVector = UtilsClass.ToNumericVector(force._vector);
            NumericVector3 numericApplicationVector = UtilsClass.ToNumericVector(force._applicationPoint);

            _collider.ApplyImpulse(numericForceVector * seconds, numericApplicationVector);

            // --- LÓGICA DE VISUALIZACIÓN ---
            float magnitude = force._vector.Length();

            if (magnitude > 0.01f)
            {

                float visualScale = MathHelper.Clamp(magnitude * 0.002f, 2f, 30f);

                Vector3 direction = Vector3.Normalize(force._vector);

                Vector3 worldStartPoint = _position + force._applicationPoint;
                Vector3 worldEndPoint = worldStartPoint + (direction * visualScale);

                DebugForces.Add(new DebugArrow
                {
                    Start = worldStartPoint,
                    End = worldEndPoint,
                    Color = Color.LimeGreen
                });

                Vector3 up = Vector3.Up;
                if (Math.Abs(Vector3.Dot(direction, up)) > 0.9f) up = Vector3.Right;
                Vector3 right = Vector3.Cross(direction, up);

                float headSize = visualScale * 0.1f;
                DebugForces.Add(new DebugArrow { Start = worldEndPoint, End = worldEndPoint - direction * headSize + right * headSize, Color = Color.Red });
                DebugForces.Add(new DebugArrow { Start = worldEndPoint, End = worldEndPoint - direction * headSize - right * headSize, Color = Color.Red });
            }
        }
        public virtual void Draw(Matrix view, Matrix projection)
        {
            if (!_isDisabled)
            {
                _renderer.Draw(this, view, projection);
                if (_collider != null)
                {
                    _collider.Draw(view, projection);
                }
                DrawLines(DebugForces, view, projection, _renderer._effect, _renderer._graphicsDevice);
            }
            DebugForces.Clear();
        }
        public static void DrawLines(List<DebugArrow> arrows, Matrix view, Matrix projection, Effect effect, GraphicsDevice graphicsDevice)
        {
            if (arrows == null || arrows.Count == 0) return;

            // Configuramos las matrices de la cámara
            if (effect.Parameters["World"] != null)
            {
                effect.Parameters["World"].SetValue(Matrix.Identity);
            }
            effect.Parameters["View"].SetValue(view);
            effect.Parameters["Projection"].SetValue(projection);
            // Creamos los vértices para las líneas
            var vertices = new VertexPositionColor[arrows.Count * 2];
            int index = 0;

            foreach (var arrow in arrows)
            {
                vertices[index++] = new VertexPositionColor(arrow.Start, arrow.Color);
                vertices[index++] = new VertexPositionColor(arrow.End, arrow.Color);
            }

            // Renderizamos
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, vertices, 0, arrows.Count);
            }
        }
        public virtual Vector3 Correction()
        {
            return Vector3.Zero;
        }

        // Para que el objeto pueda ser detectado por un misil
        public bool ContainsPoint(Vector3 point)
        {
            if (_hitbox == null)
                return false;

            return _hitbox.ContainsPoint(
                point,
                _world
            );
        }
        public virtual void CreateCollider(Simulation simulation, BufferPool bufferPool, Effect effect, GraphicsDevice graphicsDevice) { }

        public virtual void CollidedWith(ModelInScene model, Vector3 contactPointOffset, Vector3 thisPos, float impactImpulse, float destructiveImpactImpulse, Vector3 normal) { }

        public float InverseMass()
        {
            return _collider.InverseMass();
        }

        public virtual bool ShouldCollideWith(ModelInScene model, Vector3 contactPointOffset, Vector3 thisPos, float impactImpulse, float destructiveImpactImpulse, Vector3 normal)
        {
            return true;
        }
        public bool IsDestroyedWith(float destructiveImpactImpulse)
        {
            return destructiveImpactImpulse > strength;
        }
    }


}