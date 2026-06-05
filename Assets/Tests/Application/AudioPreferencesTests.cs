using NUnit.Framework;
using PuzzleGame.Application.Interfaces;

namespace PuzzleGame.Tests.Application
{
    /// <summary>
    /// Pure C# tests for the AudioPreferences value type — no Unity dependency.
    /// </summary>
    [TestFixture]
    public class AudioPreferencesTests
    {
        [Test]
        public void Default_HasMusicAndSfxEnabledAndStandardVolumes()
        {
            var d = AudioPreferences.Default;

            Assert.IsTrue(d.MusicEnabled);
            Assert.IsTrue(d.SfxEnabled);
            Assert.AreEqual(0.6f, d.MusicVolume, 0.0001f);
            Assert.AreEqual(0.8f, d.SfxVolume, 0.0001f);
        }

        [Test]
        public void EffectiveMusicVolume_ZeroWhenDisabled_OtherwiseRawVolume()
        {
            var off = new AudioPreferences(false, true, 0.7f, 0.9f);
            var on  = new AudioPreferences(true,  true, 0.7f, 0.9f);

            Assert.AreEqual(0f,   off.EffectiveMusicVolume, 0.0001f);
            Assert.AreEqual(0.7f, on.EffectiveMusicVolume,  0.0001f);
        }

        [Test]
        public void EffectiveSfxVolume_ZeroWhenDisabled_OtherwiseRawVolume()
        {
            var off = new AudioPreferences(true, false, 0.4f, 0.9f);
            var on  = new AudioPreferences(true, true,  0.4f, 0.9f);

            Assert.AreEqual(0f,   off.EffectiveSfxVolume, 0.0001f);
            Assert.AreEqual(0.9f, on.EffectiveSfxVolume,  0.0001f);
        }

        [Test]
        public void Constructor_ClampsNegativeAndOutOfRangeVolumes()
        {
            var s = new AudioPreferences(true, true, -0.5f, 2.5f);

            Assert.AreEqual(0f, s.MusicVolume, 0.0001f);
            Assert.AreEqual(1f, s.SfxVolume,   0.0001f);
        }

        [Test]
        public void With_BuildersPreserveAllUnrelatedFields()
        {
            var s = new AudioPreferences(true, false, 0.3f, 0.7f);

            Assert.AreEqual(new AudioPreferences(false, false, 0.3f, 0.7f), s.WithMusicEnabled(false));
            Assert.AreEqual(new AudioPreferences(true,  true,  0.3f, 0.7f), s.WithSfxEnabled(true));
            Assert.AreEqual(new AudioPreferences(true,  false, 0.9f, 0.7f), s.WithMusicVolume(0.9f));
            Assert.AreEqual(new AudioPreferences(true,  false, 0.3f, 0.2f), s.WithSfxVolume(0.2f));
        }

        [Test]
        public void Equality_TwoSettingsWithSameFields_AreEqual()
        {
            var a = new AudioPreferences(true, false, 0.5f, 0.4f);
            var b = new AudioPreferences(true, false, 0.5f, 0.4f);
            var c = new AudioPreferences(true, false, 0.5f, 0.41f);

            Assert.IsTrue(a.Equals(b));
            Assert.IsTrue(a == b);
            Assert.IsFalse(a.Equals(c));
            Assert.IsTrue(a != c);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Test]
        public void Equality_EnabledFlagsMatter()
        {
            var on  = new AudioPreferences(true,  true, 0.5f, 0.5f);
            var off = new AudioPreferences(false, true, 0.5f, 0.5f);

            Assert.IsFalse(on.Equals(off));
        }
    }
}
