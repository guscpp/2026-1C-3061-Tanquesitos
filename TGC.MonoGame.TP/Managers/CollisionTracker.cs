using System;

namespace TGC.MonoGame.TP.Managers
{
    public class SimpleCollisionTracker
    {
        private bool _collidedLastFrame = false;
        private bool _collidedThisFrame = false;
        private bool _playedThisFrame = false;

        // Lock para evitar condiciones de carrera
        private readonly object _lock = new object();

        public void BeginFrame()
        {
            lock (_lock)
            {
                _collidedLastFrame = _collidedThisFrame;
                _collidedThisFrame = false;
                _playedThisFrame = false;
            }
        }

        /// <summary>
        /// Evaloa los flags. Solo ejecuta la accion si es una colision nueva.
        /// El delegado debe retornar TRUE si realmente reprodujo un sonido.
        /// </summary>
        public void TryPlay(Func<bool> playAction)
        {
            lock (_lock)
            {
                if (_playedThisFrame)
                {
                    _collidedThisFrame = true;
                    return;
                }

                if (!_collidedLastFrame)
                {
                    // Ejecutamos la accion. Si retorna true, significa que si era un objeto con sonido
                    if (playAction())
                    {
                        _playedThisFrame = true; // Bloqueamos otros sonidos en este frame
                    }
                }

                // Marcamos que hubo colision este frame (para saber que ya estamos tocando algo)
                _collidedThisFrame = true;
            }
        }
    }
}