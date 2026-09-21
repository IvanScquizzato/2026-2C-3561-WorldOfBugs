using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Renderers;
using System.Collections.Concurrent;
namespace TGC.MonoGame.TP.Contact
{
    public class ContactPoint
    {
        public Vector3 _point;
        public Vector3 _normal;
        public ContactPoint(Vector3 point, Vector3 normal)
        {
            _point = point;
            _normal = normal;
        }
    }
}
