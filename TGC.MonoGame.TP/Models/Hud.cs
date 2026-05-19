using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TGC.MonoGame.TP.Models;

/// <summary>
///     hud simple que muestra los controles del juego en pantalla.
///     TODO: hacerlo responsive reemplazando los valores absolutos por relativos
/// </summary>
public class Hud
{
    private SpriteBatch _spriteBatch;
    private SpriteFont _font;
    private readonly string[] _instructions;
    private readonly Color _textColor = Color.White;
    private readonly Vector2 _padding = new Vector2(20f, 20f);

    // === VARIABLES PARA FPS ===
    private float _fps;
    private float _fpsAccumulator;
    private int _fpsFrameCount;

    // === VARIABLES PARA DEBUG ===
    public Vector3 TankPosition { get; set; }
    public float TankFuel { get; set; }

    public Hud()
    {
        _instructions = new[]
        {
            "movimiento: w / a / s / d",
            "bocina: espacio",
            "zoom: rueda del mouse",
            "salir: escape"
        };
    }

    public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
    {
        _spriteBatch = new SpriteBatch(graphicsDevice);
        _font = content.Load<SpriteFont>("SpriteFonts/ArialFont");
    }

    /// <summary>
    ///     Actualiza el contador de FPS. Llamar desde TGCGame.Update()
    /// </summary>
    public void Update(GameTime gameTime)
    {
        _fpsAccumulator += (float)gameTime.ElapsedGameTime.TotalSeconds;
        _fpsFrameCount++;

        // Recalcular el promedio cada segundo
        if (_fpsAccumulator >= 1f)
        {
            _fps = _fpsFrameCount / _fpsAccumulator;
            _fpsAccumulator = 0f;
            _fpsFrameCount = 0;
        }
    }

    public void Draw()
    {
        // Esta linea es la que desactiva el Z-Buffer
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        var drawPosition = new Vector2(_padding.X, _padding.Y);

        foreach (var line in _instructions)
        {
            _spriteBatch.DrawString(_font, line, drawPosition + new Vector2(1, 1), Color.Black);
            _spriteBatch.DrawString(_font, line, drawPosition, _textColor);
            drawPosition.Y += _font.LineSpacing + 5f;
        }

        // === INDICADOR DE FPS (esquina superior derecha) ===
        string fpsText = $"FPS: {_fps:F0}";
        var fpsSize = _font.MeasureString(fpsText);
        var fpsPosition = new Vector2(
            _spriteBatch.GraphicsDevice.Viewport.Width - fpsSize.X - _padding.X,
            _padding.Y
        );

        // Color según rendimiento (verde/amarillo/rojo)
        Color fpsColor = _fps >= 60 ? Color.Lime :
                         _fps >= 30 ? Color.Yellow :
                         Color.Red;

        // Sombra para mejor legibilidad
        _spriteBatch.DrawString(_font, fpsText, fpsPosition + Vector2.One, Color.Black);
        _spriteBatch.DrawString(_font, fpsText, fpsPosition, fpsColor);

        // === COORDENADAS DEL TANQUE ===
        string posText = $"X: {TankPosition.X:F1}  Y: {TankPosition.Y:F1}  Z: {TankPosition.Z:F1}";
        var posSize = _font.MeasureString(posText);
        var posPosition = new Vector2(fpsPosition.X - 150, fpsPosition.Y + fpsSize.Y + 8f); // 8px debajo del FPS

        _spriteBatch.DrawString(_font, posText, posPosition + Vector2.One, Color.Black); // Sombra
        _spriteBatch.DrawString(_font, posText, posPosition, _textColor);                // Texto blanco

        // === COMBUSTIBLE DEL TANQUE ===
        string fuelText = $"FUEL: {(int)TankFuel} / 100";
        var fuelSize = _font.MeasureString(fuelText);
        var fuelPosition = new Vector2(posPosition.X, posPosition.Y + posSize.Y + 8f);

        Color fuelColor = TankFuel > 30f ? Color.Lime : TankFuel > 10f ? Color.Yellow : Color.Red;

        _spriteBatch.DrawString(_font, fuelText, fuelPosition + Vector2.One, Color.Black); // Sombra
        _spriteBatch.DrawString(_font, fuelText, fuelPosition, fuelColor);                // Texto blanco


        _spriteBatch.End();
    }

    public void Dispose()
    {
        _spriteBatch?.Dispose();
    }
}