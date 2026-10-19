using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;



namespace TGC.MonoGame.TP.HUD
{
    public class HUD
    {
        private readonly Texture2D _pixel;

        public HUD(Texture2D pixel)
        {
            _pixel = pixel;
        }

        public void Draw(SpriteBatch spriteBatch)
        {

            int centerX = spriteBatch.GraphicsDevice.Viewport.Width / 2;
            int centerY = spriteBatch.GraphicsDevice.Viewport.Height / 2;
            spriteBatch.Begin();

            spriteBatch.Draw(
                _pixel,
                new Rectangle(centerX - 15, centerY, 10, 2),
                Color.White);

            spriteBatch.Draw(
                _pixel,
                new Rectangle(centerX + 5, centerY, 10, 2),
                Color.White);

            spriteBatch.Draw(
                _pixel,
                new Rectangle(centerX, centerY - 15, 2, 10),
                Color.White);

            spriteBatch.Draw(
                _pixel,
                new Rectangle(centerX, centerY + 5, 2, 10),
                Color.White);
            
            //barra de vida
            spriteBatch.Draw(_pixel, new Rectangle(15, 15, 210, 30),
            Color.Black);
            spriteBatch.Draw(_pixel,
                new Rectangle(20, 20, 200, 20),
            Color.DarkRed);

            spriteBatch.Draw(_pixel,
                new Rectangle(20, 20, /*vidaActual*/50 * 2, 20),
                Color.LimeGreen);


            //minimap
            spriteBatch.Draw(
        _pixel,
        new Rectangle(
            spriteBatch.GraphicsDevice.Viewport.Width - 220,
            20,
            200,
            200),
        Color.Black * 0.5f);

            spriteBatch.End();



            // esto protege al terreno de no renderizase mal
            spriteBatch.GraphicsDevice.BlendState = BlendState.Opaque;
            spriteBatch.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            spriteBatch.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
        }
    }
}