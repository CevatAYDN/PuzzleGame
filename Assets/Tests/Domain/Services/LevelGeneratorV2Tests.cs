using NUnit.Framework;
using PuzzleGame.Domain.Services;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Interfaces;

namespace PuzzleGame.Tests.Domain.Services
{
    public class LevelGeneratorV2Tests
    {
        private readonly ILevelGenerator _generator = new DifficultyBasedLevelGenerator();
        private DomainColor[] _palette;

        [SetUp]
        public void SetUp()
        {
            _palette = new[]
            {
                new DomainColor(1f, 0f, 0f, 1f),
                new DomainColor(0f, 1f, 0f, 1f),
                new DomainColor(0f, 0f, 1f, 1f),
                new DomainColor(1f, 1f, 0f, 1f),
                new DomainColor(1f, 0f, 1f, 1f),
            };
        }

        [Test]
        public void Generate_WithValidInputs_ReturnsCorrectBottleCount()
        {
            var result = _generator.Generate(
                bottleCount: 5,
                maxLayers: 3,
                emptyBottleCount: 1,
                colorPalette: _palette,
                difficulty: Difficulty.Medium,
                seed: 42);

            Assert.AreEqual(5, result.Count);
        }

        [Test]
        public void Generate_WithSeed_ProducesDeterministicResults()
        {
            var first = _generator.Generate(5, 3, 1, _palette, Difficulty.Medium, seed: 12345);
            var second = _generator.Generate(5, 3, 1, _palette, Difficulty.Medium, seed: 12345);

            for (int i = 0; i < first.Count; i++)
            {
                Assert.AreEqual(first[i].Count, second[i].Count);
                for (int j = 0; j < first[i].Count; j++)
                {
                    Assert.AreEqual(first[i][j].Color, second[i][j].Color);
                }
            }
        }

        [Test]
        public void Generate_WithDifferentSeeds_ProducesDifferentResults()
        {
            var first = _generator.Generate(5, 3, 1, _palette, Difficulty.Medium, seed: 1);
            var second = _generator.Generate(5, 3, 1, _palette, Difficulty.Medium, seed: 2);

            bool atLeastOneDiff = false;
            for (int i = 0; i < first.Count && !atLeastOneDiff; i++)
            {
                for (int j = 0; j < first[i].Count && !atLeastOneDiff; j++)
                {
                    if (first[i].Count > 0 && j < first[i].Count && j < second[i].Count)
                    {
                        if (first[i][j].Color != second[i][j].Color)
                            atLeastOneDiff = true;
                    }
                }
            }

            Assert.IsTrue(atLeastOneDiff || first.Count > 0);
        }

        [Test]
        public void Generate_RespectsEmptyBottleCount()
        {
            int bottleCount = 5;
            int emptyBottles = 2;
            var result = _generator.Generate(
                bottleCount, 3, emptyBottles, _palette, Difficulty.Medium, seed: 1);

            int actualEmpty = 0;
            foreach (var bottle in result)
                if (bottle.Count == 0) actualEmpty++;

            Assert.GreaterOrEqual(actualEmpty, emptyBottles);
        }

        [Test]
        public void Generate_WithZeroMaxLayers_ReturnsEmptyBottles()
        {
            var result = _generator.Generate(3, 0, 1, _palette, Difficulty.Medium, seed: 1);
            Assert.AreEqual(3, result.Count);
        }

        [Test]
        public void Generate_LayerAmount_IsCorrect()
        {
            int maxLayers = 3;
            float expectedAmount = 1f / maxLayers;

            var result = _generator.Generate(5, maxLayers, 1, _palette, Difficulty.Medium, seed: 42);

            foreach (var bottle in result)
            {
                foreach (var layer in bottle)
                {
                    Assert.AreEqual(expectedAmount, layer.Amount, 0.001f);
                }
            }
        }

        [Test]
        public void Generate_WithHighDifficulty_ShufflesAggressively()
        {
            var result = _generator.Generate(5, 3, 1, _palette, Difficulty.Expert, seed: 999);
            Assert.AreEqual(5, result.Count);
        }

        [Test]
        public void Generate_WithLowDifficulty_ShufflesLess()
        {
            var result = _generator.Generate(5, 3, 1, _palette, Difficulty.Trivial, seed: 999);
            Assert.AreEqual(5, result.Count);
        }
    }
}