using NUnit.Framework;
using UnityEngine;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Infrastructure.Implementations;
using PuzzleGame.Infrastructure.Pool;
using PuzzleGame.Tests.Fakes;

namespace PuzzleGame.Tests.Infrastructure
{
    [TestFixture]
    public class AudioServiceTests
    {
        private AudioService _sut;
        private AudioConfig _config;
        private FakeTweenService _tween;
        private PoolManager _poolManager;

        [SetUp]
        public void Setup()
        {
            _config = ScriptableObject.CreateInstance<AudioConfig>();
            _config.sfxPoolSize = 4;
            _config.musicPoolSize = 2;
            _config.masterVolume = 1f;
            _config.musicVolume = 0.5f;
            _config.sfxVolume = 0.5f;

            _tween = new FakeTweenService();
            _poolManager = new PoolManager();
            _sut = new AudioService(_config, _tween, _poolManager);
        }

        [TearDown]
        public void Teardown()
        {
            _sut.ReleaseAll();
            _poolManager.Dispose();
            if (_config != null) Object.DestroyImmediate(_config);
        }

        [Test]
        public void VolumeProperties_GetAndSetCorrectly()
        {
            _sut.MasterVolume = 0.8f;
            _sut.MusicVolume = 0.4f;
            _sut.SfxVolume = 0.6f;

            Assert.AreEqual(0.8f, _sut.MasterVolume);
            Assert.AreEqual(0.4f, _sut.MusicVolume);
            Assert.AreEqual(0.6f, _sut.SfxVolume);
        }

        [Test]
        public void PlaySfx_WithMissingClip_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _sut.PlaySfx(AudioClipId.UiClick));
        }

        [Test]
        public void PlayMusic_WithMissingClip_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _sut.PlayMusic(AudioClipId.CastLoop));
        }

        [Test]
        public void MuteAndUnmute_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _sut.MuteAll());
            Assert.DoesNotThrow(() => _sut.UnmuteAll());
        }

        [Test]
        public void StopMusic_WhenNotPlaying_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _sut.StopMusic(0f));
        }
    }
}
