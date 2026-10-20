using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TGC.MonoGame.TP.Cameras;
using TGC.MonoGame.TP.ModelsInScene;

namespace TGC.MonoGame.TP.Editor
{
    /// <summary>
    /// Editor aislado del gameplay. La seleccion usa la geometria del Model y su
    /// matriz _world actual, NO la hitbox ni el collider estatico de Bepu.
    /// </summary>
    public sealed class SceneEditor : IDisposable
    {
        private const float MouseTolerancePixels = 15f;
        private const float SameClickTolerancePixels = 7f;

        private sealed class Target
        {
            public string Id = "";
            public Func<Vector3> GetPosition = null!;
            public Action<Vector3> SetPosition = null!;
            public Func<Matrix> GetRotation = null!;
            public Action<Matrix> SetRotation = null!;
            public Func<float> GetScale = null!;
            public Action<float> SetScale = null!;
            public Func<BoundingBox>? GetBounds;
            public float PickRadius;

            // Opcional: permite seleccionar el modelo usando sus vertices reales.
            public ModelInScene? Model;
            public BoundingBox? LocalBounds;

            // Solo se ocultan los dibujos de depuracion, no la fisica real.
            public bool? OldRenderColliders;
            public bool? OldRenderHitboxes;

            public Matrix WorldMatrix => Model?._world ??
                (Matrix.CreateScale(GetScale()) * GetRotation() *
                 Matrix.CreateTranslation(GetPosition()));

            public BoundingBox FallbackWorldBounds()
            {
                if (GetBounds != null)
                    return GetBounds();

                var extent = new Vector3(PickRadius);
                var position = GetPosition();
                return new BoundingBox(position - extent, position + extent);
            }

            public Vector3 WorldCenter()
            {
                if (LocalBounds.HasValue)
                {
                    BoundingBox bounds = LocalBounds.Value;
                    return Vector3.Transform((bounds.Min + bounds.Max) * 0.5f, WorldMatrix);
                }
                BoundingBox fallback = FallbackWorldBounds();
                return (fallback.Min + fallback.Max) * 0.5f;
            }

            public Vector3[] WorldCorners()
            {
                if (LocalBounds.HasValue)
                {
                    Vector3[] corners = LocalBounds.Value.GetCorners();
                    Matrix world = WorldMatrix;
                    for (int i = 0; i < corners.Length; i++)
                        corners[i] = Vector3.Transform(corners[i], world);
                    return corners;
                }

                return FallbackWorldBounds().GetCorners();
            }

            public float? Intersects(Ray ray)
            {
                // La prueba se realiza en coordenadas locales: soporta rotacion,
                // desplazamiento del centro de malla y cambios de escala.
                if (LocalBounds.HasValue)
                {
                    Matrix inverse = Matrix.Invert(WorldMatrix);
                    Vector3 origin = Vector3.Transform(ray.Position, inverse);
                    Vector3 transformedDirection = Vector3.TransformNormal(ray.Direction, inverse);
                    if (!IsFinite(origin) || !IsFinite(transformedDirection) ||
                        transformedDirection.LengthSquared() < 1e-12f)
                        return null;

                    var localRay = new Ray(origin, Vector3.Normalize(transformedDirection));
                    float? hit = LocalBounds.Value.Intersects(localRay);
                    if (!hit.HasValue || hit.Value < 0f)
                        return null;

                    Vector3 localPoint = localRay.Position + localRay.Direction * hit.Value;
                    Vector3 worldPoint = Vector3.Transform(localPoint, WorldMatrix);
                    float distance = Vector3.Dot(worldPoint - ray.Position, ray.Direction);
                    return distance >= 0f ? distance : null;
                }

                return FallbackWorldBounds().Intersects(ray);
            }

            private static bool IsFinite(Vector3 v) =>
                float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);
        }

        private sealed class ClipboardData
        {
            public string SourceId = "";
            public Vector3 Position;
            public Matrix Rotation;
            public float Scale;
            public float Radius;
        }

