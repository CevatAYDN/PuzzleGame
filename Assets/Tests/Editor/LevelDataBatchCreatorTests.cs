using NUnit.Framework;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Models;
using PuzzleGame.Editor;
using PuzzleGame.Presentation.UI;

namespace PuzzleGame.Tests.Editor
{
    /// <summary>
    /// Validates GDD-aligned 50-level parameter generation. Pure C# tests
    /// (no AssetDatabase) — runs in EditMode without Unity asset pipeline.
    /// </summary>
    [TestFixture]
    public class LevelDataBatchCreatorTests
    {
        [Test]
        public void GetParametersForLevel_OutOfRange_ReturnsDefault()
        {
            Assert.AreEqual(default(LevelParameters), LevelDataBatchCreator.GetParametersForLevel(0));
            Assert.AreEqual(default(LevelParameters), LevelDataBatchCreator.GetParametersForLevel(-1));
            Assert.AreEqual(default(LevelParameters), LevelDataBatchCreator.GetParametersForLevel(51));
            Assert.AreEqual(default(LevelParameters), LevelDataBatchCreator.GetParametersForLevel(100));
        }

        [Test]
        public void GetParametersForLevel_All50_AreNonDefault()
        {
            for (int i = 1; i <= 50; i++)
            {
                var p = LevelDataBatchCreator.GetParametersForLevel(i);
                Assert.AreNotEqual(default, p, $"Level {i} returned default parameters");
                Assert.AreEqual(i, p.LevelNumber);
                Assert.Greater(p.MoldCount, 0, $"Level {i} MoldCount");
                Assert.Greater(p.ColorCount, 0, $"Level {i} ColorCount");
                Assert.Greater(p.ParMoves, 0, $"Level {i} ParMoves");
                Assert.GreaterOrEqual(p.GoodMoves, p.ParMoves, $"Level {i} GoodMoves >= ParMoves");
            }
        }

        [Test]
        public void Difficulty_EscalatesAcrossTiers_TrivialToExpert()
        {
            Assert.AreEqual(Difficulty.Trivial, LevelDataBatchCreator.GetParametersForLevel(1).Difficulty);
            Assert.AreEqual(Difficulty.Trivial, LevelDataBatchCreator.GetParametersForLevel(10).Difficulty);
            Assert.AreEqual(Difficulty.Easy,    LevelDataBatchCreator.GetParametersForLevel(11).Difficulty);
            Assert.AreEqual(Difficulty.Easy,    LevelDataBatchCreator.GetParametersForLevel(20).Difficulty);
            Assert.AreEqual(Difficulty.Medium,  LevelDataBatchCreator.GetParametersForLevel(21).Difficulty);
            Assert.AreEqual(Difficulty.Medium,  LevelDataBatchCreator.GetParametersForLevel(30).Difficulty);
            Assert.AreEqual(Difficulty.Hard,    LevelDataBatchCreator.GetParametersForLevel(31).Difficulty);
            Assert.AreEqual(Difficulty.Hard,    LevelDataBatchCreator.GetParametersForLevel(40).Difficulty);
            Assert.AreEqual(Difficulty.Expert,  LevelDataBatchCreator.GetParametersForLevel(41).Difficulty);
            Assert.AreEqual(Difficulty.Expert,  LevelDataBatchCreator.GetParametersForLevel(50).Difficulty);
        }

        [Test]
        public void Biome_DistributedAsGDD_25CrystalMines_25VolcanicForge()
        {
            int crystalMines = 0, volcanicForge = 0;
            for (int i = 1; i <= 50; i++)
            {
                var p = LevelDataBatchCreator.GetParametersForLevel(i);
                if (p.Biome == Biome.CrystalMines) crystalMines++;
                else if (p.Biome == Biome.VolcanicForge) volcanicForge++;
            }

            Assert.AreEqual(25, crystalMines, "L01-L25 should be Crystal Mines");
            Assert.AreEqual(25, volcanicForge, "L26-L50 should be Volcanic Forge");
        }

        [Test]
        public void Biome_SeamAtL25L26_MatchesLevelBiomeClassifier()
        {
            for (int i = 1; i <= 50; i++)
            {
                var p = LevelDataBatchCreator.GetParametersForLevel(i);
                Assert.AreEqual(LevelBiomeClassifier.GetBiome(i), p.Biome,
                    $"Level {i} biome mismatch between batch creator and classifier");
            }
        }

        [Test]
        public void MoldCount_NeverExceedsForgeConstantsMax()
        {
            for (int i = 1; i <= 50; i++)
            {
                var p = LevelDataBatchCreator.GetParametersForLevel(i);
                Assert.LessOrEqual(p.MoldCount, ForgeConstants.MaxMoldsPerLevel,
                    $"Level {i} MoldCount {p.MoldCount} exceeds MaxMoldsPerLevel");
                Assert.GreaterOrEqual(p.MoldCount, ForgeConstants.MinMoldsPerLevel,
                    $"Level {i} MoldCount {p.MoldCount} below MinMoldsPerLevel");
            }
        }

        [Test]
        public void ColorCount_NeverExceedsForgeConstantsMax_AndRespectsIntraTierRamp()
        {
            for (int i = 1; i <= 50; i++)
            {
                var p = LevelDataBatchCreator.GetParametersForLevel(i);
                Assert.LessOrEqual(p.ColorCount, ForgeConstants.MaxColorsPerLevel,
                    $"Level {i} ColorCount {p.ColorCount} exceeds MaxColorsPerLevel");
                Assert.GreaterOrEqual(p.ColorCount, ForgeConstants.MinColorsPerLevel,
                    $"Level {i} ColorCount {p.ColorCount} below MinColorsPerLevel");
            }

            // Intra-tier ramp: L01 and L10 (both Trivial) — L10 should have >= L01
            var p01 = LevelDataBatchCreator.GetParametersForLevel(1);
            var p10 = LevelDataBatchCreator.GetParametersForLevel(10);
            Assert.GreaterOrEqual(p10.ColorCount, p01.ColorCount, "ColorCount should not decrease within a tier");
        }

        [Test]
        public void RandomSeed_UniquePerLevel()
        {
            var seen = new System.Collections.Generic.HashSet<int>();
            for (int i = 1; i <= 50; i++)
            {
                var p = LevelDataBatchCreator.GetParametersForLevel(i);
                Assert.IsTrue(seen.Add(p.RandomSeed), $"Duplicate seed for level {i}: {p.RandomSeed}");
                Assert.AreEqual(i * 1337, p.RandomSeed, $"Seed formula for level {i}");
            }
        }

        [Test]
        public void MoveThresholds_GoodGreaterOrEqualToPar_AndParAtLeastThree()
        {
            for (int i = 1; i <= 50; i++)
            {
                var p = LevelDataBatchCreator.GetParametersForLevel(i);
                Assert.GreaterOrEqual(p.GoodMoves, p.ParMoves, $"Level {i} GoodMoves < ParMoves");
                Assert.GreaterOrEqual(p.ParMoves, 3, $"Level {i} ParMoves too small");
            }
        }
    }
}
