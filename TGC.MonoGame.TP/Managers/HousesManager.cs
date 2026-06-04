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
using Terrain = TGC.MonoGame.TP.Models.Terrain;
using FuelBarrel = TGC.MonoGame.TP.Models.Decorations.FuelBarrel;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace TGC.MonoGame.TP.Models;

/// <summary>
///     Genera todas las clases de assets dentro del escenario aleatoriamente
/// </summary>
public class HousesManager
{
    public const string ContentFolder3D = "Models/";
    public const string ContentFolderEffects = "Effects/";
    public const string ContentFolderMusic = "Music/";
    public const string ContentFolderSounds = "Sounds/";
    public const string ContentFolderSpriteFonts = "SpriteFonts/";
    public const string ContentFolderTextures = "Textures/";
    //PATHS
    private string[] HouseModelPaths =
    {
        "casas/casita_mediana",
        "casas/casita_pequeña",
        "casas/Large Building B",
        "casas/Medium Building B"
    };
    public const int NumberOfHouseModels = 15;
    public List<House> _houses = new();
    // sobre el terreno
    private Terrain _terrain;
    //Random
    private readonly Random _random = new();  

    public HousesManager(Terrain terrain)
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
    }

    public void LoadContent(ContentManager content, Simulation simulation)
    {
        var effect = content.Load<Effect>(ContentFolderEffects + "BasicShaderTexture");

        foreach (var house in _houses)
        {
            house.LoadContent(content, simulation, effect);
        }
    }

    public void Update(GameTime elapsedTime, Simulation simulation) { }

    public void Draw(Matrix view, Matrix projection, Gizmo gizmos, Simulation simulation)
    {
        foreach (var house in _houses)
        {
            house.Draw(view, projection);
            house.DrawCollisionChamber(gizmos, simulation);
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
        var minDistanceToSpawn =  20f;
        var spawnPoint = new Vector3(0, _terrain.GetHeight(0, 0), 0);

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
                    if (Vector3.Distance(positions[i], positions[j]) < minDistanceBetween ||
                        Vector3.Distance(positions[i], spawnPoint) < minDistanceToSpawn)
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

    public List<House> getHouses()
    {
        return new List<House>(_houses);
    }
}