        private readonly GraphicsDevice _graphics;
        private readonly BasicEffect _effect;
        private readonly List<Target> _objects = new();
        // Los Model son compartidos por muchas instancias; leer vertices solo una vez.
        private readonly Dictionary<Model, BoundingBox> _localBoundsCache = new();
        private Target? _selected;
        private ClipboardData? _clipboard;
        private int _pasteCount;
        private int _copyIndex = 1;
        private KeyboardState _lastKeyboard;
        private MouseState _lastMouse;
        private Point _lastPickPosition = new(-100000, -100000);

        public bool Enabled { get; private set; }
        public string? SelectedId => _selected?.Id;
        public Func<string, string, Vector3, Matrix, float, bool>? DuplicateFactory { get; set; }
        public Action? SaveRequested { get; set; }
        public Action? LoadRequested { get; set; }

        public SceneEditor(GraphicsDevice graphicsDevice)
        {
            _graphics = graphicsDevice;
            _effect = new BasicEffect(graphicsDevice)
            {
                World = Matrix.Identity,
                VertexColorEnabled = true,
                TextureEnabled = false,
                LightingEnabled = false
            };
        }

        /// <summary>
        /// No rompe llamadas antiguas: 'model' es opcional. Para seleccion precisa,
        /// pasar model: modelo al registrar cada ModelInScene en TGCGame.
        /// </summary>
        public void Register(
            string id,
            Func<Vector3> getPosition,
            Action<Vector3> setPosition,
            Func<Matrix> getRotation,
            Action<Matrix> setRotation,
            Func<float> getScale,
            Action<float> setScale,
            float pickRadius = 2f,
            Func<BoundingBox>? getBounds = null,
            ModelInScene? model = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("El ID no puede estar vacio.", nameof(id));
            if (_objects.Any(o => o.Id == id))
                throw new ArgumentException($"ID repetido: {id}", nameof(id));
            if (pickRadius <= 0f)
                throw new ArgumentOutOfRangeException(nameof(pickRadius));

            BoundingBox? localBounds = null;
            if (model?._model != null)
            {
                if (!_localBoundsCache.TryGetValue(model._model, out BoundingBox bounds))
                {
                    bounds = CalculateLocalBounds(model._model);
                    _localBoundsCache.Add(model._model, bounds);
                }
                localBounds = bounds;
            }

            var target = new Target
            {
                Id = id,
                GetPosition = getPosition,
                SetPosition = setPosition,
                GetRotation = getRotation,
                SetRotation = setRotation,
                GetScale = getScale,
                SetScale = setScale,
                GetBounds = getBounds,
                PickRadius = pickRadius,
                Model = model,
                LocalBounds = localBounds
            };
            _objects.Add(target);
            if (Enabled) HideDebug(target);
        }

        /// <summary>
        /// Obtiene el AABB LOCAL a partir de los vertices (una sola vez por Model).
        /// El volumen local luego se transforma en cada click usando _world.
        /// Si una GPU no permite leer vertices, usa las esferas de los meshes.
        /// </summary>
        private static BoundingBox CalculateLocalBounds(Model model)
        {
            var bones = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(bones);
            Vector3 min = new(float.MaxValue);
            Vector3 max = new(float.MinValue);
            bool any = false;

            foreach (ModelMesh mesh in model.Meshes)
            {
                Matrix bone = bones[mesh.ParentBone.Index];
                bool meshRead = false;

                try
                {
                    foreach (ModelMeshPart part in mesh.MeshParts)
                    {
                        if (part.NumVertices <= 0) continue;
                        VertexDeclaration declaration = part.VertexBuffer.VertexDeclaration;
                        VertexElement[] elements = declaration.GetVertexElements();
                        int offset = -1;
                        foreach (VertexElement element in elements)
                        {
                            if (element.VertexElementUsage == VertexElementUsage.Position &&
                                element.UsageIndex == 0)
                            {
                                offset = element.Offset;
                                break;
                            }
                        }
                        if (offset < 0) continue;

                        Vector3[] points = new Vector3[part.NumVertices];
                        int stride = declaration.VertexStride;
                        part.VertexBuffer.GetData(
                            part.VertexOffset * stride + offset,
                            points, 0, points.Length, stride);

                        foreach (Vector3 point in points)
                        {
                            Vector3 transformed = Vector3.Transform(point, bone);
                            min = Vector3.Min(min, transformed);
                            max = Vector3.Max(max, transformed);
                            any = true;
                            meshRead = true;
                        }
                    }
                }
                catch (Exception)
                {
                    // Algunas plataformas / buffers no admiten GetData.
                    // Aproximacion conservadora por esfera de cada mesh.
                }

                if (!meshRead)
                {
                    BoundingSphere sphere = mesh.BoundingSphere;
                    Vector3 center = Vector3.Transform(sphere.Center, bone);
                    float x = Vector3.TransformNormal(Vector3.Right, bone).Length();
                    float y = Vector3.TransformNormal(Vector3.Up, bone).Length();
                    float z = Vector3.TransformNormal(Vector3.Backward, bone).Length();
                    float radius = sphere.Radius * Math.Max(x, Math.Max(y, z));
                    Vector3 extent = new(radius);
                    min = Vector3.Min(min, center - extent);
                    max = Vector3.Max(max, center + extent);
                    any = true;
                }
            }

            if (!any)
                return new BoundingBox(new Vector3(-1f), new Vector3(1f));
            return new BoundingBox(min, max);
        }

