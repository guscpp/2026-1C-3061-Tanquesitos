using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using TGC.MonoGame.TP.Collisions;
using TGC.MonoGame.TP.Gizmos;

namespace TGC.MonoGame.TP.Models.Decorations
{
    public class Cactus : Decoration
    {
        public Cactus(Vector3 position, string name) :  base(position, name)
        {
            Position = position;
            _path = name; 
        }
        
        private BoundingCylinder _cactusCylinder;

        public override void InitializeCollisionChamber(Model model)
        {
            _cactusCylinder = new BoundingCylinder(Position, 1f, 2f); // hardcodeado, perdon :c
        }

        public override void Update()
        {

            base.Update();
        }

        public override bool UpdateCollisions(BoundingSphere tankSphere)
        {
            _touchingDecoration = _cactusCylinder.Intersects(tankSphere);
            return _touchingDecoration;
        }

        public override void DrawCollisionChamber(Gizmo gizmos)
        {   
            gizmos.DrawCylinder(_cactusCylinder.Transform, _touchingDecoration ? CollisionedChamberColor: CollisionChamberColor);
        }

    }
}