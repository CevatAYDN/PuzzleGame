using System.Collections.Generic;
using NUnit.Framework;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain.Services;

namespace PuzzleGame.Tests.Domain.Services
{
    /// <summary>
    /// NUnit tests for OreSortSolver.
    /// Pure C# — no MonoBehaviour, no Unity runtime required.
    /// Fix #18: Previously untested critical BFS logic.
    /// Fix #5: Tests verify that ComputeCanonicalKey symmetry elimination works correctly
    ///         (Mold order does not affect solvability detection).
    /// </summary>
    [TestFixture]
    public class OreSortSolverTests
    {
        private static DomainColor Red   => new DomainColor(1f, 0f, 0f, 1f);
        private static DomainColor Blue  => new DomainColor(0f, 0f, 1f, 1f);
        private static DomainColor Green => new DomainColor(0f, 1f, 0f, 1f);

        private static OreLayer L(DomainColor c, float amount = 0.25f) => new OreLayer(c, amount);

        // ─── Already Solved ────────────────────────────────────────────────────────

        [Test]
        public void Solve_AlreadySolved_TwoMolds_ReturnsEmptyPath()
        {
            var Molds = new List<List<OreLayer>>
            {
                new() { L(Red), L(Red) },
                new()                    // empty
            };

            var result = OreSortSolver.Solve(Molds, maxLayers: 2);

            Assert.IsTrue(result.IsSolvable, "Should detect already-solved state.");
            Assert.AreEqual(0, result.SolutionPath.Count, "No moves needed.");
        }

        [Test]
        public void Solve_AlreadySolved_MultipleColors_ReturnsEmptyPath()
        {
            var Molds = new List<List<OreLayer>>
            {
                new() { L(Red), L(Red), L(Red), L(Red) },
                new() { L(Blue), L(Blue), L(Blue), L(Blue) },
                new()
            };

            var result = OreSortSolver.Solve(Molds, maxLayers: 4);

            Assert.IsTrue(result.IsSolvable);
            Assert.AreEqual(0, result.SolutionPath.Count);
        }

        // ─── Simple Solvable ───────────────────────────────────────────────────────

        [Test]
        public void Solve_SimpleCast_OneMove_ReturnsSolvable()
        {
            // Blue is already complete. Red is split across two Molds (1 layer each).
            // Casting Red from one Mold to the other completes Red in 1 move.
            var Molds = new List<List<OreLayer>>
            {
                new() { L(Blue), L(Blue) },
                new() { L(Red) },
                new() { L(Red) }
            };

            var result = OreSortSolver.Solve(Molds, maxLayers: 2);

            Assert.IsTrue(result.IsSolvable, "Puzzle should be solvable.");
            Assert.Greater(result.VisitedStatesCount, 0, "BFS should have explored states.");
        }

        [Test]
        public void Solve_TwoColors_TwoMolds_Solvable()
        {
            var Molds = new List<List<OreLayer>>
            {
                new() { L(Red), L(Blue) },
                new() { L(Blue), L(Red) },
                new(),
                new()
            };

            var result = OreSortSolver.Solve(Molds, maxLayers: 2);

            Assert.IsTrue(result.IsSolvable);
            Assert.IsNotNull(result.SolutionPath);
        }

        // ─── Unsolvable ────────────────────────────────────────────────────────────

        [Test]
        public void Solve_Unsolvable_OneMoldNoEmpty_ReturnsFalse()
        {
            // Two colors in one Mold, no empty Mold to Cast to → impossible
            var Molds = new List<List<OreLayer>>
            {
                new() { L(Red), L(Blue) }
            };

            var result = OreSortSolver.Solve(Molds, maxLayers: 2);

            Assert.IsFalse(result.IsSolvable, "No empty Mold → cannot separate colors.");
        }

        [Test]
        public void Solve_MaxVisitedStatesExceeded_ReturnsFalse()
        {
            // Complex puzzle, limit BFS to 1 state → should not solve
            var Molds = new List<List<OreLayer>>
            {
                new() { L(Red), L(Blue), L(Green), L(Red) },
                new() { L(Green), L(Red), L(Blue), L(Green) },
                new() { L(Blue), L(Green), L(Red), L(Blue) },
                new(),
                new()
            };

            var options = new OreSortSolver.OreSortSolverOptions { MaxVisitedStates = 1 };
            var result = OreSortSolver.Solve(Molds, maxLayers: 4, options);

            // With max 1 state we can't solve → IsSolvable should be false
            Assert.IsFalse(result.IsSolvable, "BFS cut off after 1 state.");
        }

        // ─── Fix #5: Symmetry/Hash Tests ──────────────────────────────────────────

        [Test]
        public void Solve_MoldOrderIndependent_SameResult()
        {
            // Two orderings of the same puzzle should give the same solvability result
            var MoldsA = new List<List<OreLayer>>
            {
                new() { L(Red), L(Blue) },
                new() { L(Blue), L(Red) },
                new(),
                new()
            };

            var MoldsB = new List<List<OreLayer>>
            {
                new() { L(Blue), L(Red) },  // swapped
                new() { L(Red), L(Blue) },
                new(),
                new()
            };

            var resultA = OreSortSolver.Solve(MoldsA, maxLayers: 2);
            var resultB = OreSortSolver.Solve(MoldsB, maxLayers: 2);

            Assert.AreEqual(resultA.IsSolvable, resultB.IsSolvable,
                "Fix #5: Symmetry should not affect solvability detection.");
        }

        // ─── Guard / Argument Validation ───────────────────────────────────────────

        [Test]
        public void Solve_NullMolds_ThrowsArgumentNullException()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                OreSortSolver.Solve(null, maxLayers: 4));
        }

        [Test]
        public void Solve_InvalidMaxLayers_ThrowsArgumentOutOfRange()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                OreSortSolver.Solve(new List<List<OreLayer>> { new() }, maxLayers: 0));
        }

        [Test]
        public void Solve_EmptyMoldList_ReturnsFalse()
        {
            var result = OreSortSolver.Solve(new List<List<OreLayer>>(), maxLayers: 4);
            Assert.IsFalse(result.IsSolvable, "Empty puzzle has no state to solve.");
        }
    }
}
