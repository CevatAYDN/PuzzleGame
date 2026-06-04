using NUnit.Framework;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain.Services;
using PuzzleGame.Domain.Interfaces;
using System.Collections.Generic;

namespace PuzzleGame.Domain.Tests.Services
{
    public class LevelGeneratorTests
    {
        private readonly ILevelGenerator _generator = new DifficultyBasedLevelGenerator();
        private readonly DomainColor[] _palette = new DomainColor[]
        {
            new DomainColor(1, 0, 0), // red
            new DomainColor(0, 1, 0), // green
            new DomainColor(0, 0, 1), // blue
        };

        [Test]
        public void Generate_ProducesCorrectMoldCount()
        {
            var result = _generator.Generate(5, 4, 1, _palette, Difficulty.Medium, seed: 42);
            Assert.That(result.Count, Is.EqualTo(5));
        }

        [Test]
        public void Generate_TotalLayers_EqualsNumColorsTimesMaxLayers()
        {
            int MoldCount = 5;
            int maxLayers = 4;
            int emptyMolds = 1;
            int numColors = _palette.Length; // 3

            var result = _generator.Generate(MoldCount, maxLayers, emptyMolds, _palette, Difficulty.Medium, seed: 42);

            int total = 0;
            foreach (var layers in result) total += layers.Count;
            // 3 renk × 4 layer = 12
            Assert.That(total, Is.EqualTo(numColors * maxLayers));
        }

        [Test]
        public void Generate_EmptyMolds_AreNotPopulated()
        {
            var result = _generator.Generate(5, 4, 2, _palette, Difficulty.Medium, seed: 42);
            int emptyCount = 0;
            foreach (var layers in result)
                if (layers.Count == 0) emptyCount++;
            Assert.That(emptyCount, Is.EqualTo(2));
        }

        [Test]
        public void Generate_EachColor_AppearsExactTimes()
        {
            var result = _generator.Generate(5, 4, 2, _palette, Difficulty.Medium, seed: 42);

            int redCount = 0, greenCount = 0, blueCount = 0;
            foreach (var layers in result)
                foreach (var layer in layers)
                {
                    if (layer.Color.Equals(_palette[0])) redCount++;
                    else if (layer.Color.Equals(_palette[1])) greenCount++;
                    else if (layer.Color.Equals(_palette[2])) blueCount++;
                }
            Assert.That(redCount, Is.EqualTo(4));
            Assert.That(greenCount, Is.EqualTo(4));
            Assert.That(blueCount, Is.EqualTo(4));
        }

        [Test]
        public void Generate_EachMold_HasSingleColor()
        {
            // Shuffled Molds should contain mixed colors
            var result = _generator.Generate(3, 4, 0, _palette, Difficulty.Medium, seed: 42);
            bool hasMixedMold = false;
            foreach (var layers in result)
            {
                if (layers.Count < 2) continue;
                var first = layers[0].Color;
                for (int i = 1; i < layers.Count; i++)
                {
                    if (!layers[i].Color.Equals(first))
                    {
                        hasMixedMold = true;
                        break;
                    }
                }
            }
            Assert.That(hasMixedMold, Is.True, "Shuffled Molds should contain mixed colors");
        }

        [Test]
        public void Generate_SameSeed_ProducesSameResult()
        {
            var a = _generator.Generate(5, 4, 1, _palette, Difficulty.Medium, seed: 12345);
            var b = _generator.Generate(5, 4, 1, _palette, Difficulty.Medium, seed: 12345);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.That(a[i].Count, Is.EqualTo(b[i].Count));
                for (int j = 0; j < a[i].Count; j++)
                {
                    Assert.That(a[i][j].Color.Equals(b[i][j].Color), Is.True, $"Mismatch at Mold {i} layer {j}");
                }
            }
        }

        [Test]
        public void Generate_DifferentSeeds_ProduceDifferentResults()
        {
            var a = _generator.Generate(5, 4, 1, _palette, Difficulty.Medium, seed: 1);
            var b = _generator.Generate(5, 4, 1, _palette, Difficulty.Medium, seed: 999);

            var flatA = new List<DomainColor>();
            var flatB = new List<DomainColor>();
            foreach (var layers in a) foreach (var l in layers) flatA.Add(l.Color);
            foreach (var layers in b) foreach (var l in layers) flatB.Add(l.Color);

            Assert.That(flatA.Count, Is.EqualTo(flatB.Count), "Total layer count must match");
            bool differ = false;
            for (int i = 0; i < flatA.Count && !differ; i++)
                if (!flatA[i].Equals(flatB[i])) differ = true;
            Assert.That(differ, Is.True, "Different seeds should produce different distributions");
        }

        [Test]
        public void Generate_LayerAmount_IsOneOverMaxLayers()
        {
            var result = _generator.Generate(3, 4, 0, _palette, Difficulty.Medium, seed: 42);
            foreach (var layers in result)
                foreach (var layer in layers)
                    Assert.That(layer.Amount, Is.EqualTo(0.25f).Within(0.001f));
        }

        [Test]
        public void Generate_TooFewColors_StillProduces()
        {
            var small = new DomainColor[] { new DomainColor(1, 0, 0) };
            var result = _generator.Generate(4, 2, 1, small, Difficulty.Medium, seed: 42);
            Assert.That(result.Count, Is.EqualTo(4));
            int nonEmpty = 0;
            foreach (var l in result) if (l.Count > 0) nonEmpty++;
            Assert.That(nonEmpty, Is.EqualTo(1));
        }

        [Test]
        public void Generate_ZeroMaxLayers_StillProduces()
        {
            var result = _generator.Generate(3, 0, 1, _palette, Difficulty.Medium, seed: 42);
            Assert.That(result.Count, Is.EqualTo(3));
            int total = 0;
            foreach (var layers in result) total += layers.Count;
            Assert.That(total, Is.EqualTo(0));
        }
    }
}
