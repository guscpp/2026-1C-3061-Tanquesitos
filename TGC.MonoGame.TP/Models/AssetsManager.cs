using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.Models;

/// <summary>
/// Genera todas las clases de assets dentro del escenario aleatoriamente
/// </summary>
public class AssetsManager
{
    public const string ContentFolder3D = "Models/";
    public const string ContentFolderEffects = "Effects/";
    public const string ContentFolderMusic = "Music/";
    public const string ContentFolderSounds = "Sounds/";
    public const string ContentFolderSpriteFonts = "SpriteFonts/";
    public const string ContentFolderTextures = "Textures/";

    private const int NumberOfAssets = 400;

    // sobre modelos de decoraciones
    private int NumberOfDecorations => NumberOfAssets - NumberOfHouseModels;
    private List<Decoration> _decorationModels = new();
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
        "decoraciones/roca_2"
        //"decoraciones/roca_3"
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

    // sobre el terreno
    private Terrain _terrain;

    // sobre modelos de casas
    private string[] HouseModelPaths =
    {
        "casas/casita_mediana",
        //"casas/casita_pequeña",
        "casas/Large Building B",
        "casas/Medium Building B"
    };
    private const int NumberOfHouseModels = 25;
    private List<House> _houseModels = new();


    public AssetsManager(Terrain terrain)
    {
        _terrain = terrain;
    }

    public void Initialize()
    {
        for (int i = 0; i < NumberOfHouseModels; i++)
        {
            _houseModels.Add(new House());
            _houseModels[i].Initialize();
        }
        for (int i = 0; i < NumberOfDecorations; i++)
        {
            _decorationModels.Add(new Decoration());
            _decorationModels[i].Initialize();
        } 
    }

    /// <summary>
    ///     Genera posiciones aleatorias para todas las casas dentro del terreno
    /// </summary>
    private List<Vector3> GetValidHousePositions()
    {
        var positions = new List<Vector3> {};
        var minDistanceBetween = 4500f;
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
    /// Genera el path dentro del directorio Models de un modelo de casa para colocar en el mapa  
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
        var minDistanceToHouses = 3000f; 
        var minDistanceBetween = 1000f;
  
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
            if(Vector3.Distance(position, _houseModels[i].Position) < minDistance)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Genera el path dentro del directorio Models de un modelo de asset para colocar en el mapa  
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

    public void LoadContent(ContentManager content)
    {

        int i = 0;
        var houseModelPositions = GetValidHousePositions();
        foreach(var house in _houseModels)
        {
            var houseModel = content.Load<Model>(ContentFolder3D + GetRandomHousePath());
            house.LoadContent(houseModel, houseModelPositions[i], _random.NextSingle());
            i++;
        }
        i = 0;
        var decorationModelPositions = GetValidDecorationPositions();
        foreach(var asset in _decorationModels)
        {
            var assetModel = content.Load<Model>(ContentFolder3D + GetRandomAssetPath());
            asset.LoadContent(assetModel, decorationModelPositions[i], _random.NextSingle());
            i++;
        }
    }

    public void Update() { }

    public void Draw(Matrix view, Matrix projection)
    {
        foreach (var house in _houseModels)
        {
            house.Draw(view, projection);
        }
        foreach(var asset in _decorationModels)
        {
            asset.Draw(view, projection);
        }
    }
}
