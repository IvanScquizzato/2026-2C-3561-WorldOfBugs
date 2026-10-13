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

namespace TGC.MonoGame.TP.ColliderRenderers
{
    public class SphereColliderRenderer : ShapeColliderRenderer
    {

        public SphereColliderRenderer(GraphicsDevice graphicsDevice, Effect effect)
        {
            _shapeRenderer = new SphereRenderer(graphicsDevice, effect);
        }

        protected override Matrix GetWorldMatrix(Collider collider)
        {
            // Obtiene la matriz de mundo (Traslación y Rotación) sin escalar
            Matrix worldMatrix = collider.GetWorldMatrixNotScaled();

            // Le pedimos a Bepu la forma física de tipo Sphere
            ref Sphere sphereShape = ref collider._simulation.Shapes.GetShape<Sphere>(collider.GetShape().Index);

            // Como nuestro SphereRenderer tiene un diámetro base de 1.0 (radio 0.5), 
            // la escala física debe ser el diámetro de la esfera de Bepu (Radio * 2).
            float diameter = sphereShape.Radius * 2f;
            var physicsScale = new Vector3(diameter, diameter, diameter);

            // Aplicamos primero la Escala, y luego la Rotación/Traslación (worldMatrix)
            return Matrix.CreateScale(physicsScale) * worldMatrix;
        }

        public override int GetShapeId()
        {
            return Sphere.Id;
        }
    }
}