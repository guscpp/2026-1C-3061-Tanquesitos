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
using static TGC.MonoGame.TP.GameConfig;
using TGC.MonoGame.TP.Models;
using Terrain = TGC.MonoGame.TP.Models.Terrain;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace TGC.MonoGame.TP.Managers;

/// <summary>
///     Genera todas las clases de assets dentro del escenario aleatoriamente
/// </summary>
public class EnemiesManager
{/*
    public const string ContentFolder3D = "Models/";
    public const string ContentFolderEffects = "Effects/";
    public const string ContentFolderMusic = "Music/";
    public const string ContentFolderSounds = "Sounds/";
    public const string ContentFolderSpriteFonts = "SpriteFonts/";
    public const string ContentFolderTextures = "Textures/";

    private int _enemiesCount = 15;
    public List<TankEnemy> _enemies = new ();
    private List<BodyHandle> _enemiesHandles = new ();
    private List<Cannonball> _enemiesCanonballs = new();
    
    // sobre el terreno
    private Terrain _terrain;
    //Random
    private readonly Random _random = new();

    public EnemiesManager(Terrain terrain)
    {
        _terrain = terrain;
    }

    public void Initialize()
    {

    }

    public void LoadContent(Model tankModel, Texture2D tankTexture, Effect effect2, Simulation simulation)
    {
        for(int i=0; i<_enemiesCount; i++)
        {   // Inicializo los tanques y sus handles
            var TankEnemy = new TankPlayer();
            TankEnemy.Position = TankEnemy.GetPosition(_terrain, _random);
            TankEnemy.Load(tankModel, tankTexture, effect2, simulation);
            _enemies.Add(TankEnemy);
            _enemiesHandles.Add(TankEnemy.TankHandler);
        }
    }

    public void Update(GameTime elapsedTime, Simulation simulation) { }

    public void Draw(Matrix view, Matrix projection, Gizmo gizmos, Simulation simulation)
    {
        foreach(var TankEnemy in _enemies)
        {
            TankEnemy.Draw(view, projection);
        }
    }*/
}