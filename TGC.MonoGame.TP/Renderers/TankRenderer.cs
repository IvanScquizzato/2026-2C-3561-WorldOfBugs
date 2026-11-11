using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TGC.MonoGame.TP.ModelsInScene;
using TGC.MonoGame.TP.Tanks;
using System.Diagnostics;
using TGC.MonoGame.TP.Materials;
namespace TGC.MonoGame.TP.Renderers
{
    public class TankRenderer : IModelInSceneRenderer
    {
        public GraphicsDevice _graphicsDevice { get; set; }
        public Effect _effect { get; set; }
        public Effect _hitBoxEffect { get; set; }
        public Effect _arrowEffect { get; set; }
        public bool drawNormals = false;
        private float velocidad_ruedas = -4f;
        public TankRenderer(GraphicsDevice graphicsDevice, Effect effect, Effect hitboxEffect, Effect arrowEffect)
        {
            _graphicsDevice = graphicsDevice;
            _effect = effect;
            _hitBoxEffect = hitboxEffect;
            _arrowEffect = arrowEffect;
        }

        public void Draw(ModelInScene modelInScene, Matrix view, Matrix projection)
        {
            if (modelInScene is Tank tank)
            {
                _effect.Parameters["View"].SetValue(view);
                _effect.Parameters["Projection"].SetValue(projection);
                _effect.Parameters["Roughness"].SetValue(0.7f);
                _effect.Parameters["K"].SetValue((0.7f + 1f) * (0.7f + 1f) / 8f);
                _effect.Parameters["F0"].SetValue(new Vector3(0.04f, 0.04f, 0.04f));
                Vector3 realCameraPosition = Matrix.Invert(view).Translation;
                _effect.Parameters["CameraPosition"].SetValue(realCameraPosition);
                _effect.CurrentTechnique = _effect.Techniques["WithoutNormalMap"];

                var modelMeshesBaseTransforms = new Matrix[tank._model.Bones.Count];
                tank._model.CopyAbsoluteBoneTransformsTo(modelMeshesBaseTransforms);
                var index = 0;
                foreach (var mesh in tank._model.Meshes)
                {

                    if (mesh.Name == "Treadmill1")
                    {
                        _effect.Parameters["TrackOffset"]
                            .SetValue(tank.LeftTrackOffset);
                    }
                    else if (mesh.Name == "Treadmill2")
                    {
                        _effect.Parameters["TrackOffset"]
                            .SetValue(tank.RightTrackOffset);
                    }
                    else
                    {
                        _effect.Parameters["TrackOffset"]
                            .SetValue(0f);
                    }


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
                    else if (mesh.Name.StartsWith("Wheel"))
                    {
                        int wheelNumber =
                            int.Parse(mesh.Name.Replace("Wheel", ""));

                        float rotation =
                            wheelNumber <= 10
                                ? tank.LeftTrackOffset * velocidad_ruedas
                                : tank.RightTrackOffset * velocidad_ruedas;

                        world =
                            Matrix.CreateRotationX(rotation) *
                            relativeTransform *
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
                            ArrowRenderer.DrawLines(debugArrows, view, projection, _arrowEffect, _graphicsDevice);
                        }
                        _effect.Parameters["World"].SetValue(world);

                        if (tank.GetTextures()[index] != null)
                        {
                            _effect.Parameters["ModelTexture"].SetValue(tank.GetTextures()[index]);
                            if (modelInScene._normalMapsOfTexture.TryGetValue(modelInScene.GetTextures()[index], out Texture normalMap))
                            {
                                _effect.Parameters["NormalMap"].SetValue(normalMap);
                                _effect.CurrentTechnique = _effect.Techniques["WithNormalMap"];
                            }
                            else
                            {
                                _effect.CurrentTechnique = _effect.Techniques["WithoutNormalMap"];
                            }
                            if (modelInScene._materialOfNameTexture.TryGetValue(modelInScene.GetTextures()[index].Name, out IMaterial material))
                            {
                                _effect.Parameters["Roughness"].SetValue(material.Roughness);
                                _effect.Parameters["K"].SetValue(material.K);
                                _effect.Parameters["F0"].SetValue(material.F0);
                            }
                        }
                        _effect.CurrentTechnique.Passes[0].Apply();
                        _graphicsDevice.SetVertexBuffer(tank._dynamicVertexBuffers[meshPart]);
                        _graphicsDevice.Indices = meshPart.IndexBuffer;
                        _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, meshPart.StartIndex, meshPart.PrimitiveCount);
                        index++;
                    }
                }
            }
        }
    }
}
