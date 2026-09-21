using System;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TGC.MonoGame.TP.ModelsInScene;
using TGC.MonoGame.TP.Renderers;
using TGC.MonoGame.TP.Forces;
using TGC.MonoGame.TP.Contact;
using System.Collections.Generic;
using System.Linq;
using NumericVector = System.Numerics.Vector3;
using TGC.MonoGame.TP.TankRaycasts;
using BepuPhysics;
using Microsoft.VisualBasic;
using TGC.MonoGame.Utils;

namespace TGC.MonoGame.TP.Tanks
{
    public class Tank : ModelInScene
    {

        //public List<ContactPoint> leftContactPoints { get; } = new List<ContactPoint>();
        //public List<ContactPoint> rightContactPoints { get; } = new List<ContactPoint>();
        public Vector3 centerOfMass { get; set; } = Vector3.Zero;
        public List<TankRaycast> raycastPointsLeft = new List<TankRaycast>();
        public List<TankRaycast> raycastPointsRight = new List<TankRaycast>();
        public float _maxSpeed { get; set; } = 500f;
        public float _engineThrust { get; set; } = 7000000f;

        public float _lateralFriction { get; set; } = 15f;
        public Tank(ContentManager Content, String modelPath, Vector3 position, Matrix rotation, Vector3 scale, IModelInSceneRenderer renderer, float linearDrag, float angularDrag, float mass)
            : base(Content, modelPath, position, rotation, scale, renderer, linearDrag, angularDrag, mass)
        {
        }
        public void Update(GameTime gameTime)
        {
            float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (elapsedSeconds <= 0.0001f)
                return;
            float leftThrust = 0f;
            float rightThrust = 0f;
            KeyboardState keyboardState = Keyboard.GetState();

            if (keyboardState.IsKeyDown(Keys.W))
            {
                leftThrust += 1;
                rightThrust += 1;
            }
            if (keyboardState.IsKeyDown(Keys.S)) { leftThrust -= 1f; rightThrust -= 1f; }

            if (keyboardState.IsKeyDown(Keys.D)) { leftThrust += 0.5f; rightThrust -= 0.5f; }
            if (keyboardState.IsKeyDown(Keys.A)) { leftThrust -= 0.5f; rightThrust += 0.5f; }

            Vector3 leftTrackLocalPos = -_right * Width / 2f;
            Vector3 rightTrackLocalPos = _right * Width / 2f;

            float currentForwardSpeed = Vector3.Dot(_velocity, _forward);

            foreach ((List<TankRaycast>, float) listThrust in GetBothRaycastLists(leftThrust, rightThrust))
            {
                var list = listThrust.Item1;
                var thrust = listThrust.Item2;
                var raycastsCount = list.Count;
                var raycastsInContact = list.Where(r => r.CalculateRaycast(this)).ToList();
                float raycastsInContactCount = raycastsInContact.Count;

                float speedInThrustDir = currentForwardSpeed * MathF.Sign(thrust);
                float torqueMultiplier = 1f;

                // Solo limitamos el motor si estamos yendo a favor del movimiento y superando el límite
                if (speedInThrustDir > 0)
                {
                    // Interpola de 1 a 0 a medida que speedInThrustDir se acerca a _maxSpeed
                    torqueMultiplier = MathF.Max(0f, 1f - (speedInThrustDir / _maxSpeed));
                }


                foreach (TankRaycast raycast in raycastsInContact)
                {
                    var raycastDir = Vector3.Transform(raycast._directionLocal, _rotation);
                    Vector3 forceDirection = Vector3.Normalize(_forward - raycastDir * Vector3.Dot(_forward, raycastDir) / MathF.Pow(raycastDir.Length(), 2));
                    Vector3 force = forceDirection * (thrust * _engineThrust * torqueMultiplier / raycastsInContactCount);
                    this.agregarFuerzaAAplicar(new Force(force, raycast.PositionGlobalRelativeToCenter(this)), elapsedSeconds);
                }

            }

            float sidewaysSpeed = Vector3.Dot(_velocity, _right);
            float exactForceToStop = (_mass * sidewaysSpeed) / elapsedSeconds;

            float frictionMagnitude = sidewaysSpeed * _mass * _lateralFriction;

            float appliedFrictionMag = MathF.Min(MathF.Abs(frictionMagnitude), MathF.Abs(exactForceToStop));

            appliedFrictionMag *= MathF.Sign(frictionMagnitude);

            Vector3 lateralForce = -_right * appliedFrictionMag;
            this.agregarFuerzaAAplicar(new Force(lateralForce, Vector3.Zero), elapsedSeconds);

        }
        private List<(List<TankRaycast>, float)> GetBothRaycastLists(float leftThrust, float rightThrust)
        {
            return new List<(List<TankRaycast>, float)> { (raycastPointsLeft, leftThrust), (raycastPointsRight, rightThrust) };
        }
        public void LoadRaycastPoints(Simulation simulation)
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
            var dx = (points[1] - points[0]).Length() / 16f;
            var dy = (points[2] - points[0]).Length() / 32f;
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
        public override void Draw(Matrix view, Matrix projection)
        {
            _renderer.Draw(this, view, projection);
        }
        public override Vector3 Correction()
        {
            return Vector3.Transform(centerOfMass * 3.1f, Matrix.CreateScale(_scale) * _rotation);
        }
    }

}