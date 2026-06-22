using BepuPhysics;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities.Memory;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TGC.MonoGame.TP.Gizmos;
using TGC.MonoGame.TP.Models.Decorations;
using static TGC.MonoGame.TP.GameConfig;
using Terrain = TGC.MonoGame.TP.Models.Terrains.Terrain;
using FuelBarrel = TGC.MonoGame.TP.Models.Decorations.FuelBarrel;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace TGC.MonoGame.TP.Managers;

/// <summary>
///     Genera todas las clases de assets dentro del escenario aleatoriamente
/// </summary>
public class StaticsManager
{
    public const string ContentFolder3D = "Models/";
    public const string ContentFolderEffects = "Effects/";
    public const string ContentFolderMusic = "Music/";
    public const string ContentFolderSounds = "Sounds/";
    public const string ContentFolderSpriteFonts = "SpriteFonts/";
    public const string ContentFolderTextures = "Textures/";
    //PATHS
    private string[] DecorationModelPaths =
    {
        "decoraciones/arbol_muerto_1",
        "decoraciones/arbol_muerto_2",
        "decoraciones/cactus_1",
        "decoraciones/cactus_2",
        "decoraciones/cactus_3",
        "decoraciones/pozo",
        "decoraciones/roca_1",
        "decoraciones/roca_2",
        "decoraciones/roca_3"
    };
    //SPAWRATES
    private readonly float[] _decorationSpawrates =
    {
        0.03f,  // arbol_muerto_1
        0.03f,  // arbol_muerto_2
        0.10f,  // cactus_1    
        0.10f,  // cactus_2        
        0.10f,  // cactus_3       
        0.04f,  // pozo
        0.15f,  // roca_1        
        0.13f,  // roca_2         
        0.03f,  // roca_3         
    };
    //Entrega 1: exige 400 minimo
    private const int NumberOfAssets = 200; 
    // sobre modelos de decoraciones
    private int NumberOfDecorations => NumberOfAssets - 15;
    public List<Decoration> _decorationModels = new();
    public List<Vector3> _houses = new();
    // sobre el terreno
    private Terrain _terrain;
    //Random
    private readonly Random _random = new();  

    public StaticsManager(Terrain terrain, List<Vector3> houses)
    {
        _terrain = terrain;
        _houses = houses;
    }

    public void Initialize()
    {
        var decorationModelPositions = GetValidDecorationPositions();
        for (int i = 0; i < NumberOfDecorations; i++)
        {
            _decorationModels.Add(GetDecoration(decorationModelPositions[i]));
        } 
    }

    public void LoadContent(ContentManager content, Simulation simulation)
    {
        var effect = content.Load<Effect>(ContentFolderEffects + "ShadowMap");

        foreach (var asset in _decorationModels)
        {
            asset.LoadContent(content, simulation, effect);
        }
    }

    public void Update(GameTime elapsedTime, Simulation simulation) { }

    public void Draw(Matrix view, Matrix projection, Gizmo gizmos, Simulation simulation)
    {
        foreach (var asset in _decorationModels)
        {
            asset.Draw(view, projection);
        }
    }

    public void DrawDepth(Matrix lightViewProjection)
    {
        foreach (var asset in _decorationModels)
        {
            asset.DrawDepth(lightViewProjection);
        }
    }

    //Me da una posicion aleatoria sobre el terreno
    private Vector3 GetRandomPosition()
    {
        var minHorizontal = -_terrain.WidthUnits;
        var maxHorizontal = _terrain.WidthUnits;
        var horizontalRange = maxHorizontal - minHorizontal;

        var x = _random.NextSingle() * horizontalRange + minHorizontal;
        var z = _random.NextSingle() * horizontalRange + minHorizontal;
        return new Vector3(x, _terrain.GetHeight(x, z), z);
    }

    //Me genera una nueva decoracion con la posicion que le paso
    public Decoration GetDecoration(Vector3 position)
    {
        Vector3 dynamicPos = position + Vector3.Up * GameConfig.Assets.DynamicSpawnOffset;
        Vector3 rocaPos = position + Vector3.Up * 1.0f;
        var path = GetRandomAssetPath();
        return path switch
        {
            var p when p.Contains("arbol")      => new Tree(position, path),
            var p when p.Contains("cactus")     => new Cactus(position, path),
            var p when p.Contains("roca")       => new Rock(rocaPos, path),
            var p when p.Contains("pozo")       => new Pozo(position, path),
            _                                   => new Decoration(position, path)
        };
    }

    // Me da un path aleatorio para una decoracion
    private string GetRandomAssetPath()
    {
        var aux = _random.NextSingle();
        var acum = 0f;
        var index =  _random.Next(0, DecorationModelPaths.Length);
        
        for(int i=0; i<DecorationModelPaths.Length; i++)
        {
            acum += _decorationSpawrates[i];
            if(aux<acum)
                return DecorationModelPaths[i];   
        }

        return DecorationModelPaths[index];
    }
    
    // Genera posiciones para las decoraciones
    private List<Vector3> GetValidDecorationPositions()
    {
        var positions = new List<Vector3> {};
        var minDistanceToHouses = 30f;
        var minDistanceBetween = 10f;
        var minDistanceToSpawn = 20f;
        float margin = 8f;
        float playAreaLimit = _terrain.WidthUnits - margin;
  
        for(int i = 0; i < NumberOfDecorations; i++)
        {
            Vector3 candidate;
            bool valid;
            var spawnPoint = new Vector3(0, _terrain.GetHeight(0, 0), 0);
            do
            {
                candidate = GetRandomPosition();
                valid = true;
                // chequeo contra casas o spawnpoint
                if(Math.Abs(candidate.X) >= playAreaLimit || Math.Abs(candidate.Z) >= playAreaLimit)
                {
                    valid = false;
                    continue;
                }
                if(IsTooNearToAHouse(candidate, minDistanceToHouses) ||
                    Vector3.Distance(candidate, spawnPoint) < minDistanceToSpawn)
                {
                    valid = false;
                    continue;
                }
                // chequeo contra otras decoraciones ya colocadas
                for(int j = 0; j < i; j++)
                {
                    if(Vector3.Distance(candidate, positions[j]) < minDistanceBetween)
                    {
                        valid = false;
                        break;
                    }
                }

            } while(!valid);

            positions.Add(candidate);
        }

        return positions;
    }

    //Me dice si la posicion esta muy cerca de una casa
    private bool IsTooNearToAHouse(Vector3 position, float minDistance)
    {
        for(int i=0; i<_houses.Count(); i++)
        {
            if(Vector3.Distance(position, _houses[i]) < minDistance)
                return true;
        }
        return false;
    }

    public List<Vector3> GetDecorations()
    {
        return _decorationModels.Select(decoration => decoration.Position).ToList();
    }
}
