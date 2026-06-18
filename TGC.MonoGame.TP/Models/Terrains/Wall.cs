using BepuPhysics;
using BepuPhysics.Collidables;

namespace TGC.MonoGame.TP.Models.Terrains;

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

