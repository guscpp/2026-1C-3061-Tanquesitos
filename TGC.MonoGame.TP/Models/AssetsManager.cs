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
using FuelBarrel = TGC.MonoGame.TP.Models.Decorations.FuelBarrel;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace TGC.MonoGame.TP.Models;

/// <summary>
///     Genera todas las clases de assets dentro del escenario aleatoriamente
/// </summary>
public class AssetsManager
{
    public const string ContentFolder3D = "Models/";
    public const string ContentFolderEffects = "Effects/";
    public const string ContentFolderMusic = "Music/";
    public const string ContentFolderSounds = "Sounds/";
    public const string ContentFolderSpriteFonts = "SpriteFonts/";
    public const string ContentFolderTextures = "Textures/";

    private const int NumberOfAssets = 400; //Entrega 1: exige 400 minimo

    // sobre modelos de decoraciones
    private int NumberOfDecorations => NumberOfAssets - NumberOfHouseModels;
    public List<FuelBarrel> _fuelBarrels = new();
    public List<Decoration> _decorationModels = new();
    private string[] DecorationModelPaths =
    {
        "decoraciones/arbol_muerto_1",
        "decoraciones/arbol_muerto_2",
        "decoraciones/barril",
        "decoraciones/cactus_1",
        "decoraciones/cactus_2",
        "decoraciones/cactus_3",
        "decoraciones/caja",
        "decoraciones/carreta_1",
        "decoraciones/carreta_2",
        "decoraciones/escaleras",
        "decoraciones/planta_rodadora",
        "decoraciones/pozo",
        "decoraciones/roca_1",
        "decoraciones/roca_2",
        "decoraciones/roca_3"
    };

    private readonly float[] _decorationSpawrates =
    {
        0.03f,  // arbol_muerto_1
        0.03f,  // arbol_muerto_2
        0.04f,  // barril
        0.10f,  // cactus_1    
        0.10f,  // cactus_2        
        0.10f,  // cactus_3       
        0.04f,  // caja
        0.03f,  // carreta_1
        0.03f,  // carreta_2
        0.03f,  // escaleras
        0.12f,  // planta_rodadora 
        0.04f,  // pozo
        0.15f,  // roca_1        
        0.13f,  // roca_2         
        0.03f,  // roca_3         
    };

    private readonly Random _random = new();

    public Color randomColor(){
        return (new Color(_random.Next(0,256),_random.Next(0,256),_random.Next(0,256)));
    }    

    // sobre el terreno
    private Terrain _terrain;

    // sobre modelos de casas
    private string[] HouseModelPaths =
    {
        "casas/casita_mediana",
        "casas/casita_pequeña",
        "casas/Large Building B",
        "casas/Medium Building B"
    };
    public const int NumberOfHouseModels = 15;
    public List<House> _houses = new();


    public AssetsManager(Terrain terrain)
    {
        _terrain = terrain;
    }

    public void Initialize()
    {

        var houseModelPositions = GetValidHousePositions();
        for (int i = 0; i < NumberOfHouseModels; i++)
        {
            _houses.Add(GetHouse(houseModelPositions[i]));
        }
        var decorationModelPositions = GetValidDecorationPositions();
        for (int i = 0; i < NumberOfDecorations; i++)
        {
            _decorationModels.Add(GetDecoration(decorationModelPositions[i]));
        } 
    }

    /// <summary>
    ///     Genera posiciones aleatorias para todas las casas dentro del terreno
    /// </summary>
    private List<Vector3> GetValidHousePositions()
    {
        var positions = new List<Vector3> {};
        var minDistanceBetween = 45f; // GE 4500f
        // inicializacion
        for (int i = 0; i < NumberOfHouseModels; i++)
            positions.Add(GetRandomPosition(_random));

        // chequeo
        for (int i = 0; i < NumberOfHouseModels; i++)
        {
            bool valid = false;
            while (!valid)
            {
                valid = true;
                for (int j = 0; j < i; j++)  // solo compara contra las anteriores ya validadas
                {
                    if (Vector3.Distance(positions[i], positions[j]) < minDistanceBetween)
                    {
                        positions[i] = GetRandomPosition(_random);
                        valid = false;
                        break;
                    }
                }
            }
        }

        return positions;
    }

    private Vector3 GetRandomPosition(Random random)
    {
        var minHorizontal = -_terrain.WidthUnits;
        var maxHorizontal = _terrain.WidthUnits;
        var horizontalRange = maxHorizontal - minHorizontal;

        var x = random.NextSingle() * horizontalRange + minHorizontal;
        var z = random.NextSingle() * horizontalRange + minHorizontal;
        return new Vector3(x, _terrain.GetHeight(x, z), z);
    }

    /// <summary>
    ///     Genera el path dentro del directorio Models de un modelo de casa para colocar en el mapa  
    /// </summary>
    /// <param name="content"></param>
    private string GetRandomHousePath()
    {
        var index =  _random.Next(0, HouseModelPaths.Length);
        return HouseModelPaths[index];
    }

