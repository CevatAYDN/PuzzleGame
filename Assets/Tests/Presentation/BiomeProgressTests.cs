using System.Collections.Generic;
using NUnit.Framework;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;
using PuzzleGame.Presentation.UI;

namespace PuzzleGame.Tests.Presentation
{
    [TestFixture]
    public class BiomeProgressTests
    {
        private sealed class FakeLevelProgress : ILevelProgressService
        {
            private readonly HashSet<int> _completed = new HashSet<int>();
            private readonly Dictionary<int, int> _stars = new Dictionary<int, int>();

            public void Complete(int level, int stars)
            {
                _completed.Add(level);
                if (!_stars.TryGetValue(level, out int existing) || stars > existing)
                    _stars[level] = stars;
            }

            public bool IsUnlocked(int levelNumber) => true;
            public int GetStars(int levelNumber) =>
                _stars.TryGetValue(levelNumber, out int s) ? s : 0;
            public int GetBestMoves(int levelNumber) => 0;
            public bool IsCompleted(int levelNumber) => _completed.Contains(levelNumber);
            public void RecordCompletion(int levelNumber, int moveCount, int stars) => Complete(levelNumber, stars);
            public void ResetAll()
            {
                _completed.Clear();
                _stars.Clear();
            }
        }

        [Test]
        public void GetTotalLevels_CrystalMines_Returns25()
        {
            Assert.AreEqual(25, BiomeProgress.GetTotalLevels(Biome.CrystalMines));
        }

        [Test]
        public void GetTotalLevels_VolcanicForge_Returns25()
        {
            Assert.AreEqual(25, BiomeProgress.GetTotalLevels(Biome.VolcanicForge));
        }

        [Test]
        public void GetStartLevel_CrystalMines_Returns1()
        {
            Assert.AreEqual(1, BiomeProgress.GetStartLevel(Biome.CrystalMines));
        }

        [Test]
        public void GetStartLevel_VolcanicForge_Returns26()
        {
            Assert.AreEqual(26, BiomeProgress.GetStartLevel(Biome.VolcanicForge));
        }

        [Test]
        public void GetEndLevel_CrystalMines_Returns25()
        {
            Assert.AreEqual(25, BiomeProgress.GetEndLevel(Biome.CrystalMines));
        }

        [Test]
        public void GetEndLevel_VolcanicForge_Returns50()
        {
            Assert.AreEqual(50, BiomeProgress.GetEndLevel(Biome.VolcanicForge));
        }

        [Test]
        public void GetMaxStars_BothBiomes_Return75()
        {
            Assert.AreEqual(75, BiomeProgress.GetMaxStars(Biome.CrystalMines));
            Assert.AreEqual(75, BiomeProgress.GetMaxStars(Biome.VolcanicForge));
        }

        [Test]
        public void GetCompletedCount_NullProgress_ReturnsZero()
        {
            Assert.AreEqual(0, BiomeProgress.GetCompletedCount(null, Biome.CrystalMines));
            Assert.AreEqual(0, BiomeProgress.GetCompletedCount(null, Biome.VolcanicForge));
        }

        [Test]
        public void GetCompletedCount_NoCompletions_ReturnsZero()
        {
            var p = new FakeLevelProgress();
            Assert.AreEqual(0, BiomeProgress.GetCompletedCount(p, Biome.CrystalMines));
            Assert.AreEqual(0, BiomeProgress.GetCompletedCount(p, Biome.VolcanicForge));
        }

        [Test]
        public void GetCompletedCount_CrystalMines_OnlyCountsCrystalMines()
        {
            var p = new FakeLevelProgress();
            p.Complete(5, 3);
            p.Complete(10, 2);
            p.Complete(25, 3);
            p.Complete(26, 3); // VolcanicForge — should NOT count
            p.Complete(40, 1); // VolcanicForge — should NOT count
            Assert.AreEqual(3, BiomeProgress.GetCompletedCount(p, Biome.CrystalMines));
        }

        [Test]
        public void GetCompletedCount_VolcanicForge_OnlyCountsVolcanicForge()
        {
            var p = new FakeLevelProgress();
            p.Complete(26, 3);
            p.Complete(30, 2);
            p.Complete(50, 3);
            p.Complete(1, 3);  // CrystalMines — should NOT count
            p.Complete(25, 3); // CrystalMines — should NOT count
            Assert.AreEqual(3, BiomeProgress.GetCompletedCount(p, Biome.VolcanicForge));
        }

        [Test]
        public void GetCompletedCount_AllLevelsCompleted_ReturnsTotal()
        {
            var p = new FakeLevelProgress();
            for (int i = 1; i <= 25; i++) p.Complete(i, 3);
            Assert.AreEqual(25, BiomeProgress.GetCompletedCount(p, Biome.CrystalMines));
        }

        [Test]
        public void GetStarCount_SumsStarsAcrossBiome()
        {
            var p = new FakeLevelProgress();
            p.Complete(1, 3);
            p.Complete(2, 2);
            p.Complete(3, 1);
            p.Complete(26, 3); // not in CrystalMines
            Assert.AreEqual(6, BiomeProgress.GetStarCount(p, Biome.CrystalMines));
        }

        [Test]
        public void GetStarCount_NullProgress_ReturnsZero()
        {
            Assert.AreEqual(0, BiomeProgress.GetStarCount(null, Biome.VolcanicForge));
        }

        [Test]
        public void IsBiomeComplete_AllLevelsCompleted_ReturnsTrue()
        {
            var p = new FakeLevelProgress();
            for (int i = 26; i <= 50; i++) p.Complete(i, 3);
            Assert.IsTrue(BiomeProgress.IsBiomeComplete(p, Biome.VolcanicForge));
        }

        [Test]
        public void IsBiomeComplete_PartialProgress_ReturnsFalse()
        {
            var p = new FakeLevelProgress();
            for (int i = 26; i <= 49; i++) p.Complete(i, 3);
            Assert.IsFalse(BiomeProgress.IsBiomeComplete(p, Biome.VolcanicForge));
        }

        [Test]
        public void IsBiomeComplete_NoProgress_ReturnsFalse()
        {
            var p = new FakeLevelProgress();
            Assert.IsFalse(BiomeProgress.IsBiomeComplete(p, Biome.CrystalMines));
        }
    }
}
