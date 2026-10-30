using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TGC.MonoGame.TP.ModelsInScene;
using TGC.MonoGame.TP.Tanks;
using System.Diagnostics;
using TGC.MonoGame.TP.ModelsInScene;

namespace TGC.MonoGame.TP.Renderers
{
    public class ArrowRenderer
    {
        public GraphicsDevice _graphicsDevice { get; set; }
        public Effect _effect { get; set; }
        public Effect _hitBoxEffect { get; set; }


        public static void DrawLines(List<DebugArrow> arrows, Matrix view, Matrix projection, Effect effect, GraphicsDevice graphicsDevice)
        {
            if (arrows == null || arrows.Count == 0) return;

            // Configuramos las matrices de la cámara
            if (effect.Parameters["World"] != null)
            {
                effect.Parameters["World"].SetValue(Matrix.Identity);
            }
            effect.Parameters["View"].SetValue(view);
            effect.Parameters["Projection"].SetValue(projection);
            // Creamos los vértices para las líneas
            var vertices = new VertexPositionColor[arrows.Count * 2];
            int index = 0;

            foreach (var arrow in arrows)
            {
                vertices[index++] = new VertexPositionColor(arrow.Start, arrow.Color);
                vertices[index++] = new VertexPositionColor(arrow.End, arrow.Color);
            }
            effect.CurrentTechnique = effect.Techniques["SolidColorDrawing"];
            // Renderizamos
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, vertices, 0, arrows.Count);
            }
        }
    }
}