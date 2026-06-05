using NUnit.Framework;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Services;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain.Services;
using PuzzleGame.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace PuzzleGame.Domain.Tests.Services
{
    public class LevelGeneratorSolvabilityTests
    {
        private readonly ILevelGenerator _generator = new DifficultyBasedLevelGenerator();
        private readonly DomainColor[] _palette = new DomainColor[]
        {
            new DomainColor(1f, 0f, 0f),
            new DomainColor(0f, 1f, 0f),
            new DomainColor(0f, 0f, 1f),
            new DomainColor(1f, 1f, 0f),
        };

        [Test]
        public void GenerateSolvable_EasyConfig_ReturnsSolvableLayout()
        {
            var (layout, isSolvable) = _generator.GenerateSolvable(
                MoldCount: 5,
                maxLayers: 4,
                emptyMoldCount: 1,
                colorPalette: _palette,
                difficulty: Difficulty.Easy,
                seed: 12345,
                maxAttempts: 5);

            Assert.That(layout, Is.Not.Null);
            Assert.That(layout.Count, Is.EqualTo(5));
            Assert.That(isSolvable, Is.True,
                $"GenerateSolvable should produce a solvable layout for Easy difficulty (seed=12345).");
        }

        [Test]
        public void GenerateSolvable_MediumConfig_ReturnsSolvableLayout()
        {
            var (layout, isSolvable) = _generator.GenerateSolvable(
                5, 4, 1, _palette, Difficulty.Medium, seed: 999, 5);

            Assert.That(layout.Count, Is.EqualTo(5));
            Assert.That(isSolvable, Is.True);
        }

        [Test]
        public void GenerateSolvable_HardConfig_ReturnsSolvableLayout()
        {
            var (layout, isSolvable) = _generator.GenerateSolvable(
                6, 4, 2, _palette, Difficulty.Hard, seed: 7777, 8);

            Assert.That(layout.Count, Is.EqualTo(6));
            Assert.That(isSolvable, Is.True,
                $"Hard difficulty should be solvable within retry budget. Layout: {string.Join(",", layout.Select(l => l.Count))}");
        }

        [Test]
        public void GenerateSolvable_ExpertConfig_HighRetryBudget_ReturnsSolvable()
        {
            var (layout, isSolvable) = _generator.GenerateSolvable(
                6, 4, 2, _palette, Difficulty.Expert, seed: 4242, 16);

            Assert.That(layout.Count, Is.EqualTo(6));
            Assert.That(isSolvable, Is.True,
                "Expert difficulty should yield a solvable layout with sufficient retry budget.");
        }

        [Test]
        public void GenerateSolvable_ZeroRetryBudget_StillReturnsLayout()
        {
            var (layout, isSolvable) = _generator.GenerateSolvable(
                5, 4, 1, _palette, Difficulty.Medium, seed: 1, maxAttempts: 1);

            Assert.That(layout, Is.Not.Null);
            Assert.That(layout.Count, Is.EqualTo(5));
        }

        [Test]
        public void GenerateSolvable_TrivialConfig_FirstAttemptSucceeds()
        {
            var (layout, isSolvable) = _generator.GenerateSolvable(
                4, 4, 1, _palette, Difficulty.Trivial, seed: 1, maxAttempts: 1);

            Assert.That(isSolvable, Is.True,
                "Trivial difficulty (low mix factor) should be solvable on first try.");
        }

        [Test]
        public void GenerateSolvable_DifferentSeeds_CanProduceDifferentLayouts()
        {
            var (layout1, _) = _generator.GenerateSolvable(
                5, 4, 1, _palette, Difficulty.Medium, seed: 1, maxAttempts: 1);
            var (layout2, _) = _generator.GenerateSolvable(
                5, 4, 1, _palette, Difficulty.Medium, seed: 999, maxAttempts: 1);

            var flat1 = layout1.SelectMany(l => l).Select(l => l.Color).ToList();
            var flat2 = layout2.SelectMany(l => l).Select(l => l.Color).ToList();
            bool differ = false;
            for (int i = 0; i < flat1.Count && !differ; i++)
            {
                if (!flat1[i].Equals(flat2[i])) differ = true;
            }
            Assert.That(differ, Is.True, "Different seeds should produce different layouts.");
        }

        [Test]
        public void GenerateSolvable_SolvableLayout_VerifiesViaOreSortSolver()
        {
            var (layout, isSolvable) = _generator.GenerateSolvable(
                5, 4, 1, _palette, Difficulty.Medium, seed: 12345, maxAttempts: 5);

            if (isSolvable)
            {
                var result = OreSortSolver.Solve(layout, 4);
                Assert.That(result.IsSolvable, Is.True,
                    "When GenerateSolvable reports success, the OreSortSolver should also confirm solvability.");
            }
        }
    }
}
