using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.ShapeRenderers
{
    public class SphereRenderer : ShapeRenderer
    {
        private int _primitiveCount;

        public SphereRenderer(GraphicsDevice graphicsDevice, Effect effect) : base(graphicsDevice, effect)
        { }

        protected override void Load()
        {
            // Resolución de la esfera. Puedes bajar estos números para mejorar 
            // el rendimiento o subirlos para que se vea más redonda.
            int horizontalSegments = 16;
            int verticalSegments = 16;

            float radius = 0.5f; // Diámetro total de 1.0, igual que tu caja
            Color sphereColor = Color.LimeGreen;

            List<VertexPositionColor> vertexList = new List<VertexPositionColor>();
            List<short> indexList = new List<short>();

            // 1. Generar los vértices (matemática de coordenadas esféricas)
            for (int i = 0; i <= verticalSegments; i++)
            {
                float phi = (float)i / verticalSegments * MathHelper.Pi; // De 0 a PI (Polo Norte a Polo Sur)

                for (int j = 0; j <= horizontalSegments; j++)
                {
                    float theta = (float)j / horizontalSegments * MathHelper.TwoPi; // De 0 a 2PI (Ecuador)

                    float x = (float)(radius * Math.Sin(phi) * Math.Cos(theta));
                    float y = (float)(radius * Math.Cos(phi));
                    float z = (float)(radius * Math.Sin(phi) * Math.Sin(theta));

                    vertexList.Add(new VertexPositionColor(new Vector3(x, y, z), sphereColor));
                }
            }

            // 2. Generar los índices para dibujar las líneas
            for (int i = 0; i < verticalSegments; i++)
            {
                for (int j = 0; j < horizontalSegments; j++)
                {
                    short current = (short)(i * (horizontalSegments + 1) + j);
                    short next = (short)(current + 1);
                    short bottom = (short)((i + 1) * (horizontalSegments + 1) + j);

                    // Línea horizontal (Paralelos)
                    indexList.Add(current);
                    indexList.Add(next);

                    // Línea vertical (Meridianos)
                    indexList.Add(current);
                    indexList.Add(bottom);
                }
            }

            // Calculamos la cantidad de líneas (cada línea usa 2 índices)
            _primitiveCount = indexList.Count / 2;

            // 3. Crear los buffers e inyectar los datos
            vertexBuffer = new VertexBuffer(_graphicsDevice, typeof(VertexPositionColor), vertexList.Count, BufferUsage.WriteOnly);
            vertexBuffer.SetData(vertexList.ToArray());

            indexBuffer = new IndexBuffer(_graphicsDevice, typeof(short), indexList.Count, BufferUsage.WriteOnly);
            indexBuffer.SetData(indexList.ToArray());
        }

        public override void Draw(Matrix world, Matrix view, Matrix projection)
        {
            // Usamos la cantidad de primitivas calculada proceduralmente
            DrawWithLines(world, view, projection, _primitiveCount);
        }
    }
}