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

    // sobre modelos de decoraciones
    private const int NumberOfDecorations = 200;
    private List<Decoration> _decorationModels = new();
    private string[] DecorationModelPath =
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

    private readonly Random _random = new();

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
    private const int NumberOfHouseModels = 10;
    private List<House> _houseModels = new();


    public AssetsManager(Terrain terrain)
    {
        _terrain = terrain;
    }

    public void Initialize()
    {
        for (int i = 0; i < NumberOfHouseModels; i++)
            _houseModels.Add(new House());
        for (int i = 0; i < NumberOfDecorations; i++)
            _decorationModels.Add(new Decoration());

    }

    /// <summary>
    ///     Genera posiciones aleatorias para una casa dentro del terreno en un rango establecido 
    /// </summary>
    private Vector3 GetRandomHousePosition(Random random)
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
    private Vector3 GetRandomAssetPosition(Random random)
    {
        var minHorizontal = -_terrain.WidthUnits;
        var maxHorizontal = _terrain.WidthUnits;
        var horizontalRange = maxHorizontal - minHorizontal;

        var x = random.NextSingle() * horizontalRange + minHorizontal;
        var z = random.NextSingle() * horizontalRange + minHorizontal;

        var position = new Vector3(x, _terrain.GetHeight(x, z), z);
       

        return position;
    }

    /// <summary>
    /// Genera el path dentro del directorio Models de un modelo de asset para colocar en el mapa  
    /// </summary>
    /// <param name="content"></param>
    private string GetRandomAssetPath()
    {
        var index =  _random.Next(0, DecorationModelPath.Length);
        // pienso agregarle probabilidades de aparicion a cada objeto
        return DecorationModelPath[index];
    }

    public void LoadContent(ContentManager content)
    {
        var littleHouseModel = content.Load<Model>(ContentFolder3D + "casas/casita_pequeña");
        var mediumHouseModel = content.Load<Model>(ContentFolder3D + "casas/casita_mediana");
        var largeHouseModel = content.Load<Model>(ContentFolder3D + "casas/Large Building B");

        foreach(var house in _houseModels)
        {
            var houseModel = content.Load<Model>(ContentFolder3D + GetRandomHousePath());
            house.LoadContent(houseModel, GetRandomHousePosition(_random));
        }
        foreach(var asset in _decorationModels)
        {
            var assetModel = content.Load<Model>(ContentFolder3D + GetRandomAssetPath());
            asset.LoadContent(assetModel, GetRandomHousePosition(_random));
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
