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
        private readonly SpriteFont _font;

        public HUD(Texture2D pixel, Tank tank, SpriteFont font)
        {
            _pixel = pixel;
            _tank = tank;
            _font = font;
        }

        public void Draw(SpriteBatch spriteBatch, Tank player, List<Tank> tanks)
        {
            float vidaActual = player.health;
            float vidaMaxima = player.maxHealth;

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
            spriteBatch.Draw(_pixel, new Rectangle(20, 20, (int)((vidaActual / vidaMaxima) * 200f), 20), Color.Red);
            spriteBatch.DrawString(
    _font,
    $"{vidaActual}/{vidaMaxima}",
    new Vector2(20, 45),
    Color.White);
            //minimap 
            spriteBatch.Draw(_pixel, new Rectangle(spriteBatch.GraphicsDevice.Viewport.Width - 220, 20, mapSize, mapSize), Color.Black * 0.5f);
            //calculo el tamanio del mapa para dibujar una version a escala en el minimap 
            float worldSize = 12000f;
            int mapX = (int)((player._position.X + 6000f) / worldSize * mapSize);
            int mapY = (int)((player._position.Z + 6000f) / worldSize * mapSize);
            //dibujo al jugador 
            //obtengo la direccion hacia donde mira
            Vector2 dir = new Vector2(player._forward.X,player._forward.Z);

            dir.Normalize();
            //calculo su posicion
            int playerMapX =spriteBatch.GraphicsDevice.Viewport.Width - mapSize - 20 + mapX;

            int playerMapY = 20 + mapY;


            Vector2 center = new Vector2(playerMapX + 5,playerMapY + 5);

          

            Vector2 right = new Vector2(-dir.Y,dir.X);

            Vector2 front =center + dir * 12f;

            Vector2 leftCorner =center - dir * 3f - right * 5f;

            Vector2 rightCorner = center - dir * 4f + right * 5f;

            
            //dibujo los vertices del triangulo
            DrawLine(spriteBatch, leftCorner, front, Color.Lime);
            DrawLine(spriteBatch, rightCorner, front, Color.Lime);
            DrawLine(spriteBatch, leftCorner, rightCorner, Color.Lime);

            //lo relleno
            for (float t = 0; t <= 1f; t += 0.05f)
            {
                Vector2 p1 = Vector2.Lerp(front, leftCorner, t);
                Vector2 p2 = Vector2.Lerp(front, rightCorner, t);

                DrawLine(spriteBatch,p1,p2,Color.Lime);
            }

            //dibujo a los enemigos
            foreach (var tank in tanks)
            {
                if (tank == player) continue;
                int enemyMapX = (int)((tank._position.X + 6000f) / worldSize * mapSize);
                int enemyMapY = (int)((tank._position.Z + 6000f) / worldSize * mapSize);
                spriteBatch.Draw(_pixel, new Rectangle(spriteBatch.GraphicsDevice.Viewport.Width - mapSize - 20 + enemyMapX, 20 + enemyMapY, 8, 8), Color.Red);
            }

            //dibujo la barra de recarga
            spriteBatch.Draw(_pixel,new Rectangle(15, 105, 210, 30),Color.Black);

            spriteBatch.Draw(_pixel,new Rectangle(20, 110, 200, 20),Color.DarkSlateGray);

            //hago que el ancho varie segun la barra de cooldown
            int width = (int)(200 * player.CooldownProgress);

            Color reloadColor = player.CooldownProgress >= 1f ? Color.DarkGreen : Color.SeaGreen;

            
            spriteBatch.Draw(_pixel,new Rectangle(20, 110, width, 20),reloadColor);

            spriteBatch.DrawString(_font,"RECARGA",new Vector2(20, 70),Color.White);

            if(player.CooldownProgress >= 1f)
            {
                spriteBatch.DrawString(_font, "Presione 'X' para disparar", new Vector2(20, 150), Color.GreenYellow);
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

        private void DrawLine(SpriteBatch spriteBatch,Vector2 start,Vector2 end,Color color)
        {
            Vector2 edge = end - start;

            float angle =
                (float)Math.Atan2(edge.Y, edge.X);

            spriteBatch.Draw(_pixel,start,null,color,angle,Vector2.Zero,new Vector2(edge.Length(), 2),SpriteEffects.None,0);
        }
    }
}
