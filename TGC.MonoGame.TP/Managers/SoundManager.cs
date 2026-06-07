using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.IO;

namespace TGC.MonoGame.TP.Managers
{
    public class SoundManager
    {
        private ContentManager _content;
        private readonly Dictionary<string, SoundEffect> _soundEffects;
        private Song _currentSong;
        private AudioListener _listener;

        public SoundManager()
        {
            _soundEffects = new Dictionary<string, SoundEffect>();
            _listener = new AudioListener();
        }

        public void LoadContent(ContentManager content)
        {
            _content = content;

            // Registrar los sonidos disponibles. 
            // La ruta es relativa a la carpeta Content y no lleva extension.
            AddSoundEffect("cannon_fire", "Sounds/cannon_fire");
        }

        public void AddSoundEffect(string name, string assetPath)
        {
            if (!_soundEffects.ContainsKey(name))
            {
                _soundEffects.Add(name, _content.Load<SoundEffect>(assetPath));
            }
        }

        public void PlayMusic(string assetPath, bool isLooping = true)
        {
            StopMusic();
            _currentSong = _content.Load<Song>(assetPath);
            MediaPlayer.IsRepeating = isLooping;
            MediaPlayer.Play(_currentSong);
        }

        public void StopMusic()
        {
            if (MediaPlayer.State == MediaState.Playing)
            {
                MediaPlayer.Stop();
            }
        }

        public void PlaySound3D(string soundName, Vector3 emitterPosition, Vector3 listenerPosition, Vector3 listenerForward)
        {
            if (!_soundEffects.TryGetValue(soundName, out SoundEffect soundEffect))
            {
                return;
            }

            // Crear una instancia del sonido para poder aplicar efectos 3D
            SoundEffectInstance instance = soundEffect.CreateInstance();

            // Configurar el emisor (la fuente del sonido, ej: el tanque o la bala)
            AudioEmitter emitter = new AudioEmitter();
            // Convertimos a System.Numerics.Vector3 usando la extension del proyecto
            emitter.Position = emitterPosition.ToNumerics();
            emitter.Forward = Microsoft.Xna.Framework.Vector3.Forward.ToNumerics();
            emitter.Up = Microsoft.Xna.Framework.Vector3.Up.ToNumerics();

            // Configurar el oyente (la camara del jugador)
            _listener.Position = listenerPosition.ToNumerics();
            _listener.Forward = listenerForward.ToNumerics();
            _listener.Up = Microsoft.Xna.Framework.Vector3.Up.ToNumerics();
            _listener.Velocity = System.Numerics.Vector3.Zero;

            // Aplicar el efecto 3D y reproducir
            instance.Apply3D(_listener, emitter);
            instance.Play();
        }
    }
}