using System;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TGC.MonoGame.TP.ModelsInScene;
using TGC.MonoGame.TP.Renderers;
using TGC.MonoGame.TP.Forces;
using TGC.MonoGame.TP.Contact;
using TGC.MonoGame.TP.Cameras;
using System.Collections.Generic;
using System.Linq;
using NumericVector = System.Numerics.Vector3;
using TGC.MonoGame.TP.TankRaycasts;
using BepuPhysics;
using Microsoft.VisualBasic;
using TGC.MonoGame.Utils;
using TGC.MonoGame.TP.Colliders;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using TGC.MonoGame.TP.Physics.Bepu;
using System.Diagnostics;

using TGC.MonoGame.TP.Missile;
using TGC.MonoGame.TP.Terrain;
using System.Security.Cryptography.X509Certificates;
using System.Reflection.Metadata;
namespace TGC.MonoGame.TP.Tanks
{
    public class Tank : ModelInScene
    {

        //public List<ContactPoint> leftContactPoints { get; } = new List<ContactPoint>();
        //public List<ContactPoint> rightContactPoints { get; } = new List<ContactPoint>();
        public List<TankRaycast> raycastPointsLeft = new List<TankRaycast>();
        public List<TankRaycast> raycastPointsRight = new List<TankRaycast>();
        public float _maxSpeed { get; set; } = 500f;
        public float _engineThrust { get; set; } = 7000000f;

        private MouseState _previousMouseState;
        public float TurretYaw { get; set; } = 0f;
        public float CannonPitch { get; set; } = 0f;
        public float _lateralFriction { get; set; } = 40f;

        public float desiredHeight { get; set; }
        public float suspension { get; set; }
        public float cushioning { get; set; }
        private float cushioningDamping = 0.8f;
        private float quantityOfHorizontalRaycasts = 8f;
        private float quantityOfVerticalRaycasts = 18f;
        private float alturaMinTorreta = -0.4f;
        private float alturaMaxTorreta = 0.5f;
        private float coolDownSecs = 0.5f;
        private float currentCoolDownSecs = 0f;
        private float minCrashSoundSpeed = 50f;
        private const long crashSoundCoolDownMs = 500;
        private static long lastCrashSoundTicks = 0;
        public float maxHealth = 100f;

        public float health;
        public Dictionary<ModelMeshPart, (VertexPositionNormalTexture[], ModelMesh)> verticesPorMeshPart = new Dictionary<ModelMeshPart, (VertexPositionNormalTexture[], ModelMesh)>();
        private Dictionary<(ModelMeshPart, int), float> deformacionPorVertice = new Dictionary<(ModelMeshPart, int), float>();
        private float maxDeformation;
        public Dictionary<ModelMeshPart, DynamicVertexBuffer> _dynamicVertexBuffers = new Dictionary<ModelMeshPart, DynamicVertexBuffer>();
        public Dictionary<ModelMeshPart, int[]> _indexBuffers = new Dictionary<ModelMeshPart, int[]>();

