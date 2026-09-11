using System;

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
            Mouse.SetPosition(graphicsDevice.Viewport.Width / 2, graphicsDevice.Viewport.Height / 2);
            float cosPitch = MathF.Cos(_pitch);

            Position = new Vector3(MathF.Cos(_turn) * cosPitch, MathF.Sin(_pitch), MathF.Sin(_turn) * cosPitch) * distanceToModel; ;
            FrontDirection = DefaultWorldFrontVector;
            Speed = speed;
            Angle = angle;
            View = Matrix.CreateLookAt(modelThatFollows._position + Position, modelThatFollows._position, DefaultWorldUpVector);
        }

        /// <inheritdoc />
        public override void Update(GameTime gameTime)
        {

            _currentMouseState = Mouse.GetState();

            float deltaX = (_currentMouseState.X - graphicsDevice.Viewport.Width / 2) * _sensitivity;
            float deltaY = (_currentMouseState.Y - graphicsDevice.Viewport.Height / 2) * _sensitivity;

            Mouse.SetPosition(graphicsDevice.Viewport.Width / 2, graphicsDevice.Viewport.Height / 2);

            _turn += deltaX;
            _pitch = MathHelper.Clamp(_pitch + deltaY, -MathHelper.PiOver2 + 0.01f, MathHelper.PiOver2 - 0.05f);

            float cosPitch = MathF.Cos(_pitch);

            Position = new Vector3(MathF.Cos(_turn) * cosPitch, MathF.Sin(_pitch), MathF.Sin(_turn) * cosPitch) * distanceToModel;

            View = Matrix.CreateLookAt(modelThatFollows._position + Position, modelThatFollows._position, DefaultWorldUpVector);
        }
    }
}
