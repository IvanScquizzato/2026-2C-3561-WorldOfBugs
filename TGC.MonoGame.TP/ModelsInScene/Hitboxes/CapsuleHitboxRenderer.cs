using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.ModelsInScene
{
    public class CapsuleHitboxRenderer
    {
        public Effect _effect { get; set; }
        public GraphicsDevice _graphicsDevice { get; set; }

        public CapsuleHitboxRenderer(Effect effect, GraphicsDevice graphicsDevice)
        {
            _effect = effect;
            _graphicsDevice = graphicsDevice;
        }

        public void Draw(CapsuleHitbox hitbox, Matrix world, Matrix view, Matrix projection)
        {
            List<VertexPosition> verticesList = GenerateCapsuleVertices(hitbox._start, hitbox._end, hitbox._radius, world);
            VertexPosition[] vertices = verticesList.ToArray();


            _effect.Parameters["World"].SetValue(Matrix.Identity);
            _effect.Parameters["View"].SetValue(view);
            _effect.Parameters["Projection"].SetValue(projection);
            _effect.Parameters["DebugColor"].SetValue(Color.Red.ToVector4());

            foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();

                _graphicsDevice.DrawUserPrimitives(
                    PrimitiveType.LineList,
                    vertices,
                    0,
                    vertices.Length / 2
                );
            }
        }

        private List<VertexPosition> GenerateCapsuleVertices(Vector3 start, Vector3 end, float radius, Matrix world)
        {
            List<VertexPosition> vertices = new List<VertexPosition>();

            // Calcular ejes de la cápsula
            Vector3 up = end - start;
            float length = up.Length();
            if (length > 0)
                up.Normalize();
            else
                up = Vector3.Up;

            Vector3 right = Vector3.Cross(up, Vector3.Up);
            if (right.LengthSquared() < 0.001f)
                right = Vector3.Cross(up, Vector3.Right);
            right.Normalize();

            Vector3 forward = Vector3.Cross(right, up);

            int segments = 16;
            float angleStep = MathHelper.TwoPi / segments;

            Vector3[] topRing = new Vector3[segments];
            Vector3[] bottomRing = new Vector3[segments];

            for (int i = 0; i < segments; i++)
            {
                float angle = i * angleStep;
                Vector3 offset = (right * (float)Math.Cos(angle) + forward * (float)Math.Sin(angle)) * radius;

                bottomRing[i] = start + offset;
                topRing[i] = end + offset;
            }

            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;

                AddLine(vertices, bottomRing[i], bottomRing[next], world);

                AddLine(vertices, topRing[i], topRing[next], world);

                if (i % (segments / 4) == 0)
                {
                    AddLine(vertices, bottomRing[i], topRing[i], world);
                }
            }


            int arcSegments = segments / 2;
            float arcStep = MathHelper.Pi / arcSegments;

            for (int i = 0; i < arcSegments; i++)
            {
                float a1 = i * arcStep;
                float a2 = (i + 1) * arcStep;

                Vector3 t1_1 = end + (right * (float)Math.Cos(a1) + up * (float)Math.Sin(a1)) * radius;
                Vector3 t1_2 = end + (right * (float)Math.Cos(a2) + up * (float)Math.Sin(a2)) * radius;

                Vector3 t2_1 = end + (forward * (float)Math.Cos(a1) + up * (float)Math.Sin(a1)) * radius;
                Vector3 t2_2 = end + (forward * (float)Math.Cos(a2) + up * (float)Math.Sin(a2)) * radius;

                Vector3 b1_1 = start + (right * (float)Math.Cos(a1) - up * (float)Math.Sin(a1)) * radius;
                Vector3 b1_2 = start + (right * (float)Math.Cos(a2) - up * (float)Math.Sin(a2)) * radius;

                Vector3 b2_1 = start + (forward * (float)Math.Cos(a1) - up * (float)Math.Sin(a1)) * radius;
                Vector3 b2_2 = start + (forward * (float)Math.Cos(a2) - up * (float)Math.Sin(a2)) * radius;

                AddLine(vertices, t1_1, t1_2, world);
                AddLine(vertices, t2_1, t2_2, world);

                AddLine(vertices, b1_1, b1_2, world);
                AddLine(vertices, b2_1, b2_2, world);
            }

            return vertices;
        }

        private void AddLine(List<VertexPosition> vertices, Vector3 a, Vector3 b, Matrix world)
        {
            vertices.Add(new VertexPosition(Vector3.Transform(a, world)));
            vertices.Add(new VertexPosition(Vector3.Transform(b, world)));
        }
    }
}