using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Renderers;
using System.Collections.Concurrent;
using BepuPhysics;
using TGC.MonoGame.TP.Tanks;
using TGC.MonoGame.TP.Physics.Bepu;
using NumericVector3 = System.Numerics.Vector3;
using TGC.MonoGame.Utils;
using System.Diagnostics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using TGC.MonoGame.TP.Colliders;
using TGC.MonoGame.TP.ShapeRenderers;
namespace TGC.MonoGame.TP.ShapeRenderers
{
    public class BoxRenderer : ShapeRenderer
    {
        public BoxRenderer(GraphicsDevice graphicsDevice, Effect effect) : base(graphicsDevice, effect)
        { }

        protected override void Load()
        {
            // 2. Definir los 8 vértices del cubo
            VertexPositionColor[] vertices = new VertexPositionColor[8];
            Vector3[] corners = new Vector3[]
            {
            new Vector3(-0.5f,  0.5f, -0.5f), // 0: Arriba, Izquierda, Atrás
            new Vector3( 0.5f,  0.5f, -0.5f), // 1: Arriba, Derecha, Atrás
            new Vector3( 0.5f,  0.5f,  0.5f), // 2: Arriba, Derecha, Frente
            new Vector3(-0.5f,  0.5f,  0.5f), // 3: Arriba, Izquierda, Frente
            new Vector3(-0.5f, -0.5f, -0.5f), // 4: Abajo, Izquierda, Atrás
            new Vector3( 0.5f, -0.5f, -0.5f), // 5: Abajo, Derecha, Atrás
            new Vector3( 0.5f, -0.5f,  0.5f), // 6: Abajo, Derecha, Frente
            new Vector3(-0.5f, -0.5f,  0.5f)  // 7: Abajo, Izquierda, Frente
            };
            Color boxColor = Color.LimeGreen;
            for (int i = 0; i < 8; i++)
                vertices[i] = new VertexPositionColor(corners[i], boxColor);

            vertexBuffer = new VertexBuffer(_graphicsDevice, typeof(VertexPositionColor), 8, BufferUsage.WriteOnly);
            vertexBuffer.SetData(vertices);

            // 3. Definir los índices para las 12 líneas (24 índices en total)
            short[] indices = new short[]
            {
            // Cuadrado superior
            0, 1, 1, 2, 2, 3, 3, 0,
            // Cuadrado inferior
            4, 5, 5, 6, 6, 7, 7, 4,
            // Pilares verticales conectando arriba y abajo
            0, 4, 1, 5, 2, 6, 3, 7
            };

            indexBuffer = new IndexBuffer(_graphicsDevice, typeof(short), 24, BufferUsage.WriteOnly);
            indexBuffer.SetData(indices);
        }
        public override void Draw(Matrix world, Matrix view, Matrix projection)
        {
            DrawWithLines(world, view, projection, 12);
        }

    }
}