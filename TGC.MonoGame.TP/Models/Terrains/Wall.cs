using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities.Memory;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Transactions;
using TGC.MonoGame.Samples.Physics.Bepu;
using TGC.MonoGame.TP.Cameras;
using TGC.MonoGame.TP.Gizmos;
using TGC.MonoGame.TP.Models;
using TGC.MonoGame.TP.Models.Decorations;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace TGC.MonoGame.TP.Models;

public class Wall 
{ 
    private Simulation _simulation;

    public Wall(Simulation simulation) {
        _simulation = simulation;
     }

    public void LoadContent(Terrain terrain)
    {
        // 4 paredes invisibles que rodean el mapa e impiden que salgan los objetos
        float halfSize = terrain.WidthUnits; // ~259 unidades
        float margin = 8f;
        float playAreaLimit = halfSize - margin; // Los muros invisibles quedan un poco adentro del mapa
        float wallHeight = 60f; // Un poco más alto que el terreno máximo (35m)
        float wallThickness = 2f;

        // Shape para muros Norte/Sur (largos en X, finos en Z)
        var wallShapeNS = new Box(playAreaLimit * 2, wallHeight, wallThickness);
        var idxNS = _simulation.Shapes.Add(wallShapeNS);

        // Shape para muros Este/Oeste (finos en X, largos en Z)
        var wallShapeEW = new Box(wallThickness, wallHeight, playAreaLimit * 2);
        var idxEW = _simulation.Shapes.Add(wallShapeEW);

        // Norte (-Z)
        _simulation.Statics.Add(new StaticDescription(new System.Numerics.Vector3(0, wallHeight / 2f, -playAreaLimit), idxNS));
        // Sur (+Z)
        _simulation.Statics.Add(new StaticDescription(new System.Numerics.Vector3(0, wallHeight / 2f, playAreaLimit), idxNS));
        // Oeste (-X)
        _simulation.Statics.Add(new StaticDescription(new System.Numerics.Vector3(-playAreaLimit, wallHeight / 2f, 0), idxEW));
        // Este (+X)
        _simulation.Statics.Add(new StaticDescription(new System.Numerics.Vector3(playAreaLimit, wallHeight / 2f, 0), idxEW));
    }
}