        public Tank(
            ContentManager Content,
            String modelPath,
            Vector3 position,
            Matrix rotation,
            Vector3 scale,
            TankRenderer renderer,
            float mass
        )
            : base(
                Content,
                modelPath,
                position,
                rotation,
                scale,
                renderer,
                mass
            )
        {
            desiredHeight = Height * 0.35f;
            health = maxHealth;
            maxDeformation = Width * 0.15f;
        }
        public override void CreateHitbox(Effect effect, GraphicsDevice graphicsDevice)
        {
            CompoundHitbox newHitbox = new CompoundHitbox();

            // Chasis
            newHitbox.CreateSubHitbox(
                Width * 0.87f, Height * 0.12f, Depth * 0.52f,           // Half Sizes
                _midpoint.X, _midpoint.Y * 1.15f, _midpoint.Z * -0.21f, // Centers
                effect, graphicsDevice
            );

            // Lower Chasis
            newHitbox.CreateSubHitbox(
                Width * 0.89f, Height * 0.13f, Depth * 0.55f,
                _midpoint.X, _midpoint.Y * 0.9f, _midpoint.Z * -0.15f,
                effect, graphicsDevice
            );

            // Lowerer Chasis
            newHitbox.CreateSubHitbox(
                Width * 0.67f, Height * 0.3f, Depth * 0.6f,
                _midpoint.X, _midpoint.Y * 0.6f, _midpoint.Z * -0f,
                effect, graphicsDevice
            );

            // Turret
            newHitbox.CreateSubHitbox(
                Width * 0.57f, Height * 0.38f, Depth * 0.28f,
                _midpoint.X, _midpoint.Y * 1.5f, _midpoint.Z * -0.15f,
                effect, graphicsDevice
            );

            //Treadmills
            newHitbox.CreateSubHitboxSymmetricalToYZ(
                Width * 0.22f, Height * 0.41f, Depth * 0.6f,
                _midpoint.X + 135f, _midpoint.Y * 0.46f, _midpoint.Z * -0f,
                effect, graphicsDevice
            );

            //Tubes
            newHitbox.CreateSubHitboxSymmetricalToYZ(
                Width * 0.1f, Height * 0.4f, Depth * 0.03f,
                _midpoint.X - 37f, _midpoint.Y * 0.95f, _midpoint.Z * -1.9f,
                effect, graphicsDevice
            );

            _hitbox = newHitbox;
        }

