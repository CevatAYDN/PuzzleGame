using NUnit.Framework;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Services;
using PuzzleGame.Configuration;
using PuzzleGame.Infrastructure.Pool;
using UnityEngine;

namespace PuzzleGame.Tests.Application.Services
{
    public class AudioServiceTests
    {
        private AudioConfig _config;
        private AudioService _sut;
        private PoolManager _poolManager;

        [SetUp]
        public void Setup()
        {
            _config = ScriptableObject.CreateInstance<AudioConfig>();
            _config.masterVolume = 1.0f;
            _config.musicVolume = 0.6f;
            _config.sfxVolume = 0.8f;
            _config.sfxPoolSize = 4;
            _config.musicPoolSize = 2;
            _config.spatialBlend3D = false;

            // Use a stub clip for the SFX tests
            _config.pourEndClip = AudioClip.Create("StubPour", 44100, 1, 44100, false);
            _config.errorClip = AudioClip.Create("StubError", 44100, 1, 44100, false);
            _config.levelCompleteClip = AudioClip.Create("StubLevel", 44100, 1, 44100, false);
            _config.levelStartClip = AudioClip.Create("StubStart", 44100, 1, 44100, false);
            _config.uiClickClip = AudioClip.Create("StubUi", 44100, 1, 44100, false);
            _config.corkPopClip = AudioClip.Create("StubCork", 44100, 1, 44100, false);

            _poolManager = new PoolManager();
            var tween = new CoroutineTweenService();
            _sut = new AudioService(_config, tween, _poolManager);
        }

        [TearDown]
        public void Teardown()
        {
            _sut?.ReleaseAll();
            _poolManager?.Dispose();
            if (_config != null) Object.DestroyImmediate(_config);
            // Reset global state to prevent test pollution
            AudioListener.pause = false;
            AudioListener.volume = 1f;
        }

        [Test]
        public void Constructor_WithNullConfig_Throws()
        {
            var tween = new CoroutineTweenService();
            Assert.That(() => new AudioService(null, tween, _poolManager), Throws.ArgumentNullException);
        }

        [Test]
        public void Constructor_WithNullTween_Throws()
        {
            Assert.That(() => new AudioService(_config, null, _poolManager), Throws.ArgumentNullException);
        }

        [Test]
        public void Constructor_WithNullPoolManager_Throws()
        {
            var tween = new CoroutineTweenService();
            Assert.That(() => new AudioService(_config, tween, null), Throws.ArgumentNullException);
        }

        [Test]
        public void MasterVolume_ClampedBetweenZeroAndOne()
        {
            _sut.MasterVolume = 5f;
            Assert.That(_sut.MasterVolume, Is.EqualTo(1f));
            _sut.MasterVolume = -2f;
            Assert.That(_sut.MasterVolume, Is.EqualTo(0f));
        }

        [Test]
        public void MusicVolume_ClampedBetweenZeroAndOne()
        {
            _sut.MusicVolume = 5f;
            Assert.That(_sut.MusicVolume, Is.EqualTo(1f));
        }

        [Test]
        public void SfxVolume_ClampedBetweenZeroAndOne()
        {
            _sut.SfxVolume = -0.5f;
            Assert.That(_sut.SfxVolume, Is.EqualTo(0f));
        }

        [Test]
        public void PlaySfx_UnknownId_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _sut.PlaySfx(AudioClipId.PourLoop)); // No clip assigned
        }

        [Test]
        public void PlaySfx_ValidClip_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _sut.PlaySfx(AudioClipId.PourEnd));
        }

        [Test]
        public void PlayMusic_ValidClip_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _sut.PlayMusic(AudioClipId.LevelStart, loop: true));
        }

        [Test]
        public void PlayMusic_ReplacesPreviousMusic()
        {
            _sut.PlayMusic(AudioClipId.LevelStart);
            Assert.DoesNotThrow(() => _sut.PlayMusic(AudioClipId.LevelComplete));
        }

        [Test]
        public void StopMusic_WithoutActiveMusic_NoException()
        {
            Assert.DoesNotThrow(() => _sut.StopMusic());
        }

        [Test]
        public void StopMusic_AfterPlay_NoException()
        {
            _sut.PlayMusic(AudioClipId.LevelStart);
            Assert.DoesNotThrow(() => _sut.StopMusic(0f));
        }

        [Test]
        public void MuteAll_ThenUnmuteAll_NoException()
        {
            Assert.DoesNotThrow(() => _sut.MuteAll());
            Assert.DoesNotThrow(() => _sut.UnmuteAll());
        }

        [Test]
        public void ReleaseAll_Twice_NoException()
        {
            _sut.ReleaseAll();
            Assert.DoesNotThrow(() => _sut.ReleaseAll());
        }

        [Test]
        public void PlaySfx_AfterReleaseAll_NoOp()
        {
            _sut.ReleaseAll();
            Assert.DoesNotThrow(() => _sut.PlaySfx(AudioClipId.PourEnd));
        }
    }
}
