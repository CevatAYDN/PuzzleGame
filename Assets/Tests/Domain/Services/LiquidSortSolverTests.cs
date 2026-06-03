using System.Collections.Generic;
using NUnit.Framework;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain.Services;

namespace PuzzleGame.Tests.Domain.Services
{
    /// <summary>
    /// NUnit tests for LiquidSortSolver.
    /// Pure C# — no MonoBehaviour, no Unity runtime required.
    /// Fix #18: Previously untested critical BFS logic.
    /// Fix #5: Tests verify that ComputeCanonicalKey symmetry elimination works correctly
    ///         (bottle order does not affect solvability detection).
    /// </summary>
    [TestFixture]
    public class LiquidSortSolverTests
    {
        private static DomainColor Red   => new DomainColor(1f, 0f, 0f, 1f);
        private static DomainColor Blue  => new DomainColor(0f, 0f, 1f, 1f);
        private static DomainColor Green => new DomainColor(0f, 1f, 0f, 1f);

        private static LiquidLayer L(DomainColor c, float amount = 0.25f) => new LiquidLayer(c, amount);

        // ─── Already Solved ────────────────────────────────────────────────────────

        [Test]
        public void Solve_AlreadySolved_TwoBottles_ReturnsEmptyPath()
        {
            var bottles = new List<List<LiquidLayer>>
            {
                new() { L(Red), L(Red) },
                new()                    // empty
            };

            var result = LiquidSortSolver.Solve(bottles, maxLayers: 2);

            Assert.IsTrue(result.IsSolvable, "Should detect already-solved state.");
            Assert.AreEqual(0, result.SolutionPath.Count, "No moves needed.");
        }

        [Test]
        public void Solve_AlreadySolved_MultipleColors_ReturnsEmptyPath()
        {
            var bottles = new List<List<LiquidLayer>>
            {
                new() { L(Red), L(Red), L(Red), L(Red) },
                new() { L(Blue), L(Blue), L(Blue), L(Blue) },
                new()
            };

            var result = LiquidSortSolver.Solve(bottles, maxLayers: 4);

            Assert.IsTrue(result.IsSolvable);
            Assert.AreEqual(0, result.SolutionPath.Count);
        }

        // ─── Simple Solvable ───────────────────────────────────────────────────────

        [Test]
        public void Solve_SimplePour_OneMove_ReturnsSolvable()
        {
            // Blue is already complete. Red is split across two bottles (1 layer each).
            // Pouring Red from one bottle to the other completes Red in 1 move.
            var bottles = new List<List<LiquidLayer>>
            {
                new() { L(Blue), L(Blue) },
                new() { L(Red) },
                new() { L(Red) }
            };

            var result = LiquidSortSolver.Solve(bottles, maxLayers: 2);

            Assert.IsTrue(result.IsSolvable, "Puzzle should be solvable.");
            Assert.Greater(result.VisitedStatesCount, 0, "BFS should have explored states.");
        }

        [Test]
        public void Solve_TwoColors_TwoBottles_Solvable()
        {
            var bottles = new List<List<LiquidLayer>>
            {
                new() { L(Red), L(Blue) },
                new() { L(Blue), L(Red) },
                new(),
                new()
            };

            var result = LiquidSortSolver.Solve(bottles, maxLayers: 2);

            Assert.IsTrue(result.IsSolvable);
            Assert.IsNotNull(result.SolutionPath);
        }

        // ─── Unsolvable ────────────────────────────────────────────────────────────

        [Test]
        public void Solve_Unsolvable_OneBottleNoEmpty_ReturnsFalse()
        {
            // Two colors in one bottle, no empty bottle to pour to → impossible
            var bottles = new List<List<LiquidLayer>>
            {
                new() { L(Red), L(Blue) }
            };

            var result = LiquidSortSolver.Solve(bottles, maxLayers: 2);

            Assert.IsFalse(result.IsSolvable, "No empty bottle → cannot separate colors.");
        }

        [Test]
        public void Solve_MaxVisitedStatesExceeded_ReturnsFalse()
        {
            // Complex puzzle, limit BFS to 1 state → should not solve
            var bottles = new List<List<LiquidLayer>>
            {
                new() { L(Red), L(Blue), L(Green), L(Red) },
                new() { L(Green), L(Red), L(Blue), L(Green) },
                new() { L(Blue), L(Green), L(Red), L(Blue) },
                new(),
                new()
            };

            var options = new LiquidSortSolver.LiquidSortSolverOptions { MaxVisitedStates = 1 };
            var result = LiquidSortSolver.Solve(bottles, maxLayers: 4, options);

            // With max 1 state we can't solve → IsSolvable should be false
            Assert.IsFalse(result.IsSolvable, "BFS cut off after 1 state.");
        }

        // ─── Fix #5: Symmetry/Hash Tests ──────────────────────────────────────────

        [Test]
        public void Solve_BottleOrderIndependent_SameResult()
        {
            // Two orderings of the same puzzle should give the same solvability result
            var bottlesA = new List<List<LiquidLayer>>
            {
                new() { L(Red), L(Blue) },
                new() { L(Blue), L(Red) },
                new(),
                new()
            };

            var bottlesB = new List<List<LiquidLayer>>
            {
                new() { L(Blue), L(Red) },  // swapped
                new() { L(Red), L(Blue) },
                new(),
                new()
            };

            var resultA = LiquidSortSolver.Solve(bottlesA, maxLayers: 2);
            var resultB = LiquidSortSolver.Solve(bottlesB, maxLayers: 2);

            Assert.AreEqual(resultA.IsSolvable, resultB.IsSolvable,
                "Fix #5: Symmetry should not affect solvability detection.");
        }

        // ─── Guard / Argument Validation ───────────────────────────────────────────

        [Test]
        public void Solve_NullBottles_ThrowsArgumentNullException()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                LiquidSortSolver.Solve(null, maxLayers: 4));
        }

        [Test]
        public void Solve_InvalidMaxLayers_ThrowsArgumentOutOfRange()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                LiquidSortSolver.Solve(new List<List<LiquidLayer>> { new() }, maxLayers: 0));
        }

        [Test]
        public void Solve_EmptyBottleList_ReturnsFalse()
        {
            var result = LiquidSortSolver.Solve(new List<List<LiquidLayer>>(), maxLayers: 4);
            Assert.IsFalse(result.IsSolvable, "Empty puzzle has no state to solve.");
        }
    }
}
