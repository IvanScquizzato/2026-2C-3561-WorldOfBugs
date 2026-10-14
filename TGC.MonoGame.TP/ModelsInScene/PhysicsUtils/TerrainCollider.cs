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
using TGC.MonoGame.TP.ColliderRenderers;
using TGC.MonoGame.TP.ShapeRenderers;

namespace TGC.MonoGame.TP.Colliders
{
    public class TerrainCollider : Collider
    {
        public TerrainCollider(List<Triangle> triangles, NumericVector3 position, Simulation simulation, BufferPool bufferPool, Effect effect, GraphicsDevice graphicsDevice) :
        base(simulation, bufferPool, effect, graphicsDevice)
        {
            LoadTerrainCollider(triangles, position);
        }
        private void LoadTerrainCollider(List<Triangle> triangles, NumericVector3 position)
        {
            _bufferPool.Take(triangles.Count, out Buffer<Triangle> trianglesBuffer);
            for (int i = 0; i < triangles.Count; i++)
            {
                trianglesBuffer[i] = triangles[i];
            }
            Mesh terrainMesh = new Mesh(trianglesBuffer, NumericVector3.One, _bufferPool);
            TypedIndex shapeIndex = _simulation.Shapes.Add(terrainMesh);
            StaticDescription staticDescription = new StaticDescription(position, shapeIndex);
            StaticHandle staticHandle = _simulation.Statics.Add(staticDescription);
            _staticReference = _simulation.Statics.GetStaticReference(staticHandle);
        }
    }
}