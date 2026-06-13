﻿using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities.Memory;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Transactions;
using TGC.MonoGame.Samples.Physics.Bepu;
using TGC.MonoGame.TP.Cameras;
using TGC.MonoGame.TP.Gizmos;
using TGC.MonoGame.TP.Models;
using TGC.MonoGame.TP.Models.Terrains;
using TGC.MonoGame.TP.Models.Decorations;
using TGC.MonoGame.TP.Managers;
using TGC.MonoGame.TP.Models.Tanks;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace TGC.MonoGame.TP;

/// <summary>
///     Esta es la clase principal del juego.
///     Inicialmente puede ser renombrado o copiado para hacer mas ejemplos chicos, en el caso de copiar para que se
///     ejecute el nuevo ejemplo deben cambiar la clase que ejecuta Program <see cref="Program.Main()" /> linea 10.
/// </summary>
public class TGCGame : Game
{
    //Carpetas
    public const string ContentFolder3D = "Models/";
    public const string ContentFolderEffects = "Effects/";
    public const string ContentFolderMusic = "Music/";
    public const string ContentFolderSounds = "Sounds/";
    public const string ContentFolderSpriteFonts = "SpriteFonts/";
    public const string ContentFolderTextures = "Textures/";
    //-----------JUEGO
    //graficos
    private readonly GraphicsDeviceManager _graphics;
    //effects
    private Effect _effect;
    //escena
    private Matrix _projection;
    private Matrix _view;
    private Matrix _world;
    //random
    private readonly Random _random = new();
    //teclado
    private KeyboardState _lastKeyboardState;
    //mouse
    private MouseState _previousMouseState;
    //hud
    private Hud _hud;
    //gamestate
    private GameStateManager _gameStateManager;
    //-----------TANQUE
    public TankPlayer _tank;
    private TankFollowCamera _camera;
    public TankFollowCamera Camera => _camera;
    public string[] tankPaths =
    {
        "tanques/tank v4", // Scout
        "tanques/tank v4", // Medium
        "tanques/tank v4"  // Heavy
    };
    //-----------TERRENO
    private Terrain _terrain;
    private Wall _wall;
    public StaticHandle TerrainHandle => _terrainStaticHandle;
    //-----------Manager
    public HousesManager _housesManager;
    public StaticsManager _staticsManager;
    public DinamicsManager _dinamicsManager;
    public BarrelsManager _barrelsManager;
    public EnemiesManager _enemiesManager;
    private SoundManager _soundManager;
    public SoundManager SoundManager => _gameStateManager.SoundManager;
    public SimpleCollisionTracker CollisionTracker { get; private set; } = new SimpleCollisionTracker();
    //-----------FISICAS
    private Simulation _simulation;
    private BufferPool _bufferPool;
    private BodyHandle _tankHandle;
    private StaticHandle _terrainStaticHandle;
    public static TGCGame Instance { get; private set; } //Esto es para que lo use NarrowPhaseCallbacks
    //gizmos
    private Gizmo _gizmos = new();
    private List<Cannonball> _cannonballs = new();
    public List<Cannonball> Cannonballs => _cannonballs;
    private Model _cannonballModel;
    private float _shootCooldown = GameConfig.Tank.Cooldown;
    private float _currentShootCooldown = 0f;

    // Variable para guardar la eleccion del jugador desde el menu
    public static GameConfig.TankClass SelectedPlayerTank;
    // Variable para contabilizar las kills del jugador
    public int EnemiesKilled = 0;

