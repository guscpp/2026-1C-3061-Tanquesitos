using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using TGC.MonoGame.TP.Collisions;
using TGC.MonoGame.TP.Gizmos;

namespace TGC.MonoGame.TP.Models.Decorations
{
    public class WoodenBox : Decoration
    {
        public WoodenBox(Vector3 position, string name) :  base(position, name)
        {
            Position = position;
            _path = name; 
        }
        
        private BoundingBox _woodenBox;

        public override void InitializeCollisionChamber(Model model)
        {
            _woodenBox = BoundingVolumesUtils.CreateAABBFromModel(model);
            //_cartBox = BoundingVolumesUtils.Scale(_cartBox, CollisionChamberScale);
            var extents = BoundingVolumesUtils.GetExtents(_woodenBox);
            _woodenBox = new BoundingBox(Position - extents, Position + extents);
        }

        public override void Update()
        {

            base.Update();
        }

        public override bool UpdateCollisions(BoundingSphere tankSphere)
        {
            _touchingDecoration = _woodenBox.Intersects(tankSphere);
            return _touchingDecoration;
        }

        public override void DrawCollisionChamber(Gizmo gizmos)
        {   
            var size = BoundingVolumesUtils.GetExtents(_woodenBox);
            gizmos.DrawCube(Position, size, _touchingDecoration ? CollisionedChamberColor : CollisionChamberColor);
        }

    }
}