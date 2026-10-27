using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TGC.MonoGame.TP.ModelsInScene;
using TGC.MonoGame.TP.Tanks;
using System.Diagnostics;
namespace TGC.MonoGame.TP.Renderers
{
    public class TankRenderer : IModelInSceneRenderer
    {
        public GraphicsDevice _graphicsDevice { get; set; }
        public Effect _effect { get; set; }
        public Effect _hitBoxEffect { get; set; }
        public bool drawNormals = false;
        public TankRenderer(GraphicsDevice graphicsDevice, Effect effect, Effect hitboxEffect)
        {
            _graphicsDevice = graphicsDevice;
            _effect = effect;
            _hitBoxEffect = hitboxEffect;
        }

        public void Draw(ModelInScene modelInScene, Matrix view, Matrix projection)
        {
            if (modelInScene is Tank tank)
            {
                _effect.Parameters["View"].SetValue(view);
                _effect.Parameters["Projection"].SetValue(projection);

                var modelMeshesBaseTransforms = new Matrix[tank._model.Bones.Count];
                tank._model.CopyAbsoluteBoneTransformsTo(modelMeshesBaseTransforms);
                var index = 0;
                foreach (var mesh in tank._model.Meshes)
                {
                    var relativeTransform = modelMeshesBaseTransforms[mesh.ParentBone.Index];

                    Matrix world = tank._world;

                    if (mesh.Name == "Turret")
                    {

                        world =
                            relativeTransform *
                            Matrix.CreateRotationY(tank.TurretYaw) *
                            tank._world;
                    }
                    else if (mesh.Name == "Cannon")
                    {

                        world =
                            relativeTransform *
                            Matrix.CreateRotationX(tank.CannonPitch) *
                            Matrix.CreateRotationY(tank.TurretYaw) *
                            tank._world;

                    }
                    else
                    {
                        world =
                            relativeTransform *
                            tank._world;
                    }



                    foreach (var meshPart in mesh.MeshParts)
                    {
                        if (drawNormals)
                        {
                            List<DebugArrow> debugArrows = new List<DebugArrow>();
                            foreach (VertexPositionNormalTexture vertex in tank.verticesPorMeshPart[meshPart].Item1)
                            {
                                debugArrows.Add(new DebugArrow()
                                {
                                    Start = Vector3.Transform(vertex.Position, world),
                                    End = Vector3.Transform(vertex.Position + vertex.Normal * 0.1f, world),
                                    Color = Color.Aqua
                                });
                            }
                            ArrowRenderer.DrawLines(debugArrows, view, projection, _effect, _graphicsDevice);
                        }
                        _effect.Parameters["World"].SetValue(world);
                        _effect.CurrentTechnique = _effect.Techniques["BasicColorDrawing"];

                        if (tank.GetTextures()[index] != null)
                        {
                            _effect.Parameters["ModelTexture"].SetValue(tank.GetTextures()[index]);
                        }
                        _effect.CurrentTechnique.Passes[0].Apply();
                        _graphicsDevice.SetVertexBuffer(tank._dynamicVertexBuffers[meshPart]);
                        _graphicsDevice.Indices = meshPart.IndexBuffer;
                        _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, meshPart.VertexOffset, meshPart.StartIndex, meshPart.PrimitiveCount);
                        index++;
                    }
                }
            }
        }
    }
}