        private static void HideDebug(Target target)
        {
            if (target.Model == null || target.OldRenderColliders.HasValue) return;
            target.OldRenderColliders = target.Model.renderColliders;
            target.OldRenderHitboxes = target.Model.renderHitboxes;
            target.Model.renderColliders = false;
            target.Model.renderHitboxes = false;
        }

        private static void RestoreDebug(Target target)
        {
            if (target.Model == null || !target.OldRenderColliders.HasValue) return;
            target.Model.renderColliders = target.OldRenderColliders.Value;
            target.Model.renderHitboxes = target.OldRenderHitboxes ?? true;
            target.OldRenderColliders = null;
            target.OldRenderHitboxes = null;
        }

        private bool JustPressed(KeyboardState current, Keys key) =>
            current.IsKeyDown(key) && !_lastKeyboard.IsKeyDown(key);

        public void Update(GameTime time, Camera freeCamera)
        {
            KeyboardState keyboard = Keyboard.GetState();
            MouseState mouse = Mouse.GetState();
            if (JustPressed(keyboard, Keys.F1))
            {
                Enabled = !Enabled;
                foreach (Target target in _objects)
                {
                    if (Enabled) HideDebug(target);
                    else RestoreDebug(target);
                }
            }

            if (Enabled)
            {
                float dt = Math.Min(0.1f, (float)time.ElapsedGameTime.TotalSeconds);
                UpdateCamera(freeCamera, keyboard, mouse, dt);

                if (mouse.RightButton != ButtonState.Pressed &&
                    mouse.LeftButton == ButtonState.Pressed &&
                    _lastMouse.LeftButton == ButtonState.Released)
                    Pick(mouse, freeCamera.View, freeCamera.Projection);

                bool ctrl = keyboard.IsKeyDown(Keys.LeftControl) ||
                            keyboard.IsKeyDown(Keys.RightControl);
                if (ctrl)
                {
                    if (JustPressed(keyboard, Keys.C)) Copy();
                    if (JustPressed(keyboard, Keys.V)) Paste();
                    if (JustPressed(keyboard, Keys.D)) { Copy(); Paste(); }
                    if (JustPressed(keyboard, Keys.S)) SaveRequested?.Invoke();
                    if (JustPressed(keyboard, Keys.L)) LoadRequested?.Invoke();
                }
                else if (_selected != null && mouse.RightButton != ButtonState.Pressed)
                {
                    UpdateSelected(keyboard, dt);
                }
            }

            _lastKeyboard = keyboard;
            _lastMouse = mouse;
        }

