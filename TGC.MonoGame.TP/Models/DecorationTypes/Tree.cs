using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using TGC.MonoGame.TP.Collisions;
using TGC.MonoGame.TP.Gizmos;

namespace TGC.MonoGame.TP.Models.Decorations
{
    public class Tree : Decoration
    {
        
        private BoundingCylinder _treeCylinder;

        public Tree(Vector3 position, string name) :  base(position, name)
        {
            Position = position;
            _path = name;
        }

        public override void InitializeCollisionChamber(Model model)
        {
            _treeCylinder = new BoundingCylinder(Position, 2f, 4f); // hardcodeado, perdon :c
        }

        public override void Update()
        {

            base.Update();
        }

        public override bool UpdateCollisions(BoundingSphere tankSphere)
        {
            _touchingDecoration = _treeCylinder.Intersects(tankSphere);
            return _touchingDecoration;
        }

        public override void DrawCollisionChamber(Gizmo gizmos)
        {   
            gizmos.DrawCylinder(_treeCylinder.Transform, _touchingDecoration ? CollisionedChamberColor: CollisionChamberColor);
        }

    }
}