    public TGCGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Instance = this;
        Window.Title = "Tanquesitos";
        _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width - 100;
        _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height - 100;
        Content.RootDirectory = "Content";
        IsMouseVisible = true; //Oculto el mouse porque da dolor de cabeza
    }

    protected override void Initialize()
    {
        // Se activa el Backface Culling en sentido anti-horario (se renderizan las caras frontales de los triangulos)
        GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;

        // Configuramos nuestras matrices de la escena.
        _world = Matrix.Identity;
        _view = Matrix.CreateLookAt(Vector3.UnitZ * 150, Vector3.Zero, Vector3.Up);
        _projection =
            Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio, 1, 250);

        InitializePhysics();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        //Sonido
        _soundManager = new SoundManager();
        _soundManager.LoadContent(Content);

        //GameStateManager
        _gameStateManager = new GameStateManager(GraphicsDevice, Content, _soundManager);

        //RECURSOS
        //shaders
        _effect = Content.Load<Effect>(ContentFolderEffects + "BasicShader"); //modelos sin texturas
        var textureEffect = Content.Load<Effect>(ContentFolderEffects + "BasicShaderTexture"); //modelos con textura
        textureEffect.Parameters["DiffuseColor"].SetValue(Microsoft.Xna.Framework.Color.White.ToVector3());

        //texturas
        var terrainTexture = Content.Load<Texture2D>("Models/heightmaps/heightmap_512x512");
        var groundTexture = Content.Load<Texture2D>(ContentFolderTextures + "sand_1024_seamless");
        var tankTexture = Content.Load<Texture2D>(ContentFolderTextures + "paleta_256x512");

        //modelos
        _cannonballModel = Content.Load<Model>(ContentFolder3D + "cannonball/cannonball");

        //AUXILIARES
        Vector3 spawnPos = new Vector3(0, 0, 0);

        //TERRENO
        //Creo un terreno (suelo)
        _terrain = new Terrain(GraphicsDevice);
        //Le paso la textura y el efecto
        _terrain.LoadContent(terrainTexture, groundTexture, textureEffect);
        //fisicas
        _terrainStaticHandle = _terrain.CreatePhysicsTerrain(_simulation);
        //paredes invisibles
        _wall = new Wall(_simulation);
        _wall.LoadContent(_terrain);

        //ASSETS DECORATIVOS
        //casas
        _housesManager = new HousesManager(_terrain);
        _housesManager.Initialize();
        _housesManager.LoadContent(Content, _simulation);
        //estaticos
        _staticsManager = new StaticsManager(_terrain, _housesManager.getHouses());
        _staticsManager.Initialize();
        _staticsManager.LoadContent(Content, _simulation);
        //dinamicos
        _dinamicsManager = new DinamicsManager(_terrain, _staticsManager.GetDecorations(), _housesManager.getHouses());
        _dinamicsManager.Initialize();
        _dinamicsManager.LoadContent(Content, _simulation);
        
        //ENEMIGOS
        _enemiesManager = new EnemiesManager(_terrain, _simulation);
        _enemiesManager.LoadContent(Content);

        //BARRELS
        _barrelsManager = new BarrelsManager(_terrain, _staticsManager.GetDecorations(), _housesManager.getHouses());
        _barrelsManager.Initialize();
        _barrelsManager.LoadContent(Content, _simulation);

        //TANQUE
        var kb = Keyboard.GetState();
        _gameStateManager.HandleMenuState(kb, _lastKeyboardState);
        _lastKeyboardState = kb;
        // Crear el tanque usando la eleccion del jugador
        _tank = new TankPlayer(SelectedPlayerTank);
        var tankModel = Content.Load<Model>(ContentFolder3D + getTankPath());
        //Determino una posicion para el tanque
        float terrainY = _terrain.GetHeight(spawnPos.X, spawnPos.Z);//Se spawnea unos metros por encima del terreno
        _tank.Position = new Vector3(spawnPos.X, terrainY + GameConfig.Tank.SpawnZMargin, spawnPos.Z);
        //Cargo el tanque
        _tank.Load(tankModel, tankTexture, textureEffect, _simulation);
        //fisicas
        _tankHandle = _tank.TankHandler;

        //HUD
        _hud = new Hud();
        _hud.LoadContent(Content, GraphicsDevice);

        //CAMARA
        _camera = new TankFollowCamera(GraphicsDevice.Viewport.AspectRatio, _tank.Position);

        //GIZMOS
        _gizmos.LoadContent(GraphicsDevice, Content);

        base.LoadContent();
    }

    private String getTankPath()
    {
        return SelectedPlayerTank is GameConfig.TankClass.Scout ? tankPaths[0] :
            SelectedPlayerTank is GameConfig.TankClass.Medium ? tankPaths[1] :
            tankPaths[2];
    }

    protected override void Update(GameTime gameTime)
    {
        var kb = Keyboard.GetState();
        _gameStateManager.Update(gameTime, kb, _lastKeyboardState);
        _lastKeyboardState = kb;

        if (kb.IsKeyDown(Keys.P) && !_lastKeyboardState.IsKeyDown(Keys.P)) 
        {
            _gameStateManager.ForceState(_gameStateManager.CurrentState == GameState.Playing ? 
                GameState.Paused : GameState.Playing);
        }
        //El update del juego ocurre unicamente en estado Playing, sino se sale temprano
        if (_gameStateManager.CurrentState != GameState.Playing)
        {
            base.Update(gameTime);
            return;
        }

        if (kb.IsKeyDown(Keys.Escape)) Exit();

        CollisionTracker.BeginFrame();

        _simulation.Timestep(1 / 60f);

        _tank.Update(gameTime, kb, _simulation);

        //verificar si pueden recogerse los barriles
        foreach (var barrel in _barrelsManager._fuelBarrels)
        {
            if (!barrel.IsCollected) barrel.TryCollect(_tank, _simulation);
        }

        _enemiesManager.Update(gameTime, _tank.Position);

        _housesManager.Update(gameTime, _simulation);
        _staticsManager.Update(gameTime, _simulation);
        _dinamicsManager.Update(gameTime, _simulation);
        _barrelsManager.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

        // cada frame el tiempo restante de cooldown baja
        _currentShootCooldown -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        MouseState currentMouseState = Mouse.GetState();

        if (currentMouseState.LeftButton == ButtonState.Pressed 
        && _previousMouseState.LeftButton == ButtonState.Released 
        && _currentShootCooldown <= 0f)
        {
            Vector3 direction = _tank.CannonForward;
            direction.Normalize();

            // Posición desde donde sale la bala
            Vector3 spawnPosition = _tank.CannonMuzzlePosition + (direction * GameConfig.Tank.CannonSpawnOffsetForward);
            Cannonball cannonball = CreateCannonball(spawnPosition, direction, _tank.AttackDamage);
            _cannonballs.Add(cannonball);
            _currentShootCooldown = _shootCooldown;

            //reproducir sonido 3d
            _gameStateManager.SoundManager.PlaySound3D("cannon_fire", spawnPosition, _camera.ListenerPosition, _camera.ListenerForward);
        }
        _previousMouseState = currentMouseState;

        for (int i = _cannonballs.Count - 1; i >= 0; i--)
        {
            _cannonballs[i].Update(gameTime, _simulation);
            if (_cannonballs[i].IsDead)
            {
                _cannonballs.RemoveAt(i);
            }
        }

        _camera.Update(gameTime, _tank.Position, _tank.TurretRotationWorld); //A la camara ahora le paso la posicion de la torreta en vez de la base
        _gizmos.UpdateViewProjection(_camera.View, _camera.Projection);

        _hud.TankFuel = _tank.CurrentFuel;
        _hud.TankPosition = _tank.Position;
        _hud.CannonCurrentCooldown = _currentShootCooldown;
        _hud.CannonMaxCooldown = GameConfig.Tank.Cooldown;
        _hud.Update(gameTime);

        if (_tank.IsDead) _gameStateManager.ForceState(GameState.GameOver);
        if (EnemiesKilled == GameConfig.Enemies.EnemiesCount) _gameStateManager.ForceState(GameState.Win);

        base.Update(gameTime);
    }

    public void ResetGame()
    {
        _cannonballs.Clear();
        
        _currentShootCooldown = 0f;
        EnemiesKilled = 0;

        var tankModel = Content.Load<Model>(ContentFolder3D + getTankPath());
        var tankTexture = Content.Load<Texture2D>(ContentFolderTextures + "paleta_256x512");
        var textureEffect = Content.Load<Effect>(ContentFolderEffects + "BasicShaderTexture");

        Vector3 spawnPos = Vector3.Zero;
        float terrainY = _terrain.GetHeight(spawnPos.X, spawnPos.Z);

        _simulation.Bodies.Remove(_tankHandle);
        _tank = new TankPlayer(SelectedPlayerTank);
        _tank.Position = new Vector3(spawnPos.X, terrainY + GameConfig.Tank.SpawnZMargin, spawnPos.Z);
        _tank.Load(tankModel, tankTexture, textureEffect, _simulation);
        _tankHandle = _tank.TankHandler;

        _enemiesManager.Reset(_simulation);
        _dinamicsManager.ResetDynamics(_simulation);
        _barrelsManager.Reset(_simulation);

        _camera = new TankFollowCamera(GraphicsDevice.Viewport.AspectRatio, _tank.Position);
    }

    public Cannonball CreateCannonball(Vector3 spawnPosition, Vector3 direction, float damage)
    {
        return new Cannonball(_cannonballModel, damage, _effect, spawnPosition, direction, _simulation);
    }

    protected override void Draw(GameTime gameTime)
    {
        // Aca deberiamos poner toda la logica de renderizado del juego.
        var totalTime = (float)gameTime.TotalGameTime.TotalSeconds;
        GraphicsDevice.Clear(Color.CornflowerBlue);

        if (_gameStateManager.CurrentState == GameState.Playing || _gameStateManager.CurrentState == GameState.Paused)
        {
            // El terreno, al dibujarse, vuelve a activar el Z-Buffer (setea el DepthStencilState en "default")
            _terrain.Draw(_camera.View, _camera.Projection, _camera.ListenerPosition);
            _tank.Draw(_camera.View, _camera.Projection);

            foreach (var cannonball in _cannonballs)
            {
                cannonball.Draw(_camera.View, _camera.Projection);
            }
            _housesManager.Draw(_camera.View, _camera.Projection, _gizmos, _simulation);
            _staticsManager.Draw(_camera.View, _camera.Projection, _gizmos, _simulation);
            _dinamicsManager.Draw(_camera.View, _camera.Projection, _gizmos, _simulation);
            _barrelsManager.Draw(_camera.View, _camera.Projection, _gizmos, _simulation);
            _enemiesManager.Draw(_camera.View, _camera.Projection, _gizmos, _simulation);
            // El HUD se debe dibujar a lo ultimo, ya que para esto se desactiva el Z-Buffer, lo que rompe con el dibujado de los demas modelos
            _hud.Draw();
            _gizmos.Draw();
        }

        //El manager dibuja encima (Menu, Pausa, GameOver)
        string reason = _tank.IsDead ? (_tank.CurrentFuel <= 0f ? "Sin combustible" : "Tanque destruido") : "";
        _gameStateManager.Draw(reason);
    }

    public void InitializePhysics()
    {
        _bufferPool = new BufferPool();
        _simulation =
            Simulation.Create(_bufferPool,
                new NarrowPhaseCallbacks(),
                new PoseIntegratorCallbacks(new System.Numerics.Vector3(0, -9.8f, 0)),
                new SolveDescription(8, 1));
    }

    protected override void UnloadContent()
    {
        // Libero los recursos.
        Content.Unload();
        _simulation?.Dispose();
        _simulation = null;
        _bufferPool.Clear();
        _terrain?.Dispose();
        _hud?.Dispose();
        _gameStateManager?.Dispose();
        base.UnloadContent();
    }
}