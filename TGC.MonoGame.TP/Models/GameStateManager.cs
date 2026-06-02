using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace TGC.MonoGame.TP.Models;

public class GameStateManager
{
    public GameState CurrentState { get; private set; } = GameState.Menu;

    private readonly SpriteBatch _spriteBatch;
    private readonly SpriteFont _font;
    private readonly Texture2D _whitePixel;
    private Texture2D _menuBackground;

    // Opciones de menu actualizadas para elegir el tipo de tanque
    private readonly string[] _menuOptions = {
        "Iniciar (Tanque Scout)",
        "Iniciar (Tanque Medio)",
        "Iniciar (Tanque Pesado)",
        "Salir"
    };
    private int _selectedIndex = 0; //Preselecciona Iniciar en el menu

    private MouseState _lastMouseState;

    public GameStateManager(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _spriteBatch = new SpriteBatch(graphicsDevice);
        // Coincide con la ruta compilada en Content.mgcb
        _font = content.Load<SpriteFont>("SpriteFonts/ArialFont");
        _menuBackground = content.Load<Texture2D>("Textures/ConceptArt6");

        //Textura 1x1 para overlays
        _whitePixel = new Texture2D(graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
    }

    public void Update(KeyboardState kb, KeyboardState lastKb)
    {
        //el menu maneja su propia logica, early return
        if (CurrentState == GameState.Menu)
        {
            HandleMenuInput(kb, lastKb);
            return;
        }

        switch (CurrentState)
        {
            case GameState.Playing:
                if (kb.IsKeyDown(Keys.P) && lastKb.IsKeyUp(Keys.P))
                    CurrentState = GameState.Paused;
                break;
            case GameState.Paused:
                if (kb.IsKeyDown(Keys.P) && lastKb.IsKeyUp(Keys.P))
                    CurrentState = GameState.Playing;
                break;
            case GameState.GameOver:
                if (kb.IsKeyDown(Keys.Enter) && lastKb.IsKeyUp(Keys.Enter))
                {
                    CurrentState = GameState.Menu;
                    _selectedIndex = 0;
                }
                break;
        }
    }

    private void HandleMenuInput(KeyboardState kb, KeyboardState lastKb)
    {
        // Teclado: flechas arriba/abajo
        if (kb.IsKeyDown(Keys.Down) && lastKb.IsKeyUp(Keys.Down))
            _selectedIndex = (_selectedIndex + 1) % _menuOptions.Length;
        else if (kb.IsKeyDown(Keys.Up) && lastKb.IsKeyUp(Keys.Up))
            _selectedIndex = (_selectedIndex - 1 + _menuOptions.Length) % _menuOptions.Length;

        // Teclado: enter
        if (kb.IsKeyDown(Keys.Enter) && lastKb.IsKeyUp(Keys.Enter))
            ApplySelection();

        // Mouse: hover y click
        MouseState currentMouse = Mouse.GetState();
        int hoveredIndex = GetOptionAtPosition(currentMouse.X, currentMouse.Y);
        if (hoveredIndex != -1)
        {
            _selectedIndex = hoveredIndex; // Feedback de hover
            if (currentMouse.LeftButton == ButtonState.Pressed && _lastMouseState.LeftButton == ButtonState.Released)
                ApplySelection(); // Click selecciona esa opcion
        }
        _lastMouseState = currentMouse;
    }

    /// <summary>
    /// Determina si el cursor del mouse está sobre alguna opción del menú.
    /// Devuelve el índice de la opción bajo el cursor, o -1 si no está sobre ninguna.
    /// </summary>
    private int GetOptionAtPosition(int mouseX, int mouseY)
    {
        // 1. Obtener las dimensiones actuales de la ventana/pantalla
        var vp = _spriteBatch.GraphicsDevice.Viewport;

        // 2. Calcular el punto central exacto de la pantalla
        Vector2 center = new Vector2(vp.Width / 2f, vp.Height / 2f);

        // 3. Calcular la coordenada Y inicial para que el bloque completo de opciones quede centrado verticalmente.
        //    Se toma la mitad del alto total estimado del texto y se resta del centro.
        float startY = center.Y - (_font.LineSpacing * _menuOptions.Length / 2f);

        // 4. Espacio vertical entre cada línea de texto. 
        //    IMPORTANTE: Este valor debe ser idéntico al usado en DrawMenu para que el "hitbox" coincida con lo que se ve.
        float spacing = 20f;

        // 5. Recorrer cada opción del menú para verificar si el mouse está dentro de su área visual
        for (int i = 0; i < _menuOptions.Length; i++)
        {
            // Medir cuánto ocupa en píxeles el texto de esta opción (ancho y alto)
            var size = _font.MeasureString(_menuOptions[i]);

            // Calcular la posición superior izquierda donde se dibujaría esta opción:
            // - Eje X: centrado horizontalmente (mitad de pantalla menos la mitad del ancho del texto)
            // - Eje Y: posicion inicial + (indice * altura de linea + espacio extra entre opciones)
            var pos = new Vector2(center.X - size.X / 2f, startY + i * (_font.LineSpacing + spacing));

            // Crear un rectángulo invisible que actúa como "zona de clic" (hitbox) del texto
            var rect = new Rectangle((int)pos.X, (int)pos.Y, (int)size.X, (int)size.Y);

            // Verificar si las coordenadas del mouse están dentro de este rectángulo
            if (rect.Contains(mouseX, mouseY))
                return i; // ¡Encontrado! Devolver el índice de la opción
        }

        // Si el bucle termina sin encontrar coincidencia, el mouse no está sobre ninguna opción
        return -1;
    }

    private void ApplySelection()
    {
        switch (_selectedIndex)
        {
            case 0: // Iniciar Scout
                TGCGame.SelectedPlayerTank = GameConfig.TankClass.Scout;
                CurrentState = GameState.Playing;
                break;
            case 1: // Iniciar Medio
                TGCGame.SelectedPlayerTank = GameConfig.TankClass.Medium;
                CurrentState = GameState.Playing;
                break;
            case 2: // Iniciar Pesado
                TGCGame.SelectedPlayerTank = GameConfig.TankClass.Heavy;
                CurrentState = GameState.Playing;
                break;
            case 3: // Salir
                Environment.Exit(0);
                break;
        }
    }

    public void Draw(string extraInfo = "")
    {
        if (CurrentState == GameState.Playing) return; // En Playing no dibuja nada extra

        _spriteBatch.Begin();
        var vp = _spriteBatch.GraphicsDevice.Viewport;
        Vector2 center = new Vector2(vp.Width / 2f, vp.Height / 2f);

        // Fondo para menu y GameOver
        if (CurrentState == GameState.Menu || CurrentState == GameState.GameOver)
        {
            if (_menuBackground != null)
                _spriteBatch.Draw(_menuBackground, new Rectangle(0, 0, vp.Width, vp.Height), Color.White);

            // Capa oscura para mejorar legibilidad
            _spriteBatch.Draw(_whitePixel, new Rectangle(0, 0, vp.Width, vp.Height), Color.Black * 0.66f);
        }

        if (CurrentState == GameState.Menu)
            DrawMenu(center);
        else if (CurrentState == GameState.Paused)
            DrawCenteredText("PAUSA\nPresiona P para continuar", center);
        else if (CurrentState == GameState.GameOver)
            DrawCenteredText($"GAME OVER\n{extraInfo}\nPresiona ENTER para volver al menu", center);

        _spriteBatch.End();
    }

    private void DrawMenu(Vector2 center)
    {
        float startY = center.Y - (_font.LineSpacing * _menuOptions.Length / 2f);
        float spacing = 20f;

        for (int i = 0; i < _menuOptions.Length; i++)
        {
            string option = _menuOptions[i];
            var size = _font.MeasureString(option);
            var pos = new Vector2(center.X - size.X / 2f, startY + i * (_font.LineSpacing + spacing));
            bool isSelected = (i == _selectedIndex);

            Color textColor = isSelected ? Color.Gold : Color.White;
            Color shadowColor = Color.Black;

            if (isSelected)
            {
                // Feedback visual: texto dorado + sombra + flechas indicadoras
                _spriteBatch.DrawString(_font, option, pos + new Vector2(2, 2), shadowColor);
                _spriteBatch.DrawString(_font, option, pos, textColor);
                var arrowSize = _font.MeasureString("> ");
                _spriteBatch.DrawString(_font, "> ", new Vector2(pos.X - arrowSize.X, pos.Y), textColor);
                _spriteBatch.DrawString(_font, " <", pos + new Vector2(size.X, 0), textColor);
            }
            else
            {
                _spriteBatch.DrawString(_font, option, pos + new Vector2(2, 2), shadowColor);
                _spriteBatch.DrawString(_font, option, pos, textColor);
            }
        }
    }

    private void DrawCenteredText(string text, Vector2 center)
    {
        var size = _font.MeasureString(text);
        var pos = center - size / 2;
        _spriteBatch.DrawString(_font, text, pos + new Vector2(2), Color.Black);
        _spriteBatch.DrawString(_font, text, pos, Color.White);
    }

    public void ForceState(GameState state) => CurrentState = state;
}