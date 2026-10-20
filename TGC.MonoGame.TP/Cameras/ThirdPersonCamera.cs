using System;
using TGC.MonoGame.TP.Tanks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TGC.MonoGame.TP.ModelsInScene;
using System.Diagnostics;
namespace TGC.MonoGame.TP.Cameras
{
    /// <summary>
    ///     Camera with simple movement.
    /// </summary>
    public class ThirdPersonCamera : Camera
    {
        /// <summary>
        ///     Forward direction of the camera.
        /// </summary>
        public readonly Vector3 DefaultWorldFrontVector = Vector3.Forward;

        /// <summary>
        ///     The direction that is "up" from the camera's point of view.
        /// </summary>
        public readonly Vector3 DefaultWorldUpVector = Vector3.Up;
        private MouseState _currentMouseState = new MouseState();

        private float _sensitivity;
        public readonly ModelInScene modelThatFollows;
        public readonly float distanceToModel;
        private float _turn = -MathHelper.PiOver2;
        private float _pitch = MathHelper.PiOver4;
        public float _cameraYaw =  MathHelper.Pi * 2;
        public float _cameraPitch = 0f;
        private float sensibilidadX = 0.004f;
        private float sensibilidadY = 0.005f;
        private float alturaMaxCamara = 0.6f;
        private float alturaMinCamara = 0f;
        private MouseState _previousMouseState;
        

        

        private readonly GraphicsDevice graphicsDevice;
        /// <summary>
        ///     Camera with simple movement to be able to move in the 3D world, which has the up vector in (0,1,0) and the forward
        ///     vector in (0,0,-1).
        /// </summary>
        /// <param name="aspectRatio">Aspect ratio, defined as view space width divided by height.</param>
        /// <param name="position">The position of the camera.</param>
        /// <param name="speed">The speed of movement.</param>
        /// <param name="angle">The angle of movement.</param>
        /// <param name="nearPlaneDistance">Distance to the near view plane.</param>
        /// <param name="farPlaneDistance">Distance to the far view plane.</param>
        public ThirdPersonCamera(ModelInScene modelThatFollows, float distanceToModel, float sensitivity, float aspectRatio, float speed, float angle, float nearPlaneDistance,
            float farPlaneDistance, GraphicsDevice graphicsDevice)
            : base(aspectRatio, nearPlaneDistance, farPlaneDistance)
        {
            this.graphicsDevice = graphicsDevice;
            this.distanceToModel = distanceToModel;
            this._sensitivity = sensitivity;
            this.modelThatFollows = modelThatFollows;
            BuildView(Position, speed, angle);
        }

        /// <summary>
        ///     Value with which the camera is going to move.
        /// </summary>
        public float Speed { get; set; }

        /// <summary>
        ///     Value with which the camera is going to move the angle.
        /// </summary>
        public float Angle { get; set; }

        /// <summary>
        ///     Build view matrix and update the internal directions.
        /// </summary>
        /// <param name="position">The position of the camera.</param>
        /// <param name="speed">The speed of movement.</param>
        /// <param name="angle">The angle of movement.</param>
        private void BuildView(Vector3 position, float speed, float angle)
        {
            FrontDirection = DefaultWorldFrontVector;
            Speed = speed;
            Angle = angle;
        }

        /// <inheritdoc />
        public override void Update(GameTime gameTime)
        {

            MouseState mouse = Mouse.GetState();
            float deltaX = mouse.X - _previousMouseState.X;
            float deltaY = mouse.Y - _previousMouseState.Y;
            _previousMouseState = mouse;

            

            _cameraYaw -= deltaX * sensibilidadX ;
            _cameraPitch -= deltaY * sensibilidadY;

            //limites camara verticalmente
            _cameraPitch = MathHelper.Clamp(_cameraPitch,alturaMinCamara,alturaMaxCamara);
            
            Tank tank = (Tank)modelThatFollows;
            float hullYaw =(float)Math.Atan2(tank._forward.X,tank._forward.Z);
            tank.SetTurretRotation(_cameraYaw - hullYaw -MathHelper.Pi, -_cameraPitch);
            
            //con esto ajusto la distancia al tanque
            Vector3 offsetBase = new Vector3(0,distanceToModel * 0.35f,distanceToModel);

            Matrix rotation = Matrix.CreateRotationX(_cameraPitch) * Matrix.CreateRotationY(_cameraYaw);

            Position = Vector3.Transform(offsetBase, rotation);

            Vector3 lookDirection = Vector3.Transform(Vector3.Forward, rotation);

            Vector3 cameraPosition = modelThatFollows._position + Position;

            View = Matrix.CreateLookAt(cameraPosition,cameraPosition + lookDirection,Vector3.Up);
        }


    }
}
