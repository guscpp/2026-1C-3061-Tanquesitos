using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using TGC.MonoGame.TP.Collisions;
using TGC.MonoGame.TP.Gizmos;

namespace TGC.MonoGame.TP.Models.Decorations
{
    public class Cart : Decoration
    {
        public Cart(Vector3 position, string name) :  base(position, name)
        {
            Position = position;
            _path = name; 
        }
        
        private BoundingBox _cartBox;

        public override void InitializeCollisionChamber(Model model)
        {
            _cartBox = BoundingVolumesUtils.CreateAABBFromModel(model);
            //_cartBox = BoundingVolumesUtils.Scale(_cartBox, CollisionChamberScale);
            var extents = BoundingVolumesUtils.GetExtents(_cartBox);
            _cartBox = new BoundingBox(Position - extents, Position + extents);
            _cartBox = BoundingVolumesUtils.Scale(_cartBox, 0.4f);
        }

        public override void Update()
        {

            base.Update();
        }

        public override bool UpdateCollisions(BoundingSphere tankSphere)
        {
            _touchingDecoration = _cartBox.Intersects(tankSphere);
            return _touchingDecoration;
        }

        public override void DrawCollisionChamber(Gizmo gizmos)
        {   
            var size = BoundingVolumesUtils.GetExtents(_cartBox);
            gizmos.DrawCube(Position, size, _touchingDecoration ? CollisionedChamberColor : CollisionChamberColor);
        }

    }
}