        public void SetTurretRotation(float yaw, float pitch)
        {
            TurretYaw = yaw;
            CannonPitch = MathHelper.Clamp(
                pitch,
                alturaMinTorreta,
                alturaMaxTorreta
            );

            Debug.WriteLine($"set yaw={yaw} pitch={pitch}");
        }
        public void Update(GameTime gameTime)
        {
            MouseState mouse = Mouse.GetState();

            float deltaX = mouse.X - _previousMouseState.X;
            float deltaY = mouse.Y - _previousMouseState.Y;
            _previousMouseState = mouse;
            float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
            currentCoolDownSecs -= currentCoolDownSecs <= 0 ? 0 : elapsedSeconds;
            if (elapsedSeconds <= 0.0001f)
                return;
            float forwardInput = 0f;
            float turnInput = 0f;
            bool estaDoblando = false;
            bool estaAcelerando = false;
            KeyboardState keyboardState = Keyboard.GetState();

            if (keyboardState.IsKeyDown(Keys.W)) { forwardInput += 1; estaAcelerando = true; }
            if (keyboardState.IsKeyDown(Keys.S)) { forwardInput -= 1; estaAcelerando = true; }

            if (keyboardState.IsKeyDown(Keys.D)) { turnInput += 1; estaDoblando = true; }
            if (keyboardState.IsKeyDown(Keys.A)) { turnInput -= 1; estaDoblando = true; }

            //logica canon y torreta siguiendo el mouse

            Vector3 leftTrackGlobalPos = Vector3.Transform(new Vector3(_widthLocal * 0.385f, 0, 0), Matrix.CreateScale(_scale) * _rotation); ;
            Vector3 rightTrackGlobalPos = Vector3.Transform(new Vector3(-_widthLocal * 0.385f, 0, 0), Matrix.CreateScale(_scale) * _rotation);

            float currentForwardSpeed = Vector3.Dot(_velocity, _forward);

            float speedLimitFactor = 1f;
            if ((currentForwardSpeed > 0 && forwardInput > 0) || (currentForwardSpeed < 0 && forwardInput < 0))
            {
                speedLimitFactor = MathF.Max(0f, 1f - (MathF.Abs(currentForwardSpeed) / _maxSpeed));
            }

            float limitedForwardThrust = forwardInput * speedLimitFactor;


            float leftThrust = limitedForwardThrust + (turnInput * 1.5f);
            float rightThrust = limitedForwardThrust - (turnInput * 1.5f);


            Vector3 applicationPoint = leftTrackGlobalPos;
            float totalRaycastInContact = 0;
            foreach ((List<TankRaycast>, float) listThrust in GetBothRaycastLists(leftThrust, rightThrust))
            {
                var list = listThrust.Item1;
                var thrust = listThrust.Item2;
                var raycastsCount = list.Count;
                List<Force> forcesToApply = list.Select(r => r.CalculateSuspensionForce(this)).Where(f => f != null).ToList();
                float raycastsInContactCount = forcesToApply.Count;
                totalRaycastInContact += raycastsInContactCount;
                if (raycastsInContactCount == 0) continue; // Si la oruga no toca el suelo, no tracciona

                foreach (Force suspensionForce in forcesToApply)
                {
                    this.agregarFuerzaAAplicar(suspensionForce, elapsedSeconds);
                }

                Vector3 force = _forward * (thrust * _engineThrust);
                this.agregarFuerzaAAplicar(new Force(force, applicationPoint), elapsedSeconds);

                applicationPoint = rightTrackGlobalPos;
            }
            if (totalRaycastInContact > 0)
            {
                Vector3 globalGravity = new Vector3(0, -1000f, 0);
                aplicarFriccionLateral(elapsedSeconds, globalGravity);
                aplicarFriccionAngular(elapsedSeconds, estaDoblando);
                aplicarFriccionFrontal(elapsedSeconds, globalGravity, estaAcelerando);
            }
        }
        private void aplicarFriccionLateral(float elapsedSeconds, Vector3 globalGravity)
        {

            float gravitySidewaysForce = Vector3.Dot(globalGravity, _right) * _mass;
            float sidewaysSpeed = Vector3.Dot(_velocity, _right);
            float exactLateralForceToStop = (_mass * sidewaysSpeed) / elapsedSeconds + gravitySidewaysForce;
            Vector3 lateralForce = Vector3.Zero;


            if (MathF.Abs(sidewaysSpeed) < 10f)
            {
                lateralForce = -_right * exactLateralForceToStop;
            }
            else
            {
                float dynamicFriction = sidewaysSpeed * _mass * _lateralFriction;

                float maxFrictionAllowed = (_mass * MathF.Abs(sidewaysSpeed)) / elapsedSeconds;
                float appliedMag = MathF.Min(MathF.Abs(dynamicFriction), maxFrictionAllowed) * MathF.Sign(sidewaysSpeed);

                lateralForce = -_right * appliedMag;
            }

            this.agregarFuerzaAAplicar(new Force(lateralForce, Vector3.Zero), elapsedSeconds);

        }
        private void aplicarFriccionAngular(float elapsedSeconds, bool estaDoblando)
        {
            float angularDragFactor = 10.0f;
            var localInertia = _collider.GetInverseInertiaTensor();
            float yawInertia = 1.0f / localInertia.YY;
            float worldYawSpeed = Vector3.Dot(_angularVelocity, _up);
            Vector3 angularFrictionImpulse = Vector3.Zero;
            if (estaDoblando)
            {
                angularFrictionImpulse = -_up * worldYawSpeed * yawInertia * angularDragFactor;
            }
            else
            {
                float angularBrakeFactor = angularDragFactor * 10f;
                if (MathF.Abs(worldYawSpeed) < 0.05f)
                {
                    angularFrictionImpulse = -_up * (worldYawSpeed * yawInertia) / elapsedSeconds;
                }
                else
                {
                    angularFrictionImpulse = -_up * worldYawSpeed * yawInertia * angularBrakeFactor;
                }
            }

            this.agregarFuerzaAngularAAplicar(angularFrictionImpulse, elapsedSeconds);
        }
        private void aplicarFriccionFrontal(float elapsedSeconds, Vector3 globalGravity, bool estaAcelerando)
        {
            float forwardSpeed = Vector3.Dot(_velocity, this._forward); // Fricción suave hacia adelante
            Vector3 forwardFrictionImpulse = Vector3.Zero;
            if (estaAcelerando)
            {
                float rollingResistance = 0.1f;
                forwardFrictionImpulse = -this._forward * forwardSpeed * _mass * rollingResistance;
            }
            else
            {
                float brakeFactor = 30.0f;
                float gravityForwardForce = Vector3.Dot(globalGravity, this._forward) * _mass;

                if (MathF.Abs(forwardSpeed) < 10f)
                {
                    float exactForwardForce = ((forwardSpeed * _mass) / elapsedSeconds) + gravityForwardForce;
                    forwardFrictionImpulse = -this._forward * exactForwardForce;
                }
                else
                {
                    forwardFrictionImpulse = -this._forward * forwardSpeed * _mass * brakeFactor;
                }
            }
            this.agregarFuerzaAAplicar(new Force(forwardFrictionImpulse, Vector3.Zero), elapsedSeconds);
        }
        private List<(List<TankRaycast>, float)> GetBothRaycastLists(float leftThrust, float rightThrust)
        {
            return new List<(List<TankRaycast>, float)> { (raycastPointsLeft, leftThrust), (raycastPointsRight, rightThrust) };
        }
        public void LoadPhysics(Simulation simulation, Vector3 gravity)
        {
            LoadRaycastPoints(simulation);
            LoadConstants(gravity);
        }
        private void LoadConstants(Vector3 gravity)
        {
            float raycastCount = raycastPointsLeft.Count + raycastPointsRight.Count;
            float gravityMagnitude = gravity.Length();

            float massPerWheel = _mass / raycastCount;

            suspension = massPerWheel * gravityMagnitude / (0.1f * desiredHeight);
            cushioning = cushioningDamping * 2f * MathF.Sqrt(suspension * massPerWheel);
        }
        private void LoadRaycastPoints(Simulation simulation)
        {
            var leftCenterUR = new Vector3(_widthLocal * 0.31f, -_heightLocal * -0.01f, _depthLocal * 0.18f);
            var leftCenterUL = new Vector3(_widthLocal * 0.46f, -_heightLocal * -0.01f, _depthLocal * 0.18f);
            var leftCenterDL = new Vector3(_widthLocal * 0.46f, -_heightLocal * -0.01f, -_depthLocal * 0.18f);
            var leftCenterDR = new Vector3(_widthLocal * 0.31f, -_heightLocal * -0.01f, -_depthLocal * 0.18f);

            var rotacionFoward = Quaternion.CreateFromAxisAngle(
                new Vector3(1, 0, 0),
                -MathHelper.Pi / 9.7f
            );

            var leftFowardUR = new Vector3(_widthLocal * 0.31f, -_heightLocal * -0.02f, _depthLocal * 0.2f) + Vector3.Transform(new Vector3(0, 0, _depthLocal * 0.06f), rotacionFoward);
            var leftFowardUL = new Vector3(_widthLocal * 0.46f, -_heightLocal * -0.02f, _depthLocal * 0.2f) + Vector3.Transform(new Vector3(0, 0, _depthLocal * 0.06f), rotacionFoward);
            var leftFowardDL = new Vector3(_widthLocal * 0.46f, -_heightLocal * -0.02f, _depthLocal * 0.2f);
            var leftFowardDR = new Vector3(_widthLocal * 0.31f, -_heightLocal * -0.02f, _depthLocal * 0.2f);

            var rotacionBack = Quaternion.CreateFromAxisAngle(
                new Vector3(1, 0, 0),
                MathHelper.Pi / 13f
            );
            var leftBackUR = new Vector3(_widthLocal * 0.31f, -_heightLocal * -0.02f, -_depthLocal * 0.2f);
            var leftBackUL = new Vector3(_widthLocal * 0.46f, -_heightLocal * -0.02f, -_depthLocal * 0.2f);
            var leftBackDL = new Vector3(_widthLocal * 0.46f, -_heightLocal * -0.02f, -_depthLocal * 0.2f) + Vector3.Transform(new Vector3(0, 0, -_depthLocal * 0.07f), rotacionBack);
            var leftBackDR = new Vector3(_widthLocal * 0.31f, -_heightLocal * -0.02f, -_depthLocal * 0.2f) + Vector3.Transform(new Vector3(0, 0, -_depthLocal * 0.07f), rotacionBack);

            var raycastsLeftCenter = CreateRaycastPointsInSquare(leftCenterDL, leftCenterDR, leftCenterUL, leftCenterUR, simulation);
            var raycastsLeftFoward = CreateRaycastPointsInSquare(leftFowardDL, leftFowardDR, leftFowardUL, leftFowardUR, simulation);
            var raycastsLeftBack = CreateRaycastPointsInSquare(leftBackDL, leftBackDR, leftBackUL, leftBackUR, simulation);

            var raycastsRightCenter = InvertirXDeTankRaycast(raycastsLeftCenter);
            var raycastsRightFoward = InvertirXDeTankRaycast(raycastsLeftFoward);
            var raycastsRightBack = InvertirXDeTankRaycast(raycastsLeftBack);

            raycastPointsLeft = raycastPointsLeft.Concat(raycastsLeftCenter).Concat(raycastsLeftBack).Concat(raycastsLeftFoward).ToList();
            raycastPointsRight = raycastPointsRight.Concat(raycastsRightCenter).Concat(raycastsRightBack).Concat(raycastsRightFoward).ToList();

        }
        private List<TankRaycast> InvertirXDeTankRaycast(List<TankRaycast> raycasts)
        {
            return raycasts.Select(r =>
            new TankRaycast(new Vector3(r._positionLocal.X * -1, r._positionLocal.Y, r._positionLocal.Z), r._directionLocal, r._simulation)).ToList();
        }
        private List<TankRaycast> CreateRaycastPointsInSquare(Vector3 point1, Vector3 point2, Vector3 point3, Vector3 point4, Simulation simulation)
        {
            var direction = Vector3.Cross(point2 - point1, point3 - point1);
            if (direction.Y > 0)
            {
                direction *= -1;
            }
            List<Vector3> points = CreatePointsInSquare(point1, point2, point3, point4);
            return points.Select(p => new TankRaycast(p, direction, simulation)).ToList();
        }
        private List<Vector3> CreatePointsInSquare(Vector3 point1, Vector3 point2, Vector3 point3, Vector3 point4)
        {

            List<Vector3> points = new List<Vector3> { point1, point2, point3, point4 };

            points = points.OrderBy(p => (p - point1).Length()).ToList();
            var dx = (points[1] - points[0]).Length() / quantityOfHorizontalRaycasts;
            var dy = (points[2] - points[0]).Length() / quantityOfVerticalRaycasts;
            var unitX = Vector3.Normalize(points[1] - points[0]) * dx;
            var unitY = Vector3.Normalize(points[2] - points[0]) * dy;
            var result = new List<Vector3>();
            for (var curX = 0f; (unitX * curX).Length() <= (points[1] - points[0]).Length(); curX++)
            {
                for (var curY = 0f; (unitY * curY).Length() <= (points[2] - points[0]).Length(); curY++)
                {
                    result.Add(points[0] + unitX * curX + unitY * curY);
                }
            }
            return result;
        }

