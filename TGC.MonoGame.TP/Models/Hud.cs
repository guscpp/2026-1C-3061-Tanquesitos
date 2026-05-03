using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TGC.MonoGame.TP.Models;

/// <summary>
/// hud simple que muestra los controles del juego en pantalla.
/// </summary>
public class Hud
{
    private SpriteBatch _spriteBatch;
    private SpriteFont _font;
    private readonly string[] _instructions;
    private readonly Color _textColor = Color.White;
    private readonly Vector2 _padding = new Vector2(20f, 20f);

    public Hud()
    {
        _instructions = new[]
        {
            "movimiento: w / a / s / d",
            "levitar: q / e",
            "torreta / canon: mouse (TODO)",
            "barra espaciadora: bocina",
            "zoom: rueda del mouse",
            "salir: escape",
            "shaders: el BasicEffect que no podemos usar en el TP :,("
        };
    }

    public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
    {
        _spriteBatch = new SpriteBatch(graphicsDevice);
        _font = content.Load<SpriteFont>("SpriteFonts/ArialFont");
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

        _spriteBatch.End();
    }

    public void Dispose()
    {
        _spriteBatch?.Dispose();
    }
}