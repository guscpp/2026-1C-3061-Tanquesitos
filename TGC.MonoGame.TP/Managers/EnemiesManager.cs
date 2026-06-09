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
using TGC.MonoGame.TP.Models.Tanks;
using Terrain = TGC.MonoGame.TP.Models.Terrains.Terrain;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace TGC.MonoGame.TP.Managers;

/// <summary>
///     Genera todas las clases de assets dentro del escenario aleatoriamente
/// </summary>
public class EnemiesManager
{
    public const string ContentFolder3D = "Models/";
    public const string ContentFolderEffects = "Effects/";
    public const string ContentFolderMusic = "Music/";
    public const string ContentFolderSounds = "Sounds/";
    public const string ContentFolderSpriteFonts = "SpriteFonts/";
    public const string ContentFolderTextures = "Textures/";

    private int _enemiesCount = GameConfig.Enemies.EnemiesCount;
    public List<TankEnemy> _enemies = new();
    private List<BodyHandle> _enemiesHandles = new();
    
    //sobre el terreno
    private Terrain _terrain;
    //Random
    private readonly Random _random = new();
    //fisicas
    private Simulation _simulation;

    public EnemiesManager(Terrain terrain, Simulation simulation)
    {
        _terrain = terrain;
        _simulation = simulation;
    }

    public void Initialize()
    {

    }

    public void LoadContent(ContentManager content)
    {
        var tankModel = content.Load<Model>(ContentFolder3D + "tanques/tank v4");
        var tankTexture = content.Load<Texture2D>(ContentFolderTextures + "paleta_256x512");
        var effect = content.Load<Effect>(ContentFolderEffects + "BasicShaderTexture");

        for (int i = 0; i < _enemiesCount; i++)
        {
            // Inicializo los tanques y sus handles
            TankEnemy enemy = _random.Next(3) switch
            {
                0 => new TankEnemyScout(),
                1 => new TankEnemyMedium(),
                _ => new TankEnemyHeavy()
            };
            enemy.Position = enemy.GetPosition(_terrain, _random);
            enemy.Load(tankModel, tankTexture, effect, _simulation);
            _enemies.Add(enemy);
            _enemiesHandles.Add(enemy.TankHandler);
        }
    }

    public void Update(GameTime gameTime, Vector3 position) {
        foreach (var tankEnemy in _enemies)
        {
            tankEnemy.UpdateEnemy(gameTime, _simulation, position.ToNumerics(), _terrain);
        }
     }

    public void Draw(Matrix view, Matrix projection)
    {
        foreach(var tankEnemy in _enemies)
        {
            tankEnemy.Draw(view, projection);
        }
    }

    public void Reset(Simulation simulation)
    {
        // remover cuerpos físicos de la simulación
        foreach (var handle in _enemiesHandles)
            simulation.Bodies.Remove(handle);

        _enemies.Clear();
        _enemiesHandles.Clear();

        // recargar a los enemigos
        LoadContent(TGCGame.Instance.Content);
    }
}