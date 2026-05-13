using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using BepuPhysics.Collidables;
using System.Numerics;
using TGC.MonoGame.TP.Collisions;
using TGC.MonoGame.TP.Gizmos;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using BepuPhysics;

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

        public StaticHandle HouseHandler;


        /// <summary>
        /// Crea el handler de colisiones de Bepu
        /// </summary>
        /// <param name="position"></param>
        /// <param name="simulation"></param>
        public StaticHandle CreateHouse(Vector3 position, Simulation simulation)
        {
            var extents = BoundingVolumesUtils.GetExtents(_houseBox);
            var box = new Box(extents.X * 2f, extents.Y * 2f, extents.Z * 2f);
            var shape = simulation.Shapes.Add(box);

            HouseHandler = simulation.Statics.Add(new StaticDescription(VectorExtensions.ToNumerics(Position),shape));
            return HouseHandler;
        }

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