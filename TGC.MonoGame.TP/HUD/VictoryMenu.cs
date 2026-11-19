using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
namespace TGC.MonoGame.TP.HUD
{
    public class VictoryMenu
    {
        private int _selectedOption = 0;
        private KeyboardState _previousKeyboard;
        private readonly Texture2D _pixel;
        private readonly SpriteFont _font;

        public bool RestartGame { get; set; }
        public bool BackToMenu { get; set; }

        public string[] Options =
        {
            "MENU PRINCIPAL",
            "Reiniciar Juego"
        };
        public VictoryMenu(Texture2D pixel, SpriteFont font)
        {
            _pixel = pixel;
            _font = font;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin();
            // Draw a semi-transparent background
            spriteBatch.Draw(_pixel, new Rectangle(0, 0, spriteBatch.GraphicsDevice.Viewport.Width, spriteBatch.GraphicsDevice.Viewport.Height), Color.Black * 0.5f);

            // Draw the victory message
            string victoryMessage = "Ganaste!";
            Vector2 messageSize = _font.MeasureString(victoryMessage);
            Vector2 messagePosition = new Vector2((spriteBatch.GraphicsDevice.Viewport.Width - messageSize.X) / 2, ((spriteBatch.GraphicsDevice.Viewport.Height - messageSize.Y) / 2) - 50);
            spriteBatch.DrawString(_font, victoryMessage, messagePosition, Color.White);

            int centerX = spriteBatch.GraphicsDevice.Viewport.Width / 2;
            int centerY = spriteBatch.GraphicsDevice.Viewport.Height / 2;

            spriteBatch.Draw(_pixel, new Rectangle(centerX - 205, centerY - 25, 410, 90), Color.Black);
            spriteBatch.Draw(_pixel, new Rectangle(centerX - 200, centerY - 20, 400, 80), _selectedOption == 0 ? Color.Green : Color.Gray);
            spriteBatch.DrawString(_font, "Reiniciar Juego", new Vector2(centerX - 100, centerY - 0), Color.Black);

            spriteBatch.Draw(_pixel, new Rectangle(centerX - 205, centerY + 75, 410, 90), Color.Black);
            spriteBatch.Draw(_pixel, new Rectangle(centerX - 200, centerY + 80, 400, 80), _selectedOption == 1 ? Color.Green : Color.Gray);
            spriteBatch.DrawString(_font, "Volver al Menu", new Vector2(centerX - 100, centerY + 100), Color.Black);
            spriteBatch.End();
        }
        public void Update(GameTime gameTime)
        {
            KeyboardState keyboard = Keyboard.GetState();


            if (keyboard.IsKeyDown(Keys.Enter) && _previousKeyboard.IsKeyUp(Keys.Enter))
            {
                TGCGame.ClickSound.Play();
                if (_selectedOption == 0)
                {
                    RestartGame = true;
                }
                if (_selectedOption == 1)
                    BackToMenu = true;

            }

            if (keyboard.IsKeyDown(Keys.Down) && _previousKeyboard.IsKeyUp(Keys.Down))
            {
                TGCGame.ClickSound.Play();
                _selectedOption++;
            }

            if (keyboard.IsKeyDown(Keys.Up) && _previousKeyboard.IsKeyUp(Keys.Up))
            {
                TGCGame.ClickSound.Play();
                _selectedOption--;
            }

            _selectedOption = Math.Clamp(_selectedOption, 0, Options.Length - 1);
            _previousKeyboard = keyboard;
        }

     
    }
}