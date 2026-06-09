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
public class DinamicsManager
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
        "decoraciones/barril",
        "decoraciones/caja",
        "decoraciones/carreta_1",
        "decoraciones/carreta_2",
        "decoraciones/escaleras",
        "decoraciones/planta_rodadora"
    };
    //SPAWRATES
    private readonly float[] _decorationSpawrates =
    {
        0.04f,  // barril  
        0.04f,  // caja
        0.03f,  // carreta_1
        0.03f,  // carreta_2
        0.03f,  // escaleras
        0.12f,  // planta_rodadora         
    };
    //Entrega 1: exige 400 minimo
    private const int NumberOfAssets = 200; 
    public List<Decoration> _dynamicDecorations = new();
    private List<Vector3> _staticDecorations;
    public List<Vector3> _houses = new();
    // sobre el terreno
    private Terrain _terrain;
    //Random
    private readonly Random _random = new();

    public DinamicsManager(Terrain terrain, List<Vector3> staticDecorations, List<Vector3> houses)
    {
        _terrain = terrain;
        _staticDecorations = staticDecorations;
        _houses = houses;
    }

    public void Initialize()
    {
        var decorationModelPositions = GetValidDecorationPositions();
        for (int i = 0; i < NumberOfAssets; i++)
        {
            _dynamicDecorations.Add(GetDecoration(decorationModelPositions[i]));
        } 
    }

    public void LoadContent(ContentManager content, Simulation simulation)
    {
        var effect = content.Load<Effect>(ContentFolderEffects + "BasicShaderTexture");

        foreach (var asset in _dynamicDecorations)
        {
            asset.LoadContent(content, simulation, effect);
        }
    }

    public void Update(GameTime elapsedTime, Simulation simulation)
    {
        // Recorremos la lista de atrás hacia adelante para poder borrar elementos de forma segura
        //Recorro la lista de elementos decorativos (conforme los mato la lista disminuye)
        for (int i = _dynamicDecorations.Count - 1; i >= 0; i--)
        /*Tuve que quitar el foreach porque se rompia al colisionar con el objeto xdd, no decia porque, intuyo que era porque el ciclo ya no era el mismo
        al eliminar objetos de la lista, no se, la ia me recomendo cambiar a for y funco :D*/
        {
            //Tomo el asset
            var asset = _dynamicDecorations[i];

            // Reviso que sea dinamico, sino me importa poco
            if (asset is Dinamic dinamicAsset)
            {
                // Reviso si esta muerto
                if (dinamicAsset.IsDead)
                {
                    // Lo borro de bepu
                    simulation.Bodies.Remove(dinamicAsset.bodyHandle);

                    //Lo borro de la lista
                    _dynamicDecorations.RemoveAt(i);
                    continue; 
                }
            }

            // Si no esta muerto se actualiza normal
            asset.Update(simulation);
        }
    }

    public void Draw(Matrix view, Matrix projection, Gizmo gizmos, Simulation simulation)
    {
        foreach (var asset in _dynamicDecorations)
        {
            asset.Draw(view, projection);
            //asset.DrawCollisionChamber(gizmos, simulation);
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
        return new Vector3(x, _terrain.GetHeight(x, z)+2, z);
    }

    //Me genera una nueva decoracion con la posicion que le paso
    public Decoration GetDecoration(Vector3 position)
    {
        Vector3 dynamicPos = position + Vector3.Up * GameConfig.Assets.DynamicSpawnOffset;
        Vector3 rocaPos = position + Vector3.Up * 1.5f;
        var path = GetRandomAssetPath();
        return path switch
        {
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
        var minDistanceToSpawn = 20f;
        float margin = 8f;
        float playAreaLimit = _terrain.WidthUnits - margin;
  
        for(int i = 0; i < NumberOfAssets; i++)
        {
            Vector3 candidate;
            bool valid;
            var spawnPoint = new Vector3(0, _terrain.GetHeight(0, 0), 0);
            do
            {
                candidate = GetRandomPosition();
                valid = true;
                if(Math.Abs(candidate.X) >= playAreaLimit || Math.Abs(candidate.Z) >= playAreaLimit)
                {
                    valid = false;
                    continue;
                }
                // chequeo contra casas o spawnpoint
                if(IsTooNearToAHouse(candidate, minDistanceToHouses) ||
                    Vector3.Distance(candidate, spawnPoint) < minDistanceToSpawn)
                {
                    valid = false;
                    continue;
                }
                // chequeo contra decoraciones estaticas ya colocadas
                foreach (var staticAsset in _staticDecorations)
                {
                    if (Vector3.Distance(candidate, staticAsset) < minDistanceBetween)
                    {
                        valid = false;
                        break;
                    }
                }
                if (!valid) continue;

                // chequeo contra decoraciones dinamicas ya colocadas
                foreach (var approvedPos in positions)
                {
                    if (Vector3.Distance(candidate, approvedPos) < minDistanceBetween)
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

    public List<Decoration> GetDecorations()
    {
        return new List<Decoration>(_dynamicDecorations);
    }
}

