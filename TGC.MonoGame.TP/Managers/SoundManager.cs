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
            AddSoundEffect("cannon_fire", "Sounds/1-cannon_fire");
            AddSoundEffect("colision_casa", "Sounds/2-freesound_community-medium-explosion-40472");
            AddSoundEffect("impacto_mediana_escala", "Sounds/3-dragon-studio-boulder-impact-487673");
            AddSoundEffect("agarrar_combustible_1", "Sounds/4a-Power Up");
            AddSoundEffect("agarrar_combustible_2", "Sounds/4b-gravity_inverter");
            AddSoundEffect("viento", "Sounds/5-tanweraman-desert-wind-2-350417");
            AddSoundEffect("cooldown_not_ready", "Sounds/6-gun_reload_lock_or_click_sound");
            AddSoundEffect("escalera", "Sounds/7-freesound_community-floorcracking-84506");
            AddSoundEffect("enemy_cannon_fire", "Sounds/8-freesound_community-gunner-sound-43794");
            AddSoundEffect("planta_rodadora", "Sounds/9-freesound_community-leaves-14478");
            AddSoundEffect("carroceria_avanzando_1", "Sounds/10a-engine_heavy_loop");
            AddSoundEffect("carroceria_avanzando_2", "Sounds/10b-engine_heavy_slow_loop");
            AddSoundEffect("carroceria_avanzando_3", "Sounds/10c-engine_heavy_average_loop");
            AddSoundEffect("carroceria_avanzando_4", "Sounds/10d-engine_heavy_fast_loop");
            AddSoundEffect("bajo_combustible_1", "Sounds/11a-588220__mehraniiii__magical-warning");
            AddSoundEffect("bajo_combustible_2", "Sounds/11b-629312__greatsoundstube__warning-chime");
            AddSoundEffect("rotar_torreta", "Sounds/12-freesound_community-tank-turret-rotate-14879");
            AddSoundEffect("klaxon", "Sounds/13-universfield-truck-signal-153263");
            AddSoundEffect("fuego", "Sounds/14-dragon-studio-fire-sounds-405444");
            AddSoundEffect("golpear_arbol", "Sounds/15-u_xjrmmgxfru-hit-tree-03-266306");
            AddSoundEffect("golpear_roca", "Sounds/16-u_xjrmmgxfru-hit-rock-03-266305");
            AddSoundEffect("player_muere", "Sounds/17- rock_breaking");
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

        //Reproducir sonido 3D
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

        //Reproducir sonido stereo
        public void PlaySound(string soundName)
        {
            if (_soundEffects.ContainsKey(soundName))
            {
                _soundEffects[soundName].Play();
            }
        }


    }
}