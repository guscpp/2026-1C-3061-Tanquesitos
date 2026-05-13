using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Collisions;
using TGC.MonoGame.TP.Gizmos;

namespace TGC.MonoGame.TP.Models.Decorations
{
/// <summary>
/// Casas dentro de escenario 
/// </summary>
    public class House : Decoration
    {
        public House(Vector3 position, string name) : base(position, name)
        {
            Position = position;
            _path = name;
        }
        private bool _touchingHouse = false;

        public BoundingBox _houseBox;

        public override void InitializeCollisionChamber(Model model)
        {
            _houseBox = BoundingVolumesUtils.CreateAABBFromModel(model);
            _houseBox = BoundingVolumesUtils.Scale(_houseBox, CollisionChamberScale * 3);
            var extents = BoundingVolumesUtils.GetExtents(_houseBox);
            _houseBox = new BoundingBox(Position - extents, Position + extents);
        }

        public override bool UpdateCollisions(BoundingSphere tankSphere)
        {
            _touchingHouse = tankSphere.Intersects(_houseBox);
            return _touchingHouse;
        }

        public override void DrawCollisionChamber(Gizmo gizmos)
        {
            var size = BoundingVolumesUtils.GetExtents(_houseBox);
                gizmos.DrawCube(Position, size, _touchingHouse ? CollisionedChamberColor : CollisionChamberColor);
        }
    }
} 