using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using BepuPhysics;
using BepuPhysics.Collidables;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using BepuVector3 = System.Numerics.Vector3;
using TGC.MonoGame.TP.Collisions;
using TGC.MonoGame.TP.Gizmos;

namespace TGC.MonoGame.TP.Models.Decorations
{
    public class Rock : Static
    {
        private float _radius;
        private float _height; // <-- Nueva variablee para la altura del cilindro

        public Rock(Vector3 position, string path) : base(position, path) { }

        public override void LoadContent(ContentManager content, Simulation simulation, Effect effect)
            {
                base.LoadContent(content, simulation, effect);
                
                // Calculamos el radio base y lo multiplicamos por 0.45f para achicar el diámetro general
                _radius = ((_dimensions.X + _dimensions.Z) / 4f) * 0.45f; 
                
                // Hacemos la altura un poco más baja para que el tanque no choque con el aire arriba de la piedra
                _height = _dimensions.Y * 0.3f; 

                _visualScale = 1f;

                // La física de Bepu ahora usa este nuevo tamaño ajustado
                var shape = new Cylinder(_radius, _height);
                var shapeIndex = simulation.Shapes.Add(shape);

                // Dejamos la posición y la matriz de mundo igual
                var initialPos = new System.Numerics.Vector3(_position.X, _position.Y, _position.Z);
                _staticHandle = simulation.Statics.Add(new StaticDescription(initialPos, shapeIndex));

                Matrix rotation = Matrix.CreateRotationX(MathHelper.ToRadians(-90));
                modificarMatrixWorld(rotation, 0f); 
            }

        public void HandleCollision() { }
        
        // MODIFICAMOS EL GIZMO: Ahora dibujamos un cilindro para que coincida con la física
    
        // DIBUJO LAS COLISIONES (Versión Esfera Ajustada)
        public override void DrawCollisionChamber(Gizmo gizmos, Simulation simulation)
        { 
            Vector3 posicionVisual = new Vector3(_position.X, _position.Y, _position.Z);
            
            // Le pasamos el diámetro en cada eje: X y Z usan el radio, Y usa la altura chatita
            Vector3 tamañoVisual = new Vector3(_radius * 2f, _height, _radius * 2f);

            // Volvemos a DrawSphere que sabemos que existe y no tira error
            gizmos.DrawSphere(posicionVisual, tamañoVisual, Color.Violet);
        }
    }
}