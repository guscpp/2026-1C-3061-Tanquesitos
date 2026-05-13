using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using TGC.MonoGame.TP.Collisions;
using TGC.MonoGame.TP.Gizmos;

namespace TGC.MonoGame.TP.Models.Decorations
{
    public class Stairs : Decoration
    {
        public Stairs(Vector3 position, string name) :  base(position, name)
        {
            Position = position;
            _path = name; 
        }
        
        private BoundingBox _stairBox;

        public override void InitializeCollisionChamber(Model model)
        {
            _stairBox = BoundingVolumesUtils.CreateAABBFromModel(model);
            //_cartBox = BoundingVolumesUtils.Scale(_cartBox, CollisionChamberScale);
            var extents = BoundingVolumesUtils.GetExtents(_stairBox);
            _stairBox = new BoundingBox(Position - extents, Position + extents);
        }

        public override void Update()
        {

            base.Update();
        }

        public override bool UpdateCollisions(BoundingSphere tankSphere)
        {
            _touchingDecoration = _stairBox.Intersects(tankSphere);
            return _touchingDecoration;
        }

        public override void DrawCollisionChamber(Gizmo gizmos)
        {   
            var size = BoundingVolumesUtils.GetExtents(_stairBox);
            gizmos.DrawCube(Position, size, _touchingDecoration ? CollisionedChamberColor : CollisionChamberColor);
        }

    }
}