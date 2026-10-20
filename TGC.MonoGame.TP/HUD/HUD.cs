using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;


namespace TGC.MonoGame.TP.HUD {
    public class HUD {
        private readonly Texture2D _pixel;

        public HUD(Texture2D pixel) {
            _pixel = pixel;
        }

        public void Draw(SpriteBatch spriteBatch) {
            int centerX = spriteBatch.GraphicsDevice.Viewport.Width / 2;
            int centerY = spriteBatch.GraphicsDevice.Viewport.Height / 2;
            spriteBatch .Begin();
            //mira 
            spriteBatch.Draw(_pixel,new Rectangle(centerX - 20, centerY, 10, 2),Color.Red);
            spriteBatch .Draw(_pixel,new Rectangle(centerX + 10, centerY, 10, 2),Color.Red);
            spriteBatch .Draw(_pixel,new Rectangle(centerX, centerY - 20, 2, 10),Color.Red);
            spriteBatch .Draw(_pixel,new Rectangle(centerX, centerY + 10, 2, 10),Color.Red);
            DrawCircle( spriteBatch, _pixel, new Vector2(centerX, centerY), 1, Color.Red);
            DrawCircle( spriteBatch, _pixel, new Vector2(centerX, centerY), 20, Color.Red);
            
            //barra de vida spriteBatch.Draw(_pixel, new Rectangle(15, 15, 210, 30),Color.Black);
            spriteBatch .Draw(_pixel,new Rectangle(20, 20, 200, 20),Color.DarkRed);
            spriteBatch .Draw(_pixel,new Rectangle(20, 20, /*vidaActual*/ 50 * 2, 20),Color.LimeGreen);
            
            //minimap
            spriteBatch.Draw(_pixel,new Rectangle(spriteBatch.GraphicsDevice.Viewport.Width - 220,20,200,200),Color.Black * 0.5f);
            
            spriteBatch .End();
            // esto protege al terreno de no renderizase mal spriteBatch.GraphicsDevice.BlendState = BlendState.Opaque;
            spriteBatch .GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            spriteBatch .GraphicsDevice.RasterizerState = RasterizerState.CullNone;
        }

        public void DrawCircle( SpriteBatch spriteBatch, Texture2D pixel, Vector2 center, float radius, Color color) {
            int segments = 64;

            for (int i = 0; i < segments; i++) {
                float angle = MathHelper.TwoPi * i / segments;
                float x = center.X + (float)Math.Cos(angle) * radius;
                float y = center.Y + (float)Math.Sin(angle) * radius;
                spriteBatch .Draw( pixel, new Rectangle((int)x, (int)y, 2, 2), color);
            }
        }
    }
}
