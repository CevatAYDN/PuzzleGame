using NUnit.Framework;
using PuzzleGame.Domain.Models;
using PuzzleGame.Presentation.UI;

namespace PuzzleGame.Tests.Presentation
{
    [TestFixture]
    public class LevelBiomeClassifierTests
    {
        [TestCase(1, Biome.CrystalMines)]
        [TestCase(15, Biome.CrystalMines)]
        [TestCase(25, Biome.CrystalMines)]
        [TestCase(26, Biome.VolcanicForge)]
        [TestCase(40, Biome.VolcanicForge)]
        [TestCase(50, Biome.VolcanicForge)]
        public void GetBiome_AssignsCorrectBiome(int level, Biome expected)
        {
            Assert.AreEqual(expected, LevelBiomeClassifier.GetBiome(level));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-100)]
        public void GetBiome_InvalidLevel_ReturnsCrystalMines(int level)
        {
            Assert.AreEqual(Biome.CrystalMines, LevelBiomeClassifier.GetBiome(level));
        }

        [TestCase(1, true)]
        [TestCase(25, true)]
        [TestCase(26, false)]
        [TestCase(50, false)]
        public void IsInCrystalMines_ReturnsCorrectly(int level, bool expected)
        {
            Assert.AreEqual(expected, LevelBiomeClassifier.IsInCrystalMines(level));
        }

        [TestCase(1, false)]
        [TestCase(25, false)]
        [TestCase(26, true)]
        [TestCase(50, true)]
        public void IsInVolcanicForge_ReturnsCorrectly(int level, bool expected)
        {
            Assert.AreEqual(expected, LevelBiomeClassifier.IsInVolcanicForge(level));
        }

        [Test]
        public void CrystalMinesEndLevel_Is25()
        {
            Assert.AreEqual(25, LevelBiomeClassifier.CrystalMinesEndLevel);
        }

        [Test]
        public void VolcanicForgeStartLevel_Is26()
        {
            Assert.AreEqual(26, LevelBiomeClassifier.VolcanicForgeStartLevel);
        }
    }
}
