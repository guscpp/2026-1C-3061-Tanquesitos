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
    //PATHS
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
    private string[] HouseModelPaths =
    {
        "casas/casita_mediana",
        "casas/casita_pequeña",
        "casas/Large Building B",
        "casas/Medium Building B"
    };
    //SPAWRATES
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
    //Entrega 1: exige 400 minimo
    private const int NumberOfAssets = 400; 
    // sobre modelos de decoraciones
    private int NumberOfDecorations => NumberOfAssets - NumberOfHouseModels;
    public List<FuelBarrel> _fuelBarrels = new();
    public List<Decoration> _decorationModels = new();
    // sobre modelos de casas
    public const int NumberOfHouseModels = 15;
    public List<House> _houses = new();
    // sobre el terreno
    private Terrain _terrain;
    //Random
    private readonly Random _random = new();

    public Color randomColor(){
        return (new Color(_random.Next(0,256),_random.Next(0,256),_random.Next(0,256)));
    }    

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

    public void LoadContent(ContentManager content, Simulation simulation)
    {
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
        // Recorremos la lista de atrás hacia adelante para poder borrar elementos de forma segura
        //Recorro la lista de elementos decorativos (conforme los mato la lista disminuye)
        for (int i = _decorationModels.Count - 1; i >= 0; i--)
        /*Tuve que quitar el foreach porque se rompia al colisionar con el objeto xdd, no decia porque, intuyo que era porque el ciclo ya no era el mismo
        al eliminar objetos de la lista, no se, la ia me recomendo cambiar a for y funco :D*/
        {
            //Tomo el asset
            var asset = _decorationModels[i];

            // Reviso que sea dinamico, sino me importa poco
            if (asset is Dinamic dinamicAsset)
            {
                // Reviso si esta muerto
                if (dinamicAsset.IsDead)
                {
                    // Lo borro de bepu
                    simulation.Bodies.Remove(dinamicAsset.bodyHandle);

                    //Lo borro de la lista
                    _decorationModels.RemoveAt(i);
                    continue; 
                }
            }

            // Si es estatico o no esta muerto se actualiza normal
            asset.Update(simulation);
        }
    }

    public void Draw(Matrix view, Matrix projection, Gizmo gizmos, Simulation simulation)
    {
        foreach (var house in _houses)
        {
            house.Draw(view, projection);
            house.DrawCollisionChamber(gizmos, simulation);
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
        var path = GetRandomAssetPath();
        return path switch
        {
            var p when p.Contains("arbol")      => new Tree(position, path),
            var p when p.Contains("cactus")     => new Cactus(position, path),
            var p when p.Contains("roca")       => new Rock(position, path),
            var p when p.Contains("pozo")       => new Pozo(position, path),
            var p when p.Contains("barril")     => new Barrel(dynamicPos, path),
            var p when p.Contains("carreta")    => new Cart(dynamicPos, path),
            var p when p.Contains("planta")     => new Plant(dynamicPos, path),
            var p when p.Contains("caja")       => new WoodenBox(dynamicPos, path),
            var p when p.Contains("escaleras")  => new Stairs(dynamicPos, path),
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
  
        for(int i = 0; i < NumberOfDecorations; i++)
        {
            Vector3 candidate;
            bool valid;
            do
            {
                candidate = GetRandomPosition();
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

    //Me dice si la posicion esta muy cerca de una casa
    private bool IsTooNearToAHouse(Vector3 position, float minDistance)
    {
        for(int i=0; i<NumberOfHouseModels; i++)
        {
            if(Vector3.Distance(position, _houses[i].Position) < minDistance)
                return true;
        }
        return false;
    }

    //Genero una nueva casa segun una posicion
    public House GetHouse(Vector3 position)
    {
        return new House(position, GetRandomHousePath());
    }

    // Genera el path dentro del directorio Models de un modelo de casa para colocar en el mapa  
    private string GetRandomHousePath()
    {
        var index =  _random.Next(0, HouseModelPaths.Length);
        return HouseModelPaths[index];
    }
    
    // Genera posiciones aleatorias para todas las casas dentro del terreno
    private List<Vector3> GetValidHousePositions()
    {
        var positions = new List<Vector3> {};
        var minDistanceBetween = 45f; // GE 4500f
        // inicializacion
        for (int i = 0; i < NumberOfHouseModels; i++)
            positions.Add(GetRandomPosition());

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
                        positions[i] = GetRandomPosition();
                        valid = false;
                        break;
                    }
                }
            }
        }

        return positions;
    }

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

    public void UpdateFuelBarrels(float dt)
    {
        foreach (var barrel in _fuelBarrels)
        {
            barrel.UpdateRecharge(dt);
        }
    }


}


