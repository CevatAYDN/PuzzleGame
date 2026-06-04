using System;
using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Application.Logging;
using UnityEngine;
using UnityEngine.Audio;

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// Audio service backed by two GameObjectPool<AudioSource> pools (sfx + music).
    /// SFX clips auto-return to pool after playback (via ITweenService delay).
    /// Music has its own persistent pool slot, stoppable via StopMusic.
    /// Relocated to Infrastructure layer (Fix #1).
    /// </summary>
    public class AudioService : IAudioService
    {
        private readonly AudioConfig _config;
        private readonly ITweenService _tween;
        private readonly IPoolManager _poolManager;
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

        public AudioService(AudioConfig config, ITweenService tween, IPoolManager poolManager)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _tween = tween ?? throw new ArgumentNullException(nameof(tween));
            _poolManager = poolManager ?? throw new ArgumentNullException(nameof(poolManager));

            var sfxPrefab = CreateAudioSourcePrefab("SfxAudioSource_Prefab", config.sfxGroup);
            var musicPrefab = CreateAudioSourcePrefab("MusicAudioSource_Prefab", config.musicGroup);
            if (sfxPrefab == null || musicPrefab == null)
                throw new InvalidOperationException("Failed to create AudioSource prefabs (Unity may have rejected component addition).");

            _sfxPool = _poolManager.RegisterPool<AudioSource>("SfxPool", sfxPrefab, config.sfxPoolSize,
                onRent: ResetAudioSource,
                onReturn: ResetAudioSource);

            _musicPool = _poolManager.RegisterPool<AudioSource>("MusicPool", musicPrefab, config.musicPoolSize,
                onRent: ResetAudioSource,
                onReturn: ResetAudioSource);

            _clipMap = new Dictionary<AudioClipId, AudioClip>
            {
                { AudioClipId.CastLoop,       config.CastLoopClip },
                { AudioClipId.CastEnd,        config.CastEndClip },
                { AudioClipId.Error,          config.errorClip },
                { AudioClipId.LevelComplete,  config.levelCompleteClip },
                { AudioClipId.LevelStart,     config.levelStartClip },
                { AudioClipId.UiClick,        config.uiClickClip },
                { AudioClipId.CorkPop,        config.corkPopClip }
            };
        }

        public void PlaySfx(AudioClipId id)
        {
            PlaySfxInternal(id, worldPos: null);
        }

        public void PlaySfxAt(AudioClipId id, Vector3 worldPos)
        {
            PlaySfxInternal(id, worldPos: worldPos);
        }

        private void PlaySfxInternal(AudioClipId id, Vector3? worldPos)
        {
            if (_disposed) return;
            if (_isMuted && id != AudioClipId.UiClick) return;
            if (!_clipMap.TryGetValue(id, out var clip) || clip == null)
            {
                MoldLogger.LogDebug($"AudioService.PlaySfx: no clip for {id}");
                return;
            }

            AudioSource source;
            try
            {
                source = _sfxPool.Rent();
            }
            catch (Exception e)
            {
                MoldLogger.LogWarning($"AudioService.PlaySfx: pool exhausted ({e.Message})");
                return;
            }

            ConfigureSource(source, clip, loop: false, worldPos, channelVolume: SfxVolume);
            _activeSfxIds[source] = id;
            source.Play();

            // Auto-return to pool after playback duration
            _tween.Delay(clip.length)
                .OnComplete(() =>
                {
                    if (!_disposed && source != null)
                    {
                        _sfxPool.Return(source);
                    }
                });
        }

        public void PlayMusic(AudioClipId id, bool loop = true)
        {
            if (_disposed) return;
            if (!_clipMap.TryGetValue(id, out var clip) || clip == null)
            {
                MoldLogger.LogDebug($"AudioService.PlayMusic: no clip for {id}");
                return;
            }

            if (_currentMusic != null)
                StopMusic(0f);

            _currentMusic = _musicPool.Rent();
            _activeMusicIds[_currentMusic] = id;

            ConfigureSource(_currentMusic, clip, loop, worldPos: null, channelVolume: MusicVolume);
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
                _activeMusicIds.Remove(_currentMusic);
                _musicPool.Return(_currentMusic);
                _currentMusic = null;
            }

            // Snapshot keys first to avoid CollectionModified during enumeration
            var activeSources = new List<AudioSource>(_activeSfxIds.Keys);
            foreach (var source in activeSources)
                if (source != null) _sfxPool.Return(source);
            _activeSfxIds.Clear();
            _activeMusicIds.Clear();
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

        private void ConfigureSource(AudioSource source, AudioClip clip, bool loop, Vector3? worldPos, float channelVolume)
        {
            source.clip = clip;
            source.loop = loop;
            source.volume = channelVolume * MasterVolume;
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
            go.hideFlags = HideFlags.HideAndDontSave;
            go.SetActive(false);
            var source = go.GetComponent<AudioSource>();
            if (source == null) return null;
            source.playOnAwake = false;
            // AudioMixerGroup is optional in the AudioConfig; only assign when supplied
            // to avoid the destroyed-asset null warning some Unity versions log on assignment.
            if (group != null)
                source.outputAudioMixerGroup = group;
            source.spatialBlend = 0f;
            return source;
        }
    }
}
