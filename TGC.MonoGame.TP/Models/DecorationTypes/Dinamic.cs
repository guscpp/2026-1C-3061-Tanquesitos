using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using BepuPhysics;
using BepuPhysics.Collidables;
// Alias para evitar la ambigüedad molesta entre los dos motores que no se como solucionar ;_;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using BepuVector3 = System.Numerics.Vector3;
//No entiendo por que debo agregar otra vez estas librerias si ya estan en decorationnnn
using TGC.MonoGame.TP.Collisions;
using TGC.MonoGame.TP.Gizmos;

namespace TGC.MonoGame.TP.Models.Decorations
{ //Lo llame asi para diferenciarlo del Dynamic (Me confundi al escribirlo pero sirve)
    public class Dinamic : Decoration
    {

        public BodyHandle bodyHandle { get; protected set; }
        public bool IsDead { get; protected set; } = false;

        public Dinamic(Vector3 position, string path) : base(position, path) { }

        //CARGO EL CONTENIDO (Modificacion de la funcion en DECORATION)
        public override void LoadContent(ContentManager content, Simulation simulation, Effect effect)
        {
            base.LoadContent(content, simulation, effect);
        }

        //ACTUALIZO (Modificacion de la funcion en DECORATION)
        public override void Update(Simulation simulation)
        {
            base.Update(simulation);
        }

        //DIBUJO EL MODELO (Modificacion de la funcion en DECORATION)
        public override void Draw (Matrix view, Matrix projection)
        {
            if (IsDead) return; //si el modelo esta muerto no lo dibuja 
            base.Draw(view, projection);
        }
        
        //DIBUJO LAS COLISIONES (Modificacion de la funcion en DECORATION)
        public override void DrawCollisionChamber(Gizmo gizmos, Simulation simulation)
        {   
            base.DrawCollisionChamber(gizmos, simulation);
        }

        //LOGICA DE COLISION
        public void HandleCollision()
        {
            if (IsDead) return;
            IsDead = true;
        }

        //Dinamicos
        public void modificarMatrixWorld(Matrix rotation, Vector3 position){
            _world = Matrix.CreateTranslation(-_modelCenter)
                    * Matrix.CreateScale(_visualScale)
                    * rotation 
                    * Matrix.CreateTranslation(position);
        }
    }
}
