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
namespace TGC.MonoGame.TP.ShapeRenderers
{

    public class CylinderRenderer : ShapeRenderer
    {


        public CylinderRenderer(GraphicsDevice graphicsDevice, Effect effect) : base(graphicsDevice, effect, Cylinder.Id)
        {

        }
        protected override void Load()
        {
            int segments = 16;
            VertexPositionColor[] vertices = new VertexPositionColor[segments * 2];
            short[] indices = new short[segments * 6];

            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * MathHelper.TwoPi;
                float x = (float)Math.Cos(angle) * 0.5f; // Radio de 0.5 (Diámetro de 1)
                float z = (float)Math.Sin(angle) * 0.5f;

                // Tapa superior (Y = 0.5)
                vertices[i] = new VertexPositionColor(new Vector3(x, 0.5f, z), Color.LimeGreen);
                // Tapa inferior (Y = -0.5)
                vertices[i + segments] = new VertexPositionColor(new Vector3(x, -0.5f, z), Color.LimeGreen);

                // Índices para dibujar las líneas
                int next = (i + 1) % segments;

                // Líneas del círculo superior
                indices[i * 6 + 0] = (short)i;
                indices[i * 6 + 1] = (short)next;

                // Líneas del círculo inferior
                indices[i * 6 + 2] = (short)(i + segments);
                indices[i * 6 + 3] = (short)(next + segments);

                // Líneas verticales conectando arriba y abajo
                indices[i * 6 + 4] = (short)i;
                indices[i * 6 + 5] = (short)(i + segments);
            }

            vertexBuffer = new VertexBuffer(_graphicsDevice, typeof(VertexPositionColor), vertices.Length, BufferUsage.WriteOnly);
            vertexBuffer.SetData(vertices);

            indexBuffer = new IndexBuffer(_graphicsDevice, typeof(short), indices.Length, BufferUsage.WriteOnly);
            indexBuffer.SetData(indices);
        }
        protected override void DrawWithWorld(Matrix world, Matrix view, Matrix projection)
        {
            base.DrawWithWorldAndLines(world, view, projection, 48);
        }
        protected override Matrix GetWorldOfShape(Matrix worldNotScaled, TypedIndex shapeIndex, Simulation simulation)
        {
            ref Cylinder cylinderShape = ref simulation.Shapes.GetShape<Cylinder>(shapeIndex.Index);
            var physicsScale = new Vector3(cylinderShape.Radius * 2f, cylinderShape.Length, cylinderShape.Radius * 2f);
            return Matrix.CreateScale(physicsScale) * worldNotScaled;
        }
    }
}