        public override Vector3 Correction()
        {
            return Vector3.Transform(_collider._centerOfMass * 0.1f, Matrix.CreateScale(_scale) * _rotation);
        }
        public override void CreateCollider(Simulation simulation, BufferPool bufferPool, Effect effect, GraphicsDevice graphicsDevice)
        {
            _collider = new TankCollider(this, simulation, bufferPool, effect, graphicsDevice);
            LoadPhysics(simulation, TGCGame.gravity);
        }

        public Matrix GetCannonRotation()
        {
            return
                Matrix.CreateRotationX(CannonPitch) *
                Matrix.CreateRotationY(TurretYaw) *
                _rotation;
        }

        public Vector3 GetCannonDirection()
        {
            Matrix cannonRotation = GetCannonRotation();

            Vector3 direction = -cannonRotation.Forward;

            direction.Normalize();

            return direction;
        }

        public Vector3 GetCannonMuzzlePosition()
        {
            Matrix cannonRotation = GetCannonRotation();

            Vector3 direction = -cannonRotation.Forward;
            Vector3 up = cannonRotation.Up;
            Vector3 right = -cannonRotation.Right;

            direction.Normalize();
            up.Normalize();
            right.Normalize();

            float verticalOffset = Height * 0.774f;
            float forwardOffset = Depth * 0.64f;
            float lateralOffset = -5.7f;

            return
                _position +
                up * verticalOffset +
                direction * forwardOffset +
                right * lateralOffset;
        }
        public override void CollidedWith(ModelInScene model, Vector3 contactPointOffset, Vector3 thisPos, float impactImpulse, float destructiveImpactImpulse, Vector3 normal)
        {
            // golpe metalico al chocar fuerte contra algo que no sea el piso
            // la pausa evita que se repita porque este metodo se llama en cada frame mientras dura el contacto
            float velocidadDelGolpe = impactImpulse * InverseMass();
            if (model is not SimpleTerrain && velocidadDelGolpe > minCrashSoundSpeed &&
                Environment.TickCount64 - lastCrashSoundTicks > crashSoundCoolDownMs)
            {
                TGCGame.CrashSound.Play();
                lastCrashSoundTicks = Environment.TickCount64;
            }

            if (model.IsDestroyedWith(destructiveImpactImpulse))
            {
                float breakImpulse = Math.Min(impactImpulse, model.strength);

                Vector3 cambioDeVelocidad = -normal * breakImpulse * this.InverseMass();
                _collider.ApplyImpulse(UtilsClass.ToNumericVector(cambioDeVelocidad), UtilsClass.ToNumericVector(contactPointOffset));
                health -= 10f;
            }
        }
        public override bool ShouldCollideWith(ModelInScene model, Vector3 contactPointOffset, Vector3 thisPos, float impactImpulse, float destructiveImpactImpulse, Vector3 normal)
        {
            if (model.IsDestroyedWith(destructiveImpactImpulse))
            {
                return false;
            }
            return true;
        }
        public bool ShouldFireMissile(GameTime gameTime, out Vector3 direction, out Vector3 position, out Matrix rotation)
        {
            KeyboardState keyboardState = Keyboard.GetState();

            if (keyboardState.IsKeyDown(Keys.X) && currentCoolDownSecs <= 0)
            {
                direction =
                GetCannonDirection();

                position =
                GetCannonMuzzlePosition();

                rotation =
                GetCannonRotation();
                currentCoolDownSecs = coolDownSecs;
                return true;
            }
            direction = Vector3.Zero;
            position = Vector3.Zero;
            rotation = Matrix.Identity;
            return false;
        }
        public override void CollidedWithMissil(Missil missil)
        {
            Vector3 missilLocal = Vector3.Transform(missil._position, Matrix.Invert(_world));

            float maxDistanceToCylinder = missil.Width * 7f;
            float maxDistanceAlongAxis = missil.Depth * 6f;
            float deformationDepth = missil.Depth * 1f;
            Vector3 missilDirectionNormalized = Vector3.Normalize(Vector3.TransformNormal(missil.Direction, Matrix.Invert(_world)));
            List<ModelMeshPart> modelMeshParts = verticesPorMeshPart.Keys.ToList();

            var modelMeshesBaseTransforms = new Matrix[_model.Bones.Count];

            _model.CopyAbsoluteBoneTransformsTo(modelMeshesBaseTransforms);

            foreach (ModelMeshPart modelMeshPart in modelMeshParts)
            {
                ModelMesh mesh = verticesPorMeshPart[modelMeshPart].Item2;
                if (mesh.Name.Equals("Hull") || mesh.Name.Equals("Turret"))
                {
                    VertexPositionNormalTexture[] vertexs = verticesPorMeshPart[modelMeshPart].Item1;
                    var relativeTransform = modelMeshesBaseTransforms[mesh.ParentBone.Index];
                    if (mesh.Name.Equals("Turret"))
                    {
                        relativeTransform *= GetCannonRotation();
                    }
                    Matrix invertRelative = Matrix.Invert(relativeTransform);

                    int[] indices = _indexBuffers[modelMeshPart];
                    bool[] fueModificado = new bool[vertexs.Length];

                    for (int i = 0; i < indices.Length; i++)
                    {
                        int indiceActual = indices[i];
                        if (!fueModificado[indiceActual])
                        {
                            vertexs[indiceActual].Normal = Vector3.Zero;
                            Vector3 vertexPositionModelSpace = Vector3.Transform(vertexs[indiceActual].Position, relativeTransform);

                            Vector3 vectorToVertex = vertexPositionModelSpace - missilLocal;

                            float distanceAlongAxis = Vector3.Dot(vectorToVertex, missilDirectionNormalized);

                            float distanceToLine = Vector3.Cross(Vector3.Subtract(vertexPositionModelSpace, missilLocal), missilDirectionNormalized).Length();

                            if (distanceToLine < maxDistanceToCylinder && Math.Abs(distanceAlongAxis) < maxDistanceAlongAxis)
                            {
                                Vector3 missilDirectionNormalizedBoneSpace = Vector3.TransformNormal(missilDirectionNormalized, invertRelative);
                                if (!deformacionPorVertice.TryGetValue((modelMeshPart, indiceActual), out float deformacionActual))
                                {
                                    deformacionActual = 0f;
                                }
                                float deformacionNueva = deformationDepth * (1 - distanceToLine / maxDistanceToCylinder);
                                if (deformacionActual + deformacionNueva > maxDeformation)
                                {
                                    deformacionNueva = maxDeformation - deformacionActual;
                                }
                                deformacionPorVertice[(modelMeshPart, indiceActual)] = deformacionActual + deformacionNueva;
                                vertexs[indiceActual].Position += missilDirectionNormalizedBoneSpace * deformacionNueva;
                                fueModificado[indiceActual] = true;
                            }
                        }

                        //Si está en el último vértice del triángulo recalcula las normales
                        if (i % 3 == 2)
                        {
                            int i1 = indices[i - 2];
                            int i2 = indices[i - 1];
                            int i3 = indiceActual;

                            Vector3 arista1 = vertexs[i3].Position - vertexs[i1].Position;
                            Vector3 arista2 = vertexs[i2].Position - vertexs[i1].Position;
                            Vector3 normalAlTriangulo = Vector3.Cross(arista1, arista2);

                            vertexs[i1].Normal += normalAlTriangulo;
                            vertexs[i2].Normal += normalAlTriangulo;
                            vertexs[i3].Normal += normalAlTriangulo;
                        }
                    }


                    for (int i = 0; i < vertexs.Length; i++)
                    {
                        if (vertexs[i].Normal != Vector3.Zero)
                        {
                            vertexs[i].Normal = Vector3.Normalize(vertexs[i].Normal);
                        }
                    }

                    int stride = modelMeshPart.VertexBuffer.VertexDeclaration.VertexStride;
                    _dynamicVertexBuffers[modelMeshPart].SetData(
                        0,
                        vertexs,
                        0,
                        vertexs.Length,
                        stride
                    );
                }
            }
        }
        protected override void cargarModelo(ContentManager Content, string modelPath)
        {
            base.cargarModelo(Content, modelPath);
            foreach (var mesh in this._model.Meshes)
            {
                foreach (var meshPart in mesh.MeshParts)
                {

                    VertexPositionNormalTexture[] vertexs = new VertexPositionNormalTexture[meshPart.NumVertices];

                    int vertexStride = meshPart.VertexBuffer.VertexDeclaration.VertexStride;
                    int offsetInBytes = meshPart.VertexOffset * vertexStride;

                    meshPart.VertexBuffer.GetData(
                        offsetInBytes,
                        vertexs,
                        0,
                        meshPart.NumVertices,
                        vertexStride
                    );
                    DynamicVertexBuffer dynamicVB = new DynamicVertexBuffer(
                        _renderer._graphicsDevice,
                        VertexPositionNormalTexture.VertexDeclaration,
                        vertexs.Length,
                        BufferUsage.WriteOnly
                    );

                    dynamicVB.SetData(vertexs);

                    _dynamicVertexBuffers[meshPart] = dynamicVB;

                    IndexBuffer indexBuffer = meshPart.IndexBuffer;
                    int cantIndices = meshPart.PrimitiveCount * 3;
                    int[] indices = new int[cantIndices];
                    if (indexBuffer.IndexElementSize == IndexElementSize.SixteenBits)
                    {
                        ushort[] indicesCortos = new ushort[cantIndices];
                        indexBuffer.GetData(meshPart.StartIndex * 2, indicesCortos, 0, cantIndices);

                        for (int i = 0; i < cantIndices; i++)
                        {
                            indices[i] = indicesCortos[i];
                        }
                    }
                    else
                    {
                        indexBuffer.GetData(meshPart.StartIndex * 4, indices, 0, cantIndices);
                    }
                    _indexBuffers[meshPart] = indices;
                    verticesPorMeshPart[meshPart] = (vertexs, mesh);
                }
            }

        }
    }
}

