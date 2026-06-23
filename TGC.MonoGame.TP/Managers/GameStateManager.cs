using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System;
using TGC.MonoGame.TP.Models;

namespace TGC.MonoGame.TP.Managers;

public class GameStateManager
{
    public const string ContentFolder3D = "Models/";
    public const string ContentFolderEffects = "Effects/";
    public const string ContentFolderMusic = "Music/";
    public const string ContentFolderSounds = "Sounds/";
    public const string ContentFolderSpriteFonts = "SpriteFonts/";
    public const string ContentFolderTextures = "Textures/";
    public GameState CurrentState { get; private set; } = GameState.TankSelection;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly ContentManager _content;
    private readonly SpriteBatch _spriteBatch;
    private readonly SpriteFont _fontArial;
    private readonly SpriteFont _fontConsolas;
    private readonly Texture2D _whitePixel;
    private Texture2D _menuBackground;
    private Texture2D _playButtonTexture;
    private Rectangle _playButtonRectangle;
    private bool _isPlayButtonHovered;
    private Texture2D _settingsButtonTexture;
    private Rectangle _settingsButtonRectangle;
    private bool _isSettingsButtonHovered;
    private Texture2D _exitButtonTexture;
    private Rectangle _exitButtonRectangle;
    private bool _isExitButtonHovered;
    private Texture2D _backButtonTexture;
    private Rectangle _backButtonRectangle;
    private bool _isbackButtonHovered;
    private Texture2D _leftArrowButtonTexture;
    private Rectangle _leftArrowButtonRectangle;
    private bool _isLeftArrowButtonHovered;
    private Texture2D _rightArrowButtonTexture;
    private Rectangle _rightArrowButtonRectangle;
    private bool _isrightArrowButtonHovered;
    private Texture2D _plusButtonTexture;
    //private Rectangle _plusButtonRectangle;
    //private bool _isPlusButtonHovered;
    private Texture2D _minusButtonTexture;
    //private Rectangle _minusButtonRectangle;
    //private bool _isMinusButtonHovered;
    private Texture2D _checkMarkButtonTexture;
    private Rectangle _checkMarkButtonRectangle;
    private bool _isCheckMarkButtonHovered;
    private Texture2D _xButtonTexture;
    //private Rectangle _xButtonRectangle;
    //private bool _isXButtonHovered;

    private const float HoverScale = 1.08f;
    private float _introTimer = 0f;
    private float _menuInputLockTime = 0f;

    //SoundManager
    private readonly SoundManager _soundManager;
    private bool _menuMusicStarted = false;

    // 3D Menu Variables
    private Model _currentMenuTankModel;
    private Texture2D _menuTankTexture;
    private Texture2D _menuTracksTexture;
    private Effect _menuTankEffect;
    private float _menuTankRotation = 0f;
    private float _menuTankRotationSpeed = 0.015f;

    //En algun momento consideramos usar 3 modelos distintos para los 3 tipos
    private readonly string[] _menuTankModelPaths = {
        "Models/tanques/tank v5", // Scout
        "Models/tanques/tank v5", // Medium
        "Models/tanques/tank v5"  // Heavy
    };

    private int _selectedIndex = 0; //Preselecciona Iniciar en el menu
    private int _lastSelectedIndex = -1;
    private MouseState _lastMouseState;
    private ContentManager _menuContent; //evita cache compartido
    private string _cachedSpecsText = string.Empty;
    private int _specsCachedIndex = -1;

    public GameStateManager(GraphicsDevice graphicsDevice, ContentManager content, SoundManager soundManager)
    {
        _graphicsDevice = graphicsDevice;
        _content = content;
        _soundManager = soundManager;
        _spriteBatch = new SpriteBatch(graphicsDevice);
        // Coincide con la ruta compilada en Content.mgcb
        _fontArial = content.Load<SpriteFont>("SpriteFonts/ArialFont");
        _fontConsolas = content.Load<SpriteFont>("SpriteFonts/ConsolasFont");
        _menuBackground = content.Load<Texture2D>("Textures/ConceptArt6");
        
        _playButtonTexture = content.Load<Texture2D>("Textures/Buttons/Play_Button");
        _settingsButtonTexture = content.Load<Texture2D>("Textures/Buttons/Settings_Button");
        _exitButtonTexture = content.Load<Texture2D>("Textures/Buttons/Exit_Button");
        _backButtonTexture = content.Load<Texture2D>("Textures/Buttons/Back_Button");
        _leftArrowButtonTexture = content.Load<Texture2D>("Textures/Buttons/Left_Arrow_Button");
        _rightArrowButtonTexture = content.Load<Texture2D>("Textures/Buttons/Right_Arrow_Button");
        _plusButtonTexture = content.Load<Texture2D>("Textures/Buttons/Plus_Button");
        _minusButtonTexture = content.Load<Texture2D>("Textures/Buttons/Minus_Button");
        _checkMarkButtonTexture = content.Load<Texture2D>("Textures/Buttons/Check_Mark_Button");
        _xButtonTexture = content.Load<Texture2D>("Textures/Buttons/X_Button");

        //Textura 1x1 para overlays
        _whitePixel = new Texture2D(graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });

