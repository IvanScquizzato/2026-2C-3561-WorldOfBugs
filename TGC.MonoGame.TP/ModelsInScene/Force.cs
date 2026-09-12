using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Renderers;
using System.Collections.Concurrent;
namespace TGC.MonoGame.TP.Forces
{
    public class Force
    {
        public Vector3 _vector { get; set; }
        public Vector3 _applicationPoint { get; set; }
        public Force(Vector3 vector, Vector3 applicationPoint)
        {
            _vector = vector;
            _applicationPoint = applicationPoint;
        }
    }
}