using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;


namespace TGC.MonoGame.TP.HUD
{
    public class MainMenu
    {
        private int _selectedOption = 0;
        private KeyboardState _previousKeyboard;
        public bool StartGame { get; private set; }
        public bool LeaveGame { get; private set; }
        private readonly Texture2D _pixel;
        private readonly SpriteFont _font;
        public string[] Options =
        {
            "JUGAR",
            "SALIR"
        };

        public MainMenu(Texture2D pixel, SpriteFont font)
        {
            _pixel = pixel;
            _font = font;
        }

        public void Update()
        {
            KeyboardState keyboard = Keyboard.GetState();


            if (keyboard.IsKeyDown(Keys.Enter))
            {
                if (_selectedOption == 0)
                {
                    StartGame = true;
                }
                if (_selectedOption == 1)
                    LeaveGame = true;

            }

            if (keyboard.IsKeyDown(Keys.Down))
                _selectedOption++;

            if (keyboard.IsKeyDown(Keys.Up))
                _selectedOption--;

            _selectedOption = Math.Clamp(_selectedOption, 0, Options.Length - 1);
            _previousKeyboard = keyboard;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.GraphicsDevice.Clear(Color.Black);
            int centerX = spriteBatch.GraphicsDevice.Viewport.Width / 2;
            int centerY = spriteBatch.GraphicsDevice.Viewport.Height / 2;
            Console.WriteLine(centerX);
            Console.WriteLine(centerY);
            spriteBatch.Begin();



            spriteBatch.Draw(
        _pixel,
        new Rectangle(centerX - 200, centerY -50, 400, 80),
        _selectedOption == 0
            ? Color.Green
            : Color.Gray);

            spriteBatch.Draw(
                _pixel,
                new Rectangle(centerX - 200, centerY + 50, 400, 80),
                _selectedOption == 1
                    ? Color.Green
                    : Color.Gray);

            spriteBatch.DrawString(_font,"JUGAR",new Vector2(centerX - 200, centerY - 50),Color.Black);
            spriteBatch.DrawString(_font, "SALIR", new Vector2(centerX - 200, centerY + 50), Color.Black);
            spriteBatch.End();

            // esto protege al terreno de no renderizase mal
            spriteBatch.GraphicsDevice.BlendState = BlendState.Opaque;
            spriteBatch.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            spriteBatch.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
        }
    }
}