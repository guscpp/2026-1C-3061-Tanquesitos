using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using TGC.MonoGame.TP.Collisions;
using TGC.MonoGame.TP.Gizmos;

namespace TGC.MonoGame.TP.Models.Decorations
{
    public class Rock : Decoration
    {
        public Rock(Vector3 position, string name) :  base(position, name)
        {
            Position = position;
            _path = name; 
        }
        private BoundingSphere _rockSphere;

        public override void InitializeCollisionChamber(Model model)
        {
            _rockSphere = BoundingVolumesUtils.CreateSphereFrom(model);
            _rockSphere = BoundingVolumesUtils.Scale(_rockSphere, 0.01f);
            _rockSphere = new BoundingSphere(Position, _rockSphere.Radius);
        }

        public override bool UpdateCollisions(BoundingSphere tankSphere)
        {
            _touchingDecoration = tankSphere.Intersects(_rockSphere);
            return _touchingDecoration;
        }

        public override void DrawCollisionChamber(Gizmo gizmos)
        {   
            gizmos.DrawSphere(Position, _rockSphere.Radius * Vector3.One, _touchingDecoration ? CollisionedChamberColor: CollisionChamberColor);
        }

    }
}