    /// <summary>
    ///     Genera posiciones aleatorias para una casa dentro del terreno en un rango establecido 
    /// </summary>
    private List<Vector3> GetValidDecorationPositions()
    {
        var positions = new List<Vector3> {};
        var minDistanceToHouses = 30f;
        var minDistanceBetween = 10f;
  
        for(int i = 0; i < NumberOfDecorations; i++)
        {
            Vector3 candidate;
            bool valid;
            do
            {
                candidate = GetRandomPosition(_random);
                valid = true;
                // chequeo contra casas
                if(IsTooNearToAHouse(candidate, minDistanceToHouses))
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

    private bool IsTooNearToAHouse(Vector3 position, float minDistance)
    {
        for(int i=0; i<NumberOfHouseModels; i++)
        {
            if(Vector3.Distance(position, _houses[i].Position) < minDistance)
                return true;
        }
        return false;
    }

    /// <summary>
    ///     Genera el path dentro del directorio Models de un modelo de asset para colocar en el mapa  
    /// </summary>
    /// <param name="content"></param>
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

    public House GetHouse(Vector3 position)
    {
        var path = GetRandomHousePath();
        return new House(position, path);
    }

    public Decoration GetDecoration(Vector3 position)
    {
        var path = GetRandomAssetPath();

        // aplicar pequeno offset vertical a los modelos dinamicos cuando spawnean
        Vector3 dynamicPos = position + Vector3.Up * GameConfig.Assets.DynamicSpawnOffset;

        return path switch
        {
            // estaticos, no necesitan el offset
            var p when p.Contains("arbol")      => new Tree(position, path),
            var p when p.Contains("cactus")     => new Cactus(position, path),
            var p when p.Contains("roca")       => new Rock(position, path),

            // dinamicos, usan offset
            var p when p.Contains("barril")     => new Barrel(dynamicPos, path),
            var p when p.Contains("carreta")    => new Cart(dynamicPos, path),
            var p when p.Contains("planta")     => new Plant(dynamicPos, path),
            var p when p.Contains("caja")       => new WoodenBox(dynamicPos, path),
            var p when p.Contains("escaleras")  => new Stairs(dynamicPos, path),

            _                                   => new Decoration(position, path)
        };
    }

    /// <summary>
    ///     Spawnea los barriles de combustible
    /// </summary>
    public void SpawnFuelBarrels()
    {
        float minDistToDecorations = 8f;
        float minDistToHouses = 20f;
        float minDistToBarrels = 6f;

        for (int i = 0; i < GameConfig.FuelBarrel.SpawnCount; i++)
        {
            Vector3 pos;
            bool valid;
            do
            {
                pos = GetRandomPosition(_random);
                valid = true;

                // Validar distancia contra casas
                if (IsTooNearToAHouse(pos, minDistToHouses)) { valid = false; continue; }

                // Validar distancia contra decoraciones existentes
                foreach (var dec in _decorationModels)
                {
                    if (Vector3.Distance(pos, dec.Position) < minDistToDecorations) { valid = false; break; }
                }
                if (!valid) continue;

                // Validar distancia contra otros barriles ya colocados
                foreach (var barrel in _fuelBarrels)
                {
                    if (Vector3.Distance(pos, barrel.Position) < minDistToBarrels) { valid = false; break; }
                }
            } while (!valid);

            pos.Y += GameConfig.FuelBarrel.Height / 2f;

            _fuelBarrels.Add(new FuelBarrel(pos));
        }
    }

    /// <summary>
    ///     Gestiona la recarga de combustible
    /// </summary>
    public void UpdateFuelBarrels(float dt)
    {
        foreach (var barrel in _fuelBarrels)
        {
            barrel.UpdateRecharge(dt);
        }
    }
    public void LoadContent(ContentManager content, Simulation simulation)
    {
        //Cargo el efecto una vez
        var effect = content.Load<Effect>(ContentFolderEffects + "BasicShaderTexture");

        foreach (var house in _houses)
        {
            house.LoadContent(content, simulation, effect);
        }

        foreach (var asset in _decorationModels)
        {
            asset.LoadContent(content, simulation, effect);
        }

        foreach (var barrel in _fuelBarrels)
        {
            barrel.LoadContent(content, simulation, effect);
        }

    }

    public void Update(GameTime elapsedTime, Simulation simulation)
    {
        //simulation.Timestep(dt); //Se elimina porque ya lo controla el tgc game
        foreach (var asset in _decorationModels)
        {
            asset.Update(simulation);
        }
    }

    public void Draw(Matrix view, Matrix projection, Gizmo gizmos, Simulation simulation)
    {
        foreach (var house in _houses)
        {
            house.Draw(view, projection);
            house.DrawCollisionChamber(gizmos, null); //Como las casas ninguna se mueve la simulacion puede ser nula
        }

        foreach (var asset in _decorationModels)
        {
            asset.Draw(view, projection);
            asset.DrawCollisionChamber(gizmos, simulation);
        }

        foreach (var barrel in _fuelBarrels)
        {
            if (!barrel.IsCollected) barrel.Draw(view, projection);
            barrel.DrawCollisionChamber(gizmos, simulation);
        }
    }
}


