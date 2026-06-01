using System;
using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Configuration;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Infrastructure.Pool;
using PuzzleGame.Logging;
using UnityEngine;
using UnityEngine.Audio;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Audio service backed by two GameObjectPool<AudioSource> pools (sfx + music).
    /// SFX clips auto-return to pool after playback (via ITweenService delay).
    /// Music has its own persistent pool slot, stoppable via StopMusic.
    /// </summary>
    public class AudioService : IAudioService
    {
        private readonly AudioConfig _config;
        private readonly ITweenService _tween;
        private readonly IGameObjectPool<AudioSource> _sfxPool;
        private readonly IGameObjectPool<AudioSource> _musicPool;

        private readonly Dictionary<AudioClipId, AudioClip> _clipMap;
        private readonly Dictionary<AudioSource, AudioClipId> _activeSfxIds = new Dictionary<AudioSource, AudioClipId>();
        private readonly Dictionary<AudioSource, AudioClipId> _activeMusicIds = new Dictionary<AudioSource, AudioClipId>();

        private AudioSource _currentMusic;
        private bool _isMuted;
        private bool _disposed;

        public float MasterVolume
        {
            get => _config.masterVolume;
            set { _config.masterVolume = Mathf.Clamp01(value); }
        }

        public float MusicVolume
        {
            get => _config.musicVolume;
            set { _config.musicVolume = Mathf.Clamp01(value); }
        }

        public float SfxVolume
        {
            get => _config.sfxVolume;
            set { _config.sfxVolume = Mathf.Clamp01(value); }
        }

        public AudioService(AudioConfig config, ITweenService tween)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _tween = tween ?? throw new ArgumentNullException(nameof(tween));

            var sfxPrefab = CreateAudioSourcePrefab("SfxAudioSource_Prefab", config.sfxGroup);
            var musicPrefab = CreateAudioSourcePrefab("MusicAudioSource_Prefab", config.musicGroup);

            _sfxPool = new GameObjectPool<AudioSource>(sfxPrefab, config.sfxPoolSize,
                onRent: ResetAudioSource,
                onReturn: ResetAudioSource);

            _musicPool = new GameObjectPool<AudioSource>(musicPrefab, config.musicPoolSize,
                onRent: ResetAudioSource,
                onReturn: ResetAudioSource);

            _clipMap = new Dictionary<AudioClipId, AudioClip>
            {
                { AudioClipId.PourLoop,       config.pourLoopClip },
                { AudioClipId.PourEnd,        config.pourEndClip },
                { AudioClipId.Error,          config.errorClip },
                { AudioClipId.LevelComplete,  config.levelCompleteClip },
                { AudioClipId.LevelStart,     config.levelStartClip },
                { AudioClipId.UiClick,        config.uiClickClip },
                { AudioClipId.CorkPop,        config.corkPopClip }
            };
        }

        public void PlaySfx(AudioClipId id, Vector3? worldPos = null)
        {
            if (_disposed) return;
            if (_isMuted && id != AudioClipId.UiClick) return;
            if (!_clipMap.TryGetValue(id, out var clip) || clip == null)
            {
                BottleLogger.LogDebug($"AudioService.PlaySfx: no clip for {id}");
                return;
            }

            AudioSource source;
            try
            {
                source = _sfxPool.Rent();
            }
            catch (Exception e)
            {
                BottleLogger.LogWarning($"AudioService.PlaySfx: pool exhausted ({e.Message})");
                return;
            }

            ConfigureSource(source, clip, loop: false, worldPos);
            _activeSfxIds[source] = id;
            source.Play();
        }

        public void PlayMusic(AudioClipId id, bool loop = true)
        {
            if (_disposed) return;
            if (!_clipMap.TryGetValue(id, out var clip) || clip == null)
            {
                BottleLogger.LogDebug($"AudioService.PlayMusic: no clip for {id}");
                return;
            }

            if (_currentMusic != null)
                StopMusic(0f);

            _currentMusic = _musicPool.Rent();
            _activeMusicIds[_currentMusic] = id;

            ConfigureSource(_currentMusic, clip, loop, worldPos: null);
            _currentMusic.volume = MusicVolume * MasterVolume;
            _currentMusic.Play();
        }

        public void StopMusic(float fadeOut = 0.5f)
        {
            if (_disposed) return;
            if (_currentMusic == null) return;

            var music = _currentMusic;
            _currentMusic = null;

            if (fadeOut <= 0f)
            {
                FinalizeMusicStop(music);
                return;
            }

            // Fade out via tween; tween's OnComplete releases the source
            float startVol = music.volume;
            _tween.TweenCustom(music, 0f, 1f, fadeOut, (tweenable, t) =>
            {
                if (music == null) return;
                music.volume = Mathf.Lerp(startVol, 0f, t);
            })
            .OnComplete(() => FinalizeMusicStop(music));
        }

        public void MuteAll()
        {
            _isMuted = true;
            AudioListener.pause = true;
        }

        public void UnmuteAll()
        {
            _isMuted = false;
            AudioListener.pause = false;
        }

        public void ReleaseAll()
        {
            if (_disposed) return;
            _disposed = true;

            if (_currentMusic != null)
            {
                _currentMusic.Stop();
                _musicPool.Return(_currentMusic);
                _currentMusic = null;
            }

            // Force return of all active sfx (skip the auto-return)
            foreach (var kvp in _activeSfxIds)
                if (kvp.Key != null) _sfxPool.Return(kvp.Key);
            _activeSfxIds.Clear();
        }

        // ──────────────────────────────────────────────
        //  Private helpers
        // ──────────────────────────────────────────────

        private void FinalizeMusicStop(AudioSource music)
        {
            if (music == null) return;
            music.Stop();
            if (_activeMusicIds.ContainsKey(music))
                _activeMusicIds.Remove(music);
            _musicPool.Return(music);
        }

        private void ConfigureSource(AudioSource source, AudioClip clip, bool loop, Vector3? worldPos)
        {
            source.clip = clip;
            source.loop = loop;
            source.volume = SfxVolume * MasterVolume;
            source.pitch = 1f;
            source.spatialBlend = (_config.spatialBlend3D && worldPos.HasValue) ? 1f : 0f;
            if (worldPos.HasValue)
            {
                source.transform.position = worldPos.Value;
                source.minDistance = _config.spatialMinDistance;
                source.maxDistance = _config.spatialMaxDistance;
            }
        }

        private void ResetAudioSource(AudioSource source)
        {
            if (source == null) return;
            source.Stop();
            source.clip = null;
            source.volume = 1f;
            source.loop = false;
            source.spatialBlend = 0f;
            source.minDistance = 1f;
            source.maxDistance = 500f;
            _activeSfxIds.Remove(source);
            _activeMusicIds.Remove(source);
        }

        private static AudioSource CreateAudioSourcePrefab(string name, AudioMixerGroup group)
        {
            var go = new GameObject(name, typeof(AudioSource));
            go.SetActive(false);
            var source = go.GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.outputAudioMixerGroup = group;
            source.spatialBlend = 0f;
            return source;
        }
    }
}
