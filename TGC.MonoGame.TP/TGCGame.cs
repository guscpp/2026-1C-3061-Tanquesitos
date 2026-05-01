using System;
using System.Transactions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using TGC.MonoGame.TP.Cameras;
using TGC.MonoGame.TP.Models;
using TGC.MonoGame.TP.Viewer;
using TGC.MonoGame.TP.Viewer.Gizmos;

namespace TGC.MonoGame.TP;

/// <summary>
///     Esta es la clase principal del juego.
///     Inicialmente puede ser renombrado o copiado para hacer mas ejemplos chicos, en el caso de copiar para que se
///     ejecute el nuevo ejemplo deben cambiar la clase que ejecuta Program <see cref="Program.Main()" /> linea 10.
/// </summary>
public class TGCGame : Game
{
    public const string ContentFolder3D = "Models/";
    public const string ContentFolderEffects = "Effects/";
    public const string ContentFolderMusic = "Music/";
    public const string ContentFolderSounds = "Sounds/";
    public const string ContentFolderSpriteFonts = "SpriteFonts/";
    public const string ContentFolderTextures = "Textures/";
    
    private readonly GraphicsDeviceManager _graphics;

    private Effect _effect;
    private Model _model;
    private Matrix _projection;
    private float _rotation;
    private SpriteBatch _spriteBatch;
    private Matrix _view;
    private Matrix _world;

    private SoundEffect _klaxonSound;
    private SoundEffect _hornSound;
    private KeyboardState _lastKeyboardState;
    private readonly Random _random = new();

    private Tank _tank;
    private TankFollowCamera _camera;
    private Terrain _terrain;
    private Hud _hud;

    public bool GodModeEnabled { get; set; } = true;    // provisional
    public Gizmos Gizmos { get; }

    /// <summary>
    ///     Constructor del juego.
    /// </summary>
    public TGCGame()
    {
        // Maneja la configuracion y la administracion del dispositivo grafico.
        _graphics = new GraphicsDeviceManager(this);

        _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width - 100;
        _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height - 100;

        // Para que el juego sea pantalla completa se puede usar Graphics IsFullScreen.
        // Carpeta raiz donde va a estar toda la Media.
        Content.RootDirectory = "Content";
        // Hace que el mouse sea visible.
        IsMouseVisible = true;

        //Gizmos 
        Gizmos = new Gizmos();
    }

    /// <summary>
    ///     Se llama una sola vez, al principio cuando se ejecuta el ejemplo.
    ///     Escribir aqui el codigo de inicializacion: el procesamiento que podemos pre calcular para nuestro juego.
    /// </summary>
    protected override void Initialize()
    {
        // La logica de inicializacion que no depende del contenido se recomienda poner en este metodo.

        // Apago el backface culling.
        // Esto se hace por un problema en el diseno del modelo del logo de la materia.
        // Una vez que empiecen su juego, esto no es mas necesario y lo pueden sacar.
        var rasterizerState = new RasterizerState();
        rasterizerState.CullMode = CullMode.None;
        GraphicsDevice.RasterizerState = rasterizerState;
        // Seria hasta aca.

        base.Initialize();

        // Configuramos nuestras matrices de la escena.
        _world = Matrix.Identity;
        _view = Matrix.CreateLookAt(Vector3.UnitZ * 150, Vector3.Zero, Vector3.Up);
        _projection =
            Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio, 1, 250);

        base.Initialize();
    }

    /// <summary>
    ///     Se llama una sola vez, al principio cuando se ejecuta el ejemplo, despues de Initialize.
    ///     Escribir aqui el codigo de inicializacion: cargar modelos, texturas, estructuras de optimizacion, el procesamiento
    ///     que podemos pre calcular para nuestro juego.
    /// </summary>
    protected override void LoadContent()
    {
        // Aca es donde deberiamos cargar todos los contenido necesarios antes de iniciar el juego.
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _tank = new Tank();

        //Cargad de modelo
        var tankModel = Content.Load<Model>(ContentFolder3D + "tanques/tank");
        //Carga de texturas
        var tankTexture = Content.Load<Texture2D>(ContentFolderTextures + "paleta");

        _tank.Load(tankModel, tankTexture);

        _camera = new TankFollowCamera(GraphicsDevice.Viewport.AspectRatio, _tank.Position);

        _terrain = new Terrain(GraphicsDevice);
        _terrain.LoadContent(Content);

        _hud = new Hud();
        _hud.LoadContent(Content, GraphicsDevice);

        _klaxonSound = Content.Load<SoundEffect>(ContentFolderSounds + "klaxon");
        _hornSound = Content.Load<SoundEffect>(ContentFolderSounds + "horn");

        Gizmos.LoadContent(GraphicsDevice, Content);

        base.LoadContent();
    }

    /// <summary>
    ///     Se llama en cada frame.
    ///     Se debe escribir toda la logica de computo del modelo, asi como tambien verificar entradas del usuario y reacciones
    ///     ante ellas.
    /// </summary>
    protected override void Update(GameTime gameTime)
    {
        // Aca deberiamos poner toda la logica de actualizacion del juego.

        // Capturar Input teclado
        var kb = Keyboard.GetState();
        if (kb.IsKeyDown(Keys.Escape)) Exit();
        if (kb.IsKeyDown(Keys.Space) && _lastKeyboardState.IsKeyUp(Keys.Space))
        {
            int chance = _random.Next(0, 100);
            if (chance < 90) _klaxonSound.Play();
            else _hornSound.Play();
        }

        _lastKeyboardState = kb;

        // Basado en el tiempo que paso se va generando una rotacion.
        //_rotation += Convert.ToSingle(gameTime.ElapsedGameTime.TotalSeconds);
        //_world = Matrix.CreateRotationY(_rotation);

        _tank.Update(gameTime, kb);

        // Actualiza la posicionY del tanque según el terreno
        float terrainHeight = _terrain.GetHeight(_tank.Position); //Altura correcta que debe usar
        _tank.SetHeight(terrainHeight);
        //Actualmente el tanque hace esto, primero dice donde quiere moverse (tanl.Update) y luego nosotros le corregimos la posicion segun el mapa

        _camera.Update(gameTime, _tank.Position, _tank.RotationY);

        if(!GodModeEnabled)
            Gizmos.Enabled = false;

        Gizmos.UpdateViewProjection(_camera.View, _camera.Projection);

        base.Update(gameTime);
    }

    /// <summary>
    ///     Se llama cada vez que hay que refrescar la pantalla.
    ///     Escribir aqui el codigo referido al renderizado.
    /// </summary>
    protected override void Draw(GameTime gameTime)
    {
        // Aca deberiamos poner toda la logia de renderizado del juego.
        GraphicsDevice.Clear(Color.Goldenrod);

        _terrain.Draw(_camera.View, _camera.Projection);
        _tank.Draw(_camera.View, _camera.Projection);
        _hud.Draw();

        // dibujo los gizmos!
        Gizmos.SetColor(Color.Red);
        // quiero hacerle modificaciones al cilindro
        // Gizmos.DrawCylinder(_tank.Position, Matrix.Identity, new Vector3(500f, 500f, 500f));
        Gizmos.DrawCube(_tank.Position, new Vector3(700f), Color.Red);
        Gizmos.Draw();

        base.Draw(gameTime);
    }

    /// <summary>
    ///     Libero los recursos que se cargaron en el juego.
    /// </summary>
    protected override void UnloadContent()
    {
        // Libero los recursos.
        Content.Unload();

        _terrain?.Dispose();
        _hud?.Dispose();

        base.UnloadContent();
    }
}