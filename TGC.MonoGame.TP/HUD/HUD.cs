using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Tanks;
using TGC.MonoGame.TP.Trees;
using System.Collections.Generic;
using System;


namespace TGC.MonoGame.TP.HUD
{
    public class HUD
    {
        private readonly Texture2D _pixel;
        private int mapSize = 200;
        private Tank _tank;

        public HUD(Texture2D pixel, Tank tank)
        {
            _pixel = pixel;
            _tank = tank;
        }

        public void Draw(SpriteBatch spriteBatch, Tank player, List<Tank> tanks)
        {
            float vidaActual = _tank.health;
            float vidaMaxima = _tank.maxHealth;

            int centerX = spriteBatch.GraphicsDevice.Viewport.Width / 2;
            int centerY = spriteBatch.GraphicsDevice.Viewport.Height / 2;
            spriteBatch.Begin();
            //mira
            spriteBatch.Draw(_pixel, new Rectangle(centerX - 20, centerY, 10, 2), Color.Red);
            spriteBatch.Draw(_pixel, new Rectangle(centerX + 10, centerY, 10, 2), Color.Red);
            spriteBatch.Draw(_pixel, new Rectangle(centerX, centerY - 20, 2, 10), Color.Red);
            spriteBatch.Draw(_pixel, new Rectangle(centerX, centerY + 10, 2, 10), Color.Red);
            DrawCircle(spriteBatch, _pixel, new Vector2(centerX, centerY), 1, Color.Red);
            DrawCircle(spriteBatch, _pixel, new Vector2(centerX, centerY), 20, Color.Red);
            //barra de vida
            spriteBatch.Draw(_pixel, new Rectangle(15, 15, 210, 30), Color.Black);
            spriteBatch.Draw(_pixel, new Rectangle(20, 20, 200, 20), Color.DarkRed);
            spriteBatch.Draw(_pixel, new Rectangle(20, 20, (int)((vidaActual / vidaMaxima) * 50f * 4f), 20), Color.LimeGreen);
            //minimap 
            spriteBatch.Draw(_pixel, new Rectangle(spriteBatch.GraphicsDevice.Viewport.Width - 220, 20, mapSize, mapSize), Color.Black * 0.5f);
            //calculo el tamanio del mapa para dibujar una version a escala en el minimap 
            float worldSize = 12000f;
            int mapX = (int)((player._position.X + 6000f) / worldSize * mapSize);
            int mapY = (int)((player._position.Z + 6000f) / worldSize * mapSize);
            //dibujo al jugador 
            spriteBatch.Draw(_pixel, new Rectangle(spriteBatch.GraphicsDevice.Viewport.Width - mapSize - 20 + mapX, 20 + mapY, 10, 10), Color.Green);
            //dibujo enemigos 
            foreach (var tank in tanks)
            {
                if (tank == player) continue;
                int enemyMapX = (int)((tank._position.X + 6000f) / worldSize * mapSize);
                int enemyMapY = (int)((tank._position.Z + 6000f) / worldSize * mapSize);
                spriteBatch.Draw(_pixel, new Rectangle(spriteBatch.GraphicsDevice.Viewport.Width - mapSize - 20 + enemyMapX, 20 + enemyMapY, 8, 8), Color.Red);
            }

            spriteBatch.End();
            // esto protege al terreno de no renderizase mal
            spriteBatch.GraphicsDevice.BlendState = BlendState.Opaque;
            spriteBatch.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            spriteBatch.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
        }

        public void DrawCircle(SpriteBatch spriteBatch, Texture2D pixel, Vector2 center, float radius, Color color)
        {
            int segments = 64;

            for (int i = 0; i < segments; i++)
            {
                float angle = MathHelper.TwoPi * i / segments;
                float x = center.X + (float)Math.Cos(angle) * radius;
                float y = center.Y + (float)Math.Sin(angle) * radius;
                spriteBatch.Draw(pixel, new Rectangle((int)x, (int)y, 2, 2), color);
            }
        }
    }
}