        LoadMenu3DAssets();

        CurrentState = GameState.Intro;
        _introTimer = 0f;
    }

    private void LoadMenu3DAssets()
    {
        try
        {
            //De este modo el modelo del menu es distinto al del juego
            _menuContent = new ContentManager(_content.ServiceProvider, "Content");

            //_menuTankEffect = _content.Load<Effect>("Effects/BasicShaderTexture");
            _menuTankEffect = _menuContent.Load<Effect>("Effects/BlinnPhong");
            _menuTankTexture = _menuContent.Load<Texture2D>(ContentFolderTextures + "paleta_256x512");
            _menuTracksTexture = _menuContent.Load<Texture2D>(ContentFolderTextures + GameConfig.Tank.TankTracksTexture);
            _currentMenuTankModel = _menuContent.Load<Model>(ContentFolder3D + GameConfig.Tank.TankModelPath);

            UpdateMenuTankModel(0);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading 3D menu assets: {ex.Message}");
        }
    }

    private void UpdateMenuTankModel(int tankIndex)
    {
        if (tankIndex >= 0 && tankIndex < _menuTankModelPaths.Length)
        {
            try
            {
                _currentMenuTankModel = _menuContent.Load<Model>("Models/" + GameConfig.Tank.TankModelPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading model {tankIndex}: {ex.Message}");
                _currentMenuTankModel = null;
            }
        }
    }

    public void HandleMenuState()
    {
        if (_selectedIndex != _lastSelectedIndex)
        {
            UpdateMenuTankModel(_selectedIndex);
            _lastSelectedIndex = _selectedIndex;
        }
    }

    public void Update(GameTime gameTime, KeyboardState kb, KeyboardState lastKb)
    {
        HandleMusic();
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (CurrentState == GameState.Intro)
        {
            _introTimer += dt;

            bool keyPressed = kb.GetPressedKeys().Length > 0 && lastKb.GetPressedKeys().Length == 0;

            MouseState currentMouse = Mouse.GetState();
            bool mouseClicked = currentMouse.LeftButton == ButtonState.Pressed && _lastMouseState.LeftButton == ButtonState.Released;
            _lastMouseState = currentMouse;

            if (keyPressed || mouseClicked)
            {
                CurrentState = GameState.MainMenu;
                _introTimer = 0f;
                _menuInputLockTime = 0.3f; //impide input bleed en GameState.Menu
            }
        }

        if (CurrentState == GameState.MainMenu)
        {
            HandleMainMenuInput(kb, lastKb);
            return;
        }

        _menuTankRotation += _menuTankRotationSpeed;

        if (CurrentState == GameState.TankSelection)
        {
            if (_menuInputLockTime > 0f)
            {
                _menuInputLockTime -= dt;
                return;
            }

            HandleTankSelectionInput();
            HandleMenuState();
            return;
        }

        switch (CurrentState)
        {
            case GameState.Playing:
                if (kb.IsKeyDown(Keys.P) && lastKb.IsKeyUp(Keys.P))
                    CurrentState = GameState.Paused;
                break;
            case GameState.Paused:
                // SoundManager.StopMusic();
                if (kb.IsKeyDown(Keys.P) && lastKb.IsKeyUp(Keys.P))
                    CurrentState = GameState.Playing;
                break;
            case GameState.GameOver:
            case GameState.Win: // por ahora, mismo comportamiento que cuando se pierde
                SoundManager.StopMusic();
                if (kb.IsKeyDown(Keys.Enter) && lastKb.IsKeyUp(Keys.Enter))
                {
                    CurrentState = GameState.MainMenu;
                    TGCGame.Instance.IsMouseVisible = true;
                    _selectedIndex = 0;
                    _menuMusicStarted = false;
                    _currentMenuTankModel = null;
                    UpdateMenuTankModel(0);
                }
                break;
        }
    }

    private void HandleMainMenuInput(KeyboardState kb, KeyboardState lastKb)
    {
        UpdateMainMenuLayout(_graphicsDevice.Viewport);
        
        MouseState currentMouse = Mouse.GetState();
        Point mousePosition = new(currentMouse.X, currentMouse.Y);

        _isPlayButtonHovered = _playButtonRectangle.Contains(mousePosition);
        _isSettingsButtonHovered = _settingsButtonRectangle.Contains(mousePosition);
        _isExitButtonHovered = _exitButtonRectangle.Contains(mousePosition);
        
        bool leftClick = currentMouse.LeftButton == ButtonState.Pressed && _lastMouseState.LeftButton == ButtonState.Released;

        if (leftClick)
        {
            if (_isPlayButtonHovered)
            {
                CurrentState = GameState.TankSelection;
                _soundManager.PlaySound("colision_casa");
                _menuInputLockTime = 0.3f;
            }
            else if (_isSettingsButtonHovered)
            {
                // Pantalla de configuracion
            }
            else if (_isExitButtonHovered)
            {
                Environment.Exit(0);
            }
        }

        _lastMouseState = currentMouse;
    }

    private void UpdateMainMenuLayout(Viewport vp)
    {
        // TO DO: Agregar condicional para que se ejecute solo cuando cambia el viewport
        int buttonSpacing = (int)(20 * vp.AspectRatio);
        float buttonScale = vp.AspectRatio / 4f;

        int buttonWidth = (int)(_playButtonTexture.Width * buttonScale);
        int buttonHeight = (int)(_playButtonTexture.Height * buttonScale);

        int totalHeight = buttonHeight * 3 + buttonSpacing * 2;
        int heightOffset = (int)(vp.AspectRatio * 20);

        int startY = (vp.Height - totalHeight) / 2 + heightOffset;
        int centerX = vp.Width / 2 - buttonWidth / 2;

        _playButtonRectangle = new Rectangle(centerX, startY, buttonWidth, buttonHeight);

        _settingsButtonRectangle = new Rectangle(centerX, startY + buttonHeight + buttonSpacing, buttonWidth, buttonHeight);

        _exitButtonRectangle = new Rectangle(centerX, startY + (buttonHeight + buttonSpacing) * 2, buttonWidth, buttonHeight);
    }

    private void HandleTankSelectionInput()
    {
        UpdateTankSelectionLayout(_graphicsDevice.Viewport);

        MouseState currentMouse = Mouse.GetState();
        Point mousePosition = new(currentMouse.X, currentMouse.Y);

        _isbackButtonHovered = _backButtonRectangle.Contains(mousePosition);
        _isLeftArrowButtonHovered = _leftArrowButtonRectangle.Contains(mousePosition);

        _isCheckMarkButtonHovered = _checkMarkButtonRectangle.Contains(mousePosition);

        _isrightArrowButtonHovered = _rightArrowButtonRectangle.Contains(mousePosition);

        bool leftClick = currentMouse.LeftButton == ButtonState.Pressed && _lastMouseState.LeftButton == ButtonState.Released;

        if (leftClick)
        {
            if (_isLeftArrowButtonHovered)
            {
                _selectedIndex = (_selectedIndex - 1 + 3) % 3;  // "%3" porque solo existen 3 opciones
                _soundManager.PlaySound("enemy_cannon_fire");
            }
            else if (_isrightArrowButtonHovered)
            {
                _selectedIndex = (_selectedIndex + 1) % 3;
                _soundManager.PlaySound("enemy_cannon_fire");
            }
            else if (_isCheckMarkButtonHovered)
            {
                ApplySelection();
            }
            else if (_isbackButtonHovered)
            {
                CurrentState = GameState.MainMenu;
                _menuInputLockTime = 0.3f;
            }
        }

        _lastMouseState = currentMouse;
    }

    private void UpdateTankSelectionLayout(Viewport vp)
    {
        float buttonScale = vp.AspectRatio / 5.5f;

        int buttonWidth = (int)(_backButtonTexture.Width * buttonScale);
        int buttonHeight = (int)(_backButtonTexture.Height * buttonScale);

        int marginX = (int)(vp.Width * 0.01f);

        _backButtonRectangle = new Rectangle(marginX, vp.Height - buttonHeight, buttonWidth, buttonHeight);

        
        float selectorButtonScale = vp.AspectRatio / 8f;

        int selectorButtonWidth = (int)(_checkMarkButtonTexture.Width * selectorButtonScale);
        int selectorButtonHeight = (int)(_checkMarkButtonTexture.Height * selectorButtonScale);

        float specsX = vp.Width * 0.1f;
        float specsY = vp.Height * 0.2f;

        // Aproximadamente debajo del bloque de estadísticas
        int buttonsY = (int)(specsY + 240);
        int spacing = 20;

        _leftArrowButtonRectangle = new Rectangle((int)specsX, buttonsY, selectorButtonWidth, selectorButtonHeight);
        _checkMarkButtonRectangle = new Rectangle(_leftArrowButtonRectangle.Right + spacing, buttonsY, selectorButtonWidth, selectorButtonHeight);
        _rightArrowButtonRectangle = new Rectangle(_checkMarkButtonRectangle.Right + spacing, buttonsY, selectorButtonWidth, selectorButtonHeight);
    }

    private Rectangle ScaleRectangle(Rectangle rectangle, float scale)
    {
        int newWidth = (int)(rectangle.Width * scale);
        int newHeight = (int)(rectangle.Height * scale);

        int newX = rectangle.Center.X - newWidth / 2;
        int newY = rectangle.Center.Y - newHeight / 2;

        return new Rectangle(newX, newY, newWidth, newHeight);
    }

    private void ApplySelection()
    {
        switch (_selectedIndex)
        {
            case 0:
                TGCGame.SelectedPlayerTank = GameConfig.TankClass.Scout;
                break;

            case 1:
                TGCGame.SelectedPlayerTank = GameConfig.TankClass.Medium;
                break;

            case 2:
                TGCGame.SelectedPlayerTank = GameConfig.TankClass.Heavy;
                break;
        }

        TGCGame.Instance.ResetGame();
        CurrentState = GameState.Playing;
    }

    public void Draw(string extraInfo = "")
    {
        if (CurrentState == GameState.Playing) return; // En Playing no dibuja nada extra

        var vp = _graphicsDevice.Viewport;
        Vector2 center = new Vector2(vp.Width / 2f, vp.Height / 2f);

        if (CurrentState == GameState.Intro)
        {
            _graphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            //Dibujar la textura estirada a toda la pantalla
            _spriteBatch.Draw(_menuBackground, new Rectangle(0, 0, vp.Width, vp.Height), Color.White);

            //Texto de "Presiona cualquier tecla o clic para continuar"
            if (_introTimer > 1.0f)
            {
                //Efecto de "pulso"
                float pulse = (MathF.Sin(_introTimer * 4f) + 1f) / 2f; // Oscila suavemente entre 0 y 1
                float alpha = MathHelper.Clamp((_introTimer - 1.0f) * 2f, 0f, 1f); // Fade-in de 0.5 segundos
                float scale = 1.3f + (pulse * 0.1f); // Escala entre 1.3x y 1.4x

                string hint = "Presiona cualquier tecla o clic para continuar";

                //Usar ConsolasFont
                Vector2 hintSize = _fontConsolas.MeasureString(hint) * scale;
                Vector2 hintPos = new Vector2(vp.Width / 2f - hintSize.X / 2f, vp.Height - 120f);

                //Fondo semitransparente detras del texto
                Rectangle bgRect = new Rectangle((int)hintPos.X - 30, (int)hintPos.Y - 15, (int)hintSize.X + 60, (int)hintSize.Y + 30);
                _spriteBatch.Draw(_whitePixel, bgRect, new Color(0, 0, 0, (int)(alpha * 200)));

                //Sombra gruesa y desplazada
                _spriteBatch.DrawString(_fontConsolas, hint, hintPos + new Vector2(4, 4), new Color(0, 0, 0, (int)(alpha * 255)), 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

                //Texto principal pulsante
                Color textColor = Color.Lerp(Color.White, Color.Gold, pulse);
                _spriteBatch.DrawString(_fontConsolas, hint, hintPos, new Color(textColor.R, textColor.G, textColor.B, (int)(alpha * 255)), 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }

            _spriteBatch.End();
        }

        if (CurrentState == GameState.MainMenu)
        {
            _graphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            _spriteBatch.Draw(_menuBackground, new Rectangle(0, 0, vp.Width, vp.Height), Color.White);
            DrawMainMenuButtons();
            
            _spriteBatch.End();
        }

        if (CurrentState == GameState.TankSelection)
        {
            _graphicsDevice.Clear(Color.DarkSlateGray);

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            // Fondo del menú
            _spriteBatch.Draw(_menuBackground, new Rectangle(0, 0, vp.Width, vp.Height), Color.White);

            // Overlay gris oscuro semitransparente
            _spriteBatch.Draw(_whitePixel,new Rectangle(0, 0, vp.Width, vp.Height),new Color(30, 30, 30, 180));

            _spriteBatch.End();
            
            if (_selectedIndex < 3 && _currentMenuTankModel != null)
            {

                // Camera and scale adjustments
                Matrix world = Matrix.CreateScale(4f) *
                               Matrix.CreateRotationX(MathHelper.ToRadians(-90f)) *
                               Matrix.CreateRotationY(_menuTankRotation);

                Matrix view = Matrix.CreateLookAt(new Vector3(10f, 13f, 18f), new Vector3(-8, 1, 0), Vector3.Up);
                Matrix projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, vp.AspectRatio, 0.1f, 100f);

                // Fix culling and depth issues
                _graphicsDevice.RasterizerState = RasterizerState.CullNone;
                _graphicsDevice.DepthStencilState = DepthStencilState.Default;
                _graphicsDevice.BlendState = BlendState.Opaque;

                Draw3DTank(world, view, projection);
            }

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            if (_selectedIndex < 3)
                DrawTankSpecs(vp);
            
            DrawTankSelectionButtons();

            _spriteBatch.End();
        }
        else if (CurrentState == GameState.Paused || CurrentState == GameState.GameOver || CurrentState == GameState.Win)
        {
            //_graphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            if (CurrentState == GameState.Paused)
            {
                _spriteBatch.Draw(_whitePixel, new Rectangle(0, 0, vp.Width, vp.Height), Color.Black * 0.66f);
                DrawCenteredText("PAUSA\nPresiona P para continuar", center);
            }
            else if (CurrentState == GameState.GameOver)
            {
                _spriteBatch.Draw(_whitePixel, new Rectangle(0, 0, vp.Width, vp.Height), Color.Black * 0.66f);
                DrawCenteredText($"GAME OVER\n{extraInfo}\nPresiona ENTER para volver al menu", center);
            }
            else if (CurrentState == GameState.Win)
            {
                _spriteBatch.Draw(_whitePixel, new Rectangle(0, 0, vp.Width, vp.Height), Color.Black * 0.66f);
                DrawCenteredText($"! GANASTE !\n{extraInfo}\nPresiona ENTER para volver al menu", center);
            }

            _spriteBatch.End();
        }
    }

    private void Draw3DTank(Matrix world, Matrix view, Matrix projection)
    {
        if (_currentMenuTankModel == null || _menuTankEffect == null || _menuTankTexture == null)
        {
            return;
        }

        Microsoft.Xna.Framework.Vector3 whiteColor = Microsoft.Xna.Framework.Vector3.One;
        Microsoft.Xna.Framework.Vector3 menuTankColor = Microsoft.Xna.Framework.Color.White.ToVector3();
        
        if (_selectedIndex == 0) // Scout
            menuTankColor = new Microsoft.Xna.Framework.Color(50, 205, 50).ToVector3();   // Verde
        else if (_selectedIndex == 1) // Medium
            menuTankColor = new Microsoft.Xna.Framework.Color(255, 215, 0).ToVector3();   // Amarillo
        else if (_selectedIndex == 2) // Heavy
            menuTankColor = new Microsoft.Xna.Framework.Color(178, 34, 34).ToVector3();   // Rojo

        _menuTankEffect.Parameters["LightDirection"].SetValue(new Microsoft.Xna.Framework.Vector3(0.5f, 1.0f, 0.3f));
        _menuTankEffect.Parameters["LightColor"].SetValue(Vector3.One);
        _menuTankEffect.Parameters["AmbientColor"].SetValue(new Vector3(0.2f, 0.2f, 0.2f));
        _menuTankEffect.Parameters["EyePosition"].SetValue(new Vector3(10f, 13f, 18f));
        _menuTankEffect.Parameters["Shininess"].SetValue(32f);

        foreach (var mesh in _currentMenuTankModel.Meshes)
        {
            Texture2D activeTexture = _menuTankTexture;
            if (mesh.Name.Contains("Cadena")) activeTexture = _menuTracksTexture;
            else activeTexture = _menuTankTexture;
            _menuTankEffect.Parameters["ModelTexture"].SetValue(activeTexture);

            Microsoft.Xna.Framework.Vector3 colorToApply = whiteColor;

            if (mesh.Name.Contains("Cabeza") || mesh.Name.Contains("Anillo") ||
                mesh.Name.Contains("Proteccion_d") || mesh.Name.Contains("Proteccion_i") ||
                mesh.Name.Contains("Cuerpo") || mesh.Name.Contains("Cubre"))
                colorToApply = menuTankColor;

            foreach (var part in mesh.MeshParts)
            {
                part.Effect = _menuTankEffect;
                _menuTankEffect.Parameters["World"].SetValue(world);
                _menuTankEffect.Parameters["View"].SetValue(view);
                _menuTankEffect.Parameters["Projection"].SetValue(projection);
                _menuTankEffect.Parameters["ModelTexture"].SetValue(activeTexture);
                _menuTankEffect.Parameters["DiffuseColor"].SetValue(colorToApply);

                _menuTankEffect.Parameters["HasImpact"]?.SetValue(0);
                _menuTankEffect.Parameters["ImpactPointWorld"]?.SetValue(Vector3.Zero);
                _menuTankEffect.Parameters["ImpactRadius"]?.SetValue(GameConfig.Tank.ImpactRadius);
                _menuTankEffect.Parameters["ImpactDepth"]?.SetValue(GameConfig.Tank.ImpactDepth);
                _menuTankEffect.Parameters["IsDeformable"]?.SetValue(0);
            }
            mesh.Draw();
        }
    }

    private void DrawTankSpecs(Viewport vp)
    {
        if (_selectedIndex != _specsCachedIndex)
        {
            string className;
            float playerHealth, maxSpeed, motorForce, turnSpeed, attackDamage;

            if (_selectedIndex == 0)
            {
                className = "SCOUT";
                playerHealth = GameConfig.TankClasses.Scout.PlayerHealth;
                maxSpeed = GameConfig.TankClasses.Scout.MaxSpeed;
                motorForce = GameConfig.TankClasses.Scout.MotorForce;
                turnSpeed = GameConfig.TankClasses.Scout.TurnSpeed;
                attackDamage = GameConfig.TankClasses.Scout.AttackDamage;
            }
            else if (_selectedIndex == 1)
            {
                className = "MEDIUM";
                playerHealth = GameConfig.TankClasses.Medium.PlayerHealth;
                maxSpeed = GameConfig.TankClasses.Medium.MaxSpeed;
                motorForce = GameConfig.TankClasses.Medium.MotorForce;
                turnSpeed = GameConfig.TankClasses.Medium.TurnSpeed;
                attackDamage = GameConfig.TankClasses.Medium.AttackDamage;
            }
            else
            {
                className = "HEAVY";
                playerHealth = GameConfig.TankClasses.Heavy.PlayerHealth;
                maxSpeed = GameConfig.TankClasses.Heavy.MaxSpeed;
                motorForce = GameConfig.TankClasses.Heavy.MotorForce;
                turnSpeed = GameConfig.TankClasses.Heavy.TurnSpeed;
                attackDamage = GameConfig.TankClasses.Heavy.AttackDamage;
            }

        // Se genera la cadena de texto una sola vez por cada cambio de tanque
        _cachedSpecsText = $"CLASE: {className}\n\n" +
                        $"HP Jugador:   {playerHealth}\n" +
                        $"Velocidad:    {maxSpeed} m/s\n" +
                        $"Fuerza Motor: {motorForce}\n" +
                        $"Vel. Giro:    {turnSpeed}\n" +
                        $"Danio Ataque: {attackDamage}";

        // Actualizamos el indice de control para recordar que este tanque ya esta procesado
        _specsCachedIndex = _selectedIndex;
        }

        // Posicionamiento de la caja de especificaciones en pantalla
        float padX = vp.Width * 0.14f;
        float padY = vp.Height * 0.2f;
        Vector2 specsPos = new Vector2(padX, padY);

        // Dibujamos usando el string en caché (Costo de CPU y asignaciones de memoria = 0)
        _spriteBatch.DrawString(_fontConsolas, _cachedSpecsText, specsPos + Vector2.One, Color.Black);
        _spriteBatch.DrawString(_fontConsolas, _cachedSpecsText, specsPos, Color.White);
    }

    private void DrawMainMenuButtons()
    {
        Rectangle playRect = _isPlayButtonHovered ? ScaleRectangle(_playButtonRectangle, HoverScale) : _playButtonRectangle;
        Rectangle settingsRect = _isSettingsButtonHovered ? ScaleRectangle(_settingsButtonRectangle, HoverScale) : _settingsButtonRectangle;
        Rectangle exitRect = _isExitButtonHovered ? ScaleRectangle(_exitButtonRectangle, HoverScale) : _exitButtonRectangle;

        _spriteBatch.Draw(_playButtonTexture, playRect, _isPlayButtonHovered ? Color.PaleGoldenrod : Color.AntiqueWhite);
        _spriteBatch.Draw(_settingsButtonTexture, settingsRect, _isSettingsButtonHovered ? Color.PaleGoldenrod : Color.AntiqueWhite);
        _spriteBatch.Draw(_exitButtonTexture, exitRect, _isExitButtonHovered ? Color.PaleGoldenrod : Color.AntiqueWhite);
    }

    private void DrawTankSelectionButtons()
    {
        Rectangle backRect = _isbackButtonHovered ? ScaleRectangle(_backButtonRectangle, HoverScale) : _backButtonRectangle;
        Rectangle leftRect = _isLeftArrowButtonHovered ? ScaleRectangle(_leftArrowButtonRectangle, HoverScale) : _leftArrowButtonRectangle;
        Rectangle checkRect = _isCheckMarkButtonHovered ? ScaleRectangle(_checkMarkButtonRectangle, HoverScale) : _checkMarkButtonRectangle;
        Rectangle rightRect = _isrightArrowButtonHovered ? ScaleRectangle(_rightArrowButtonRectangle, HoverScale) : _rightArrowButtonRectangle;

        _spriteBatch.Draw(_backButtonTexture,backRect, _isbackButtonHovered ? Color.PaleGoldenrod : Color.AntiqueWhite);
        _spriteBatch.Draw(_leftArrowButtonTexture, leftRect,_isLeftArrowButtonHovered ? Color.PaleGoldenrod : Color.AntiqueWhite);
        _spriteBatch.Draw(_checkMarkButtonTexture, checkRect,_isCheckMarkButtonHovered ? Color.PaleGoldenrod : Color.AntiqueWhite);
        _spriteBatch.Draw(_rightArrowButtonTexture, rightRect,_isrightArrowButtonHovered ? Color.PaleGoldenrod : Color.AntiqueWhite);
    }

    private void DrawCenteredText(string text, Vector2 center)
    {
        var size = _fontArial.MeasureString(text);
        var pos = center - size / 2;
        _spriteBatch.DrawString(_fontArial, text, pos + new Vector2(2), Color.Black);
        _spriteBatch.DrawString(_fontArial, text, pos, Color.White);
    }

    public void ForceState(GameState state) => CurrentState = state;

    // Maneja la reproduccion de musica segun el estado del juego
    private void HandleMusic()
    {
        if (CurrentState == GameState.MainMenu || CurrentState == GameState.TankSelection)
        {
            // Reproducir musica del menu solo si no esta sonando
            if (!_menuMusicStarted && MediaPlayer.State != MediaState.Playing)
            {
                _soundManager.StopMusic();
                _soundManager.PlayMusic("Music/100-song18", true);
                _menuMusicStarted = true;
            }
        }
        else
        {
            // Detener musica del menu al salir de ese estado
            if (_menuMusicStarted && MediaPlayer.State == MediaState.Playing)
            {
                _soundManager.StopMusic();
                _soundManager.PlayMusic("Music/101-Juhani Junkala [Retro Game Music Pack] Level 1", true);
                _menuMusicStarted = false;
            }
        }
    }

    //exponerlo para reproducir efectos 3d
    public SoundManager SoundManager => _soundManager;

    public void Dispose()
    {
        _spriteBatch?.Dispose();
        _whitePixel?.Dispose();
    }
}