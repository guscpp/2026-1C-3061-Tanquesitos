using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using TGC.MonoGame.TP.Collisions;
using TGC.MonoGame.TP.Gizmos;

namespace TGC.MonoGame.TP.Models.Decorations
{
    public class Barrel : Decoration
    {
        public Barrel(Vector3 position, string name) :  base(position, name)
        {
            Position = position;
            _path = name; 
        }
        
        private BoundingCylinder _barrelCylinder;

        public override void InitializeCollisionChamber(Model model)
        {
            _barrelCylinder = new BoundingCylinder(Position, 1f, 2f); // hardcodeado, perdon :c
        }

        public override void Update()
        {

            base.Update();
        }

        public override bool UpdateCollisions(BoundingSphere tankSphere)
        {
            _touchingDecoration = _barrelCylinder.Intersects(tankSphere);
            return _touchingDecoration;
        }

        public override void DrawCollisionChamber(Gizmo gizmos)
        {   
            gizmos.DrawCylinder(_barrelCylinder.Transform, _touchingDecoration ? CollisionedChamberColor: CollisionChamberColor);
        }

    }
}