using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.ModelsInScene
{
    public class BoxHitboxRenderer
    {

        public Effect _effect { get; set; }
        public GraphicsDevice _graphicsDevice { get; set; }
        public BoxHitboxRenderer(Effect effect, GraphicsDevice graphicsDevice)
        {
            _effect = effect;
            _graphicsDevice = graphicsDevice;
        }
        public void Draw(BoxHitbox hitbox, Matrix world, Matrix view, Matrix projection)
        {
            Vector3[] corners =
                            hitbox.GetWorldCorners(world);

            VertexPosition[] vertices =
            {
                new VertexPosition(corners[0]),
                new VertexPosition(corners[1]),

                new VertexPosition(corners[1]),
                new VertexPosition(corners[2]),

                new VertexPosition(corners[2]),
                new VertexPosition(corners[3]),

                new VertexPosition(corners[3]),
                new VertexPosition(corners[0]),

                new VertexPosition(corners[4]),
                new VertexPosition(corners[5]),

                new VertexPosition(corners[5]),
                new VertexPosition(corners[6]),

                new VertexPosition(corners[6]),
                new VertexPosition(corners[7]),

                new VertexPosition(corners[7]),
                new VertexPosition(corners[4]),

                new VertexPosition(corners[0]),
                new VertexPosition(corners[4]),

                new VertexPosition(corners[1]),
                new VertexPosition(corners[5]),

                new VertexPosition(corners[2]),
                new VertexPosition(corners[6]),

                new VertexPosition(corners[3]),
                new VertexPosition(corners[7])
            };

            _effect.Parameters["World"]
                .SetValue(Matrix.Identity);

            _effect.Parameters["View"]
                .SetValue(view);

            _effect.Parameters["Projection"]
                .SetValue(projection);

            _effect.Parameters["DebugColor"]
                .SetValue(Color.Red.ToVector4());

            foreach (
                EffectPass pass
                in _effect.CurrentTechnique.Passes
            )
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
    }
}