        // Movimiento libre del editor: WASD, Espacio / Shift y Alt para acelerar.
        private void UpdateCamera(Camera camera, KeyboardState keyboard, MouseState mouse, float dt)
        {
            Vector3 forward = camera.FrontDirection;
            if (forward.LengthSquared() < 0.001f) forward = Vector3.Forward;
            forward.Normalize();

            if (mouse.RightButton == ButtonState.Pressed &&
                _lastMouse.RightButton == ButtonState.Pressed)
            {
                float yaw = MathF.Atan2(forward.X, -forward.Z);
                float pitch = MathF.Asin(MathHelper.Clamp(forward.Y, -1f, 1f));
                yaw += (mouse.X - _lastMouse.X) * 0.003f;
                pitch -= (mouse.Y - _lastMouse.Y) * 0.003f;
                pitch = MathHelper.Clamp(pitch, -1.5f, 1.5f);
                forward = new Vector3(
                    MathF.Sin(yaw) * MathF.Cos(pitch),
                    MathF.Sin(pitch),
                    -MathF.Cos(yaw) * MathF.Cos(pitch));
            }

            Vector3 right = Vector3.Normalize(Vector3.Cross(forward, Vector3.Up));
            float speed = 800f * ((keyboard.IsKeyDown(Keys.LeftAlt) ||
                                   keyboard.IsKeyDown(Keys.RightAlt)) ? 3f : 1f);
            Vector3 direction = Vector3.Zero;
            bool ctrl = keyboard.IsKeyDown(Keys.LeftControl) ||
                        keyboard.IsKeyDown(Keys.RightControl);
            if (!ctrl)
            {
                if (keyboard.IsKeyDown(Keys.W)) direction += forward;
                if (keyboard.IsKeyDown(Keys.S)) direction -= forward;
                if (keyboard.IsKeyDown(Keys.D)) direction += right;
                if (keyboard.IsKeyDown(Keys.A)) direction -= right;
            }
            if (keyboard.IsKeyDown(Keys.Space)) direction += Vector3.Up;
            if (keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift))
                direction -= Vector3.Up;

            if (direction.LengthSquared() > 0.001f)
            {
                direction.Normalize();
                camera.Position += direction * speed * dt;
            }

