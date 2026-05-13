using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using TGC.MonoGame.TP.Collisions;
using TGC.MonoGame.TP.Gizmos;

namespace TGC.MonoGame.TP.Models.Decorations
{
    public class Plant : Decoration
    {
        public Plant(Vector3 position, string name) :  base(position, name)
        {
            Position = position;
            _path = name; 
        }
        private BoundingSphere _plantSphere;

        public override void InitializeCollisionChamber(Model model)
        {
            _plantSphere = BoundingVolumesUtils.CreateSphereFrom(model);
            _plantSphere = BoundingVolumesUtils.Scale(_plantSphere, CollisionChamberScale);
            _plantSphere = new BoundingSphere(Position, _plantSphere.Radius);
        }

        public override bool UpdateCollisions(BoundingSphere tankSphere)
        {
            _touchingDecoration = tankSphere.Intersects(_plantSphere);
            return _touchingDecoration;
        }

        public override void DrawCollisionChamber(Gizmo gizmos)
        {   
            gizmos.DrawSphere(Position, _plantSphere.Radius * Vector3.One, _touchingDecoration ? CollisionedChamberColor: CollisionChamberColor);
        }

    }
}