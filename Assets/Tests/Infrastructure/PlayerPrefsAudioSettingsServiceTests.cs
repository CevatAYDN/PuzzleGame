using System;
using System.Collections.Generic;
using NUnit.Framework;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Infrastructure.Implementations;
using UnityEngine;

namespace PuzzleGame.Tests.Infrastructure
{
    /// <summary>
    /// PlayerPrefs-backed audio settings tests. Uses real PlayerPrefs in the
    /// editor (SetUp/TearDown wipe the keys under test).
    /// </summary>
    [TestFixture]
    public class PlayerPrefsAudioSettingsServiceTests
    {
        // Mirror the keys from PlayerPrefsAudioSettingsService (private const)
        private const string KeyMusicEnabled = "PuzzleGame.Audio.MusicEnabled";
        private const string KeySfxEnabled   = "PuzzleGame.Audio.SfxEnabled";
        private const string KeyMusicVolume  = "PuzzleGame.Audio.MusicVolume";
        private const string KeySfxVolume    = "PuzzleGame.Audio.SfxVolume";

        [SetUp]
        public void SetUp() => WipePrefs();
        [TearDown]
        public void TearDown() => WipePrefs();

        private static void WipePrefs()
        {
            PlayerPrefs.DeleteKey(KeyMusicEnabled);
            PlayerPrefs.DeleteKey(KeySfxEnabled);
            PlayerPrefs.DeleteKey(KeyMusicVolume);
            PlayerPrefs.DeleteKey(KeySfxVolume);
            PlayerPrefs.Save();
        }

        [Test]
        public void Constructor_NoSavedPrefs_LoadsDefaults()
        {
            var svc = new PlayerPrefsAudioSettingsService();

            Assert.AreEqual(AudioPreferences.Default, svc.Current);
        }

        [Test]
        public void Constructor_WithSavedPrefs_LoadsPersistedValues()
        {
            PlayerPrefs.SetInt(KeyMusicEnabled, 0);
            PlayerPrefs.SetInt(KeySfxEnabled,   1);
            PlayerPrefs.SetFloat(KeyMusicVolume, 0.42f);
            PlayerPrefs.SetFloat(KeySfxVolume,   0.91f);
            PlayerPrefs.Save();

            var svc = new PlayerPrefsAudioSettingsService();

            var s = svc.Current;
            Assert.IsFalse(s.MusicEnabled);
            Assert.IsTrue(s.SfxEnabled);
            Assert.AreEqual(0.42f, s.MusicVolume, 0.0001f);
            Assert.AreEqual(0.91f, s.SfxVolume,   0.0001f);
        }

        [Test]
        public void Setters_MutateAndPersistAndRaiseEvent()
        {
            var svc = new PlayerPrefsAudioSettingsService();
            var events = new List<AudioPreferences>();
            svc.OnSettingsChanged += events.Add;

            svc.SetMusicEnabled(false);
            svc.SetSfxVolume(0.33f);

            // Each setter that actually changes the value should fire once.
            Assert.AreEqual(2, events.Count);
            Assert.IsFalse(events[0].MusicEnabled);
            Assert.IsTrue(events[0].SfxEnabled);
            Assert.IsFalse(events[1].MusicEnabled);
            Assert.AreEqual(0.33f, events[1].SfxVolume, 0.0001f);

            // Persisted
            var fresh = new PlayerPrefsAudioSettingsService();
            Assert.IsFalse(fresh.Current.MusicEnabled);
            Assert.AreEqual(0.33f, fresh.Current.SfxVolume, 0.0001f);
        }

        [Test]
        public void Setters_NoOpWhenValueUnchanged_DoesNotRaiseEvent()
        {
            var svc = new PlayerPrefsAudioSettingsService();
            int count = 0;
            svc.OnSettingsChanged += _ => count++;

            svc.SetMusicEnabled(AudioPreferences.Default.MusicEnabled); // already true
            svc.SetMusicVolume(AudioPreferences.Default.MusicVolume);   // already 0.6
            svc.SetSfxEnabled(AudioPreferences.Default.SfxEnabled);
            svc.SetSfxVolume(AudioPreferences.Default.SfxVolume);

            Assert.AreEqual(0, count);
        }

        [Test]
        public void ResetToDefaults_RestoresDefault_AndPersists()
        {
            PlayerPrefs.SetInt(KeyMusicEnabled, 0);
            PlayerPrefs.SetInt(KeySfxEnabled, 0);
            PlayerPrefs.SetFloat(KeyMusicVolume, 0.1f);
            PlayerPrefs.SetFloat(KeySfxVolume, 0.1f);
            PlayerPrefs.Save();

            var svc = new PlayerPrefsAudioSettingsService();
            svc.ResetToDefaults();

            Assert.AreEqual(AudioPreferences.Default, svc.Current);

            var fresh = new PlayerPrefsAudioSettingsService();
            Assert.AreEqual(AudioPreferences.Default, fresh.Current);
        }

        [Test]
        public void Save_ForcesFlush_NoOpByDefaultAfterSetters()
        {
            var svc = new PlayerPrefsAudioSettingsService();
            svc.SetMusicEnabled(false);

            // Setters already call PlayerPrefs.Save() internally; Save() is idempotent.
            Assert.DoesNotThrow(() => svc.Save());
        }

        [Test]
        public void Constructor_WithNullEventAggregator_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => new PlayerPrefsAudioSettingsService(eventAggregator: null));
        }
    }
}