            camera.FrontDirection = forward;
            camera.RightDirection = right;
            camera.UpDirection = Vector3.Up;
            camera.View = Matrix.CreateLookAt(camera.Position, camera.Position + forward, Vector3.Up);
        }

        private void UpdateSelected(KeyboardState keyboard, float dt)
        {
            Target obj = _selected!;
            float step = dt * 250f;
            Vector3 delta = Vector3.Zero;
            if (keyboard.IsKeyDown(Keys.Left)) delta.X -= step;
            if (keyboard.IsKeyDown(Keys.Right)) delta.X += step;
            if (keyboard.IsKeyDown(Keys.Up)) delta.Z -= step;
            if (keyboard.IsKeyDown(Keys.Down)) delta.Z += step;
            if (keyboard.IsKeyDown(Keys.PageUp)) delta.Y += step;
            if (keyboard.IsKeyDown(Keys.PageDown)) delta.Y -= step;
            if (delta != Vector3.Zero) obj.SetPosition(obj.GetPosition() + delta);

            float angle = 1.2f * dt;
            float yaw = (keyboard.IsKeyDown(Keys.R) ? angle : 0f) -
                        (keyboard.IsKeyDown(Keys.F) ? angle : 0f);
            float pitch = (keyboard.IsKeyDown(Keys.T) ? angle : 0f) -
                          (keyboard.IsKeyDown(Keys.G) ? angle : 0f);
            float roll = (keyboard.IsKeyDown(Keys.Y) ? angle : 0f) -
                         (keyboard.IsKeyDown(Keys.H) ? angle : 0f);
            if (yaw != 0f || pitch != 0f || roll != 0f)
                obj.SetRotation(obj.GetRotation() * Matrix.CreateFromYawPitchRoll(yaw, pitch, roll));

            bool grow = keyboard.IsKeyDown(Keys.E) || keyboard.IsKeyDown(Keys.OemPlus) ||
                        keyboard.IsKeyDown(Keys.Add);
            bool shrink = keyboard.IsKeyDown(Keys.Q) || keyboard.IsKeyDown(Keys.OemMinus) ||
                          keyboard.IsKeyDown(Keys.Subtract);
            int change = (grow ? 1 : 0) - (shrink ? 1 : 0);
            if (change != 0)
            {
                float scale = MathHelper.Clamp(obj.GetScale() * MathF.Exp(change * 1.2f * dt),
                    0.01f, 10000f);
                obj.SetScale(scale);
            }
        }

        public void Copy()
        {
            if (_selected == null) return;
            _clipboard = new ClipboardData
            {
                SourceId = _selected.Id,
                Position = _selected.GetPosition(),
                Rotation = _selected.GetRotation(),
                Scale = _selected.GetScale(),
                Radius = _selected.PickRadius
            };
            _pasteCount = 0;
        }

        public void Paste()
        {
            if (_clipboard == null || DuplicateFactory == null) return;
            string id;
            do { id = $"Copy_{_copyIndex++:D6}"; }
            while (_objects.Any(o => o.Id == id));

            _pasteCount++;
            float offset = Math.Max(50f, _clipboard.Radius * 0.5f) * _pasteCount;
            Vector3 position = _clipboard.Position + new Vector3(offset, 0f, offset);
            if (DuplicateFactory(_clipboard.SourceId, id, position,
                    _clipboard.Rotation, _clipboard.Scale))
                _selected = _objects.FirstOrDefault(o => o.Id == id);
        }

        /// <summary>
        /// 1) Interseccion real del rayo con la caja orientada por _world.
        /// 2) Si el usuario clickea cerca de un objeto fino, tolerancia de 15 px.
        /// 3) Repetir clic en la misma posicion alterna objetos superpuestos.
        /// </summary>
        private void Pick(MouseState mouse, Matrix view, Matrix projection)
        {
            Viewport viewport = _graphics.Viewport;
            Vector3 near = viewport.Unproject(new Vector3(mouse.X, mouse.Y, 0f),
                projection, view, Matrix.Identity);
            Vector3 far = viewport.Unproject(new Vector3(mouse.X, mouse.Y, 1f),
                projection, view, Matrix.Identity);
            Vector3 direction = far - near;
            if (direction.LengthSquared() < 1e-10f) return;

            var ray = new Ray(near, Vector3.Normalize(direction));
            var directHits = new List<(Target Target, float Distance)>();
            var nearHits = new List<(Target Target, float PixelDistance, float Distance)>();

            foreach (Target target in _objects)
            {
                if (target.Model?._isDisabled == true) continue;
                float? hit = target.Intersects(ray);
                if (hit.HasValue && hit.Value >= 0f)
                {
                    directHits.Add((target, hit.Value));
                    continue;
                }

                if (ScreenDistance(target, mouse, view, projection,
                        out float pixelDistance, out float depth) &&
                    pixelDistance <= MouseTolerancePixels)
                    nearHits.Add((target, pixelDistance, depth));
            }

            // Coincidencias precisas antes que las de proximidad.
            directHits.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            nearHits.Sort((a, b) =>
            {
                int byPixels = a.PixelDistance.CompareTo(b.PixelDistance);
                return byPixels != 0 ? byPixels : a.Distance.CompareTo(b.Distance);
            });

            var candidates = new List<Target>(directHits.Count + nearHits.Count);
            candidates.AddRange(directHits.Select(item => item.Target));
            candidates.AddRange(nearHits.Select(item => item.Target));

            Point currentPoint = new(mouse.X, mouse.Y);
            int dx = currentPoint.X - _lastPickPosition.X;
            int dy = currentPoint.Y - _lastPickPosition.Y;
            bool sameSpot = (float)dx * dx + (float)dy * dy <=
                            SameClickTolerancePixels * SameClickTolerancePixels;

            if (sameSpot && _selected != null)
            {
                int index = candidates.IndexOf(_selected);
                _selected = index >= 0 && candidates.Count > 1
                    ? candidates[(index + 1) % candidates.Count]
                    : candidates.FirstOrDefault();
            }
            else
            {
                _selected = candidates.FirstOrDefault();
            }
            _lastPickPosition = currentPoint;
        }

        private bool ScreenDistance(Target target, MouseState mouse,
            Matrix view, Matrix projection, out float pixels, out float depth)
        {
            pixels = float.MaxValue;
            depth = float.MaxValue;
            Vector3[] corners = target.WorldCorners();
            float left = float.MaxValue, top = float.MaxValue;
            float right = float.MinValue, bottom = float.MinValue;
            bool found = false;

            foreach (Vector3 corner in corners)
            {
                Vector3 cameraSpace = Vector3.Transform(corner, view);
                if (cameraSpace.Z >= -0.01f) continue; // detras de la camara
                Vector3 screen = _graphics.Viewport.Project(corner, projection, view, Matrix.Identity);
                if (!float.IsFinite(screen.X) || !float.IsFinite(screen.Y)) continue;
                left = Math.Min(left, screen.X);
                right = Math.Max(right, screen.X);
                top = Math.Min(top, screen.Y);
                bottom = Math.Max(bottom, screen.Y);
                depth = Math.Min(depth, -cameraSpace.Z);
                found = true;
            }
            if (!found || right < 0 || bottom < 0 ||
                left > _graphics.Viewport.Width || top > _graphics.Viewport.Height)
                return false;

            float dx = Math.Max(left - mouse.X, Math.Max(0f, mouse.X - right));
            float dy = Math.Max(top - mouse.Y, Math.Max(0f, mouse.Y - bottom));
            pixels = MathF.Sqrt(dx * dx + dy * dy);
            return true;
        }

        public void Draw(Matrix view, Matrix projection)
        {
            if (!Enabled || _selected == null) return;

            Vector3[] corners = _selected.WorldCorners();
            // Orden de GetCorners() de BoundingBox no es trivial;
            // usamos pares de esquinas que difieren en un solo eje.
            var lines = new List<VertexPositionColor>(30);
            Vector3[] referenceCorners = _selected.LocalBounds.HasValue
                ? _selected.LocalBounds.Value.GetCorners()
                : _selected.FallbackWorldBounds().GetCorners();
            for (int i = 0; i < corners.Length; i++)
            {
                for (int j = i + 1; j < corners.Length; j++)
                {
                    // En el orden de GetCorners(), cada vertice posee tres
                    // aristas. Calculamos la adyacencia en la caja LOCAL.
                    // Dibujar 12 aristas, sin depender del orden de GetCorners().
                    Vector3 li = referenceCorners[i];
                    Vector3 lj = referenceCorners[j];
                    int different = 0;
                    if (MathF.Abs(li.X - lj.X) > 0.00001f) different++;
                    if (MathF.Abs(li.Y - lj.Y) > 0.00001f) different++;
                    if (MathF.Abs(li.Z - lj.Z) > 0.00001f) different++;
                    if (different == 1)
                        AddLine(lines, corners[i], corners[j], Color.Yellow);
                }
            }

            Vector3 center = _selected.WorldCenter();
            float length = Math.Max(10f, (corners[0] - corners[6]).Length() * 0.25f);
            AddLine(lines, center, center + Vector3.Right * length, Color.Red);
            AddLine(lines, center, center + Vector3.Up * length, Color.Lime);
            AddLine(lines, center, center + Vector3.Backward * length, Color.Blue);

            _effect.View = view;
            _effect.Projection = projection;
            DepthStencilState oldDepth = _graphics.DepthStencilState;
            try
            {
                _graphics.DepthStencilState = DepthStencilState.None;
                foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    if (lines.Count > 0)
                        _graphics.DrawUserPrimitives(PrimitiveType.LineList,
                            lines.ToArray(), 0, lines.Count / 2);
                }
            }
            finally
            {
                _graphics.DepthStencilState = oldDepth;
            }
        }

        private static void AddLine(List<VertexPositionColor> lines,
            Vector3 from, Vector3 to, Color color)
        {
            lines.Add(new VertexPositionColor(from, color));
            lines.Add(new VertexPositionColor(to, color));
        }

        public void ClearRegistered()
        {
            foreach (Target target in _objects) RestoreDebug(target);
            _objects.Clear();
            _selected = null;
            _clipboard = null;
            _pasteCount = 0;
            _lastPickPosition = new Point(-100000, -100000);
        }

        public void Dispose()
        {
            foreach (Target target in _objects) RestoreDebug(target);
            _effect.Dispose();
        }
    }
}
