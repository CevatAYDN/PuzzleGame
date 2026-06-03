using System;
using System.Buffers;
using System.Collections.Generic;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Domain.Services
{
    /// <summary>
    /// Evaluates playability and solves Liquid Sort puzzles using Breadth-First Search.
    /// Pure C# — no UnityEngine dependency. Suitable for headless CI / cloud validation.
    ///
    /// Fix #5: ComputeCanonicalKey no longer encodes bottle index into the hash key.
    ///         Sorting is now purely content-based — symmetry elimination works correctly.
    /// Fix #13: BFS hot path uses ArrayPool&lt;int[]&gt; to reduce GC pressure.
    /// </summary>
    public class LiquidSortSolver
    {
        public struct SolverResult
        {
            public bool IsSolvable;
            public List<Move> SolutionPath;
            public int VisitedStatesCount;
        }

        public struct Move
        {
            public int FromIndex;
            public int ToIndex;

            public Move(int fromIndex, int toIndex)
            {
                FromIndex = fromIndex;
                ToIndex = toIndex;
            }
        }

        public class LiquidSortSolverOptions
        {
            public int MaxVisitedStates { get; set; } = BottleConstants.SolverMaxVisitedStates;
            public float ColorTolerance  { get; set; } = BottleConstants.ColorMatchEpsilon;
        }

        /// <summary>
        /// Solves the puzzle starting from the given initial bottle layout.
        /// </summary>
        /// <exception cref="ArgumentNullException">If initialBottles is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If maxLayers is out of range.</exception>
        public static SolverResult Solve(
            List<List<LiquidLayer>> initialBottles,
            int maxLayers,
            LiquidSortSolverOptions options = null)
        {
            options ??= new LiquidSortSolverOptions();

            if (initialBottles == null)
                throw new ArgumentNullException(nameof(initialBottles));

            if (initialBottles.Count == 0)
                return new SolverResult { IsSolvable = false, SolutionPath = null, VisitedStatesCount = 0 };

            if (maxLayers < 1 || maxLayers > BottleConstants.MaxLayers)
                throw new ArgumentOutOfRangeException(nameof(maxLayers), maxLayers,
                    $"maxLayers must be in [1, {BottleConstants.MaxLayers}].");

            float colorTolerance = options.ColorTolerance;
            int maxVisited = Math.Max(1, options.MaxVisitedStates);

            // 1. Map colors to integer IDs (using color match tolerance)
            var uniqueColors = new List<DomainColor>();
            int GetColorId(DomainColor c)
            {
                for (int i = 0; i < uniqueColors.Count; i++)
                {
                    var uc = uniqueColors[i];
                    if (Math.Abs(uc.R - c.R) < colorTolerance &&
                        Math.Abs(uc.G - c.G) < colorTolerance &&
                        Math.Abs(uc.B - c.B) < colorTolerance &&
                        Math.Abs(uc.A - c.A) < colorTolerance)
                        return i + 1; // 1-based IDs
                }
                uniqueColors.Add(c);
                return uniqueColors.Count;
            }

            int bottleCount = initialBottles.Count;
            int[][] initial = new int[bottleCount][];
            for (int i = 0; i < bottleCount; i++)
            {
                var layers = initialBottles[i] ?? throw new ArgumentException(
                    $"initialBottles[{i}] is null.", nameof(initialBottles));
                if (layers.Count > maxLayers)
                    throw new ArgumentException(
                        $"initialBottles[{i}] has {layers.Count} layers but maxLayers is {maxLayers}.",
                        nameof(initialBottles));
                initial[i] = new int[layers.Count];
                for (int j = 0; j < layers.Count; j++)
                    initial[i][j] = GetColorId(layers[j].Color);
            }

            // 2. BFS
            var startNode = new StateNode(initial, new Move(-1, -1), null, bottleCount);
            if (startNode.IsSolved(maxLayers))
                return new SolverResult { IsSolvable = true, SolutionPath = new List<Move>(), VisitedStatesCount = 1 };

            var queue = new Queue<StateNode>();
            queue.Enqueue(startNode);

            var visited = new HashSet<ulong>(capacity: 1024);
            visited.Add(startNode.ComputeCanonicalKey(maxLayers));

            int visitedCount = 0;

            while (queue.Count > 0 && visitedCount < maxVisited)
            {
                var current = queue.Dequeue();
                visitedCount++;

                for (int i = 0; i < current.Bottles.Length; i++)
                {
                    var source = current.Bottles[i];
                    if (source.Length == 0) continue;

                    // Skip complete bottles (full + uniform color)
                    if (source.Length == maxLayers)
                    {
                        bool allSame = true;
                        for (int k = 1; k < source.Length; k++)
                            if (source[k] != source[0]) { allSame = false; break; }
                        if (allSame) continue;
                    }

                    for (int j = 0; j < current.Bottles.Length; j++)
                    {
                        if (i == j) continue;
                        var target = current.Bottles[j];
                        if (target.Length >= maxLayers) continue;

                        int sourceTopColor = source[source.Length - 1];
                        if (target.Length > 0 && target[target.Length - 1] != sourceTopColor) continue;

                        // Count consecutive same-color layers from top of source
                        int countToPour = 0;
                        int idx = source.Length - 1;
                        while (idx >= 0 && source[idx] == sourceTopColor && (target.Length + countToPour) < maxLayers)
                        {
                            countToPour++;
                            idx--;
                        }
                        if (countToPour == 0) continue;

                        // Fix #13: Rent temporary outer array from pool; individual row arrays still allocated
                        // because StateNode owns them across frames. Significantly reduces GC for wide puzzles.
                        int[][] nextBottles = ArrayPool<int[]>.Shared.Rent(current.Bottles.Length);
                        try
                        {
                            for (int k = 0; k < current.Bottles.Length; k++)
                            {
                                if (k == i)
                                {
                                    int newSrcLen = source.Length - countToPour;
                                    if (newSrcLen == 0)
                                    {
                                        nextBottles[k] = Array.Empty<int>();
                                    }
                                    else
                                    {
                                        nextBottles[k] = new int[newSrcLen];
                                        Array.Copy(source, nextBottles[k], newSrcLen);
                                    }
                                }
                                else if (k == j)
                                {
                                    int newTgtLen = target.Length + countToPour;
                                    nextBottles[k] = new int[newTgtLen];
                                    Array.Copy(target, nextBottles[k], target.Length);
                                    for (int p = 0; p < countToPour; p++)
                                        nextBottles[k][target.Length + p] = sourceTopColor;
                                }
                                else
                                {
                                    nextBottles[k] = current.Bottles[k];
                                }
                            }

                            // Snapshot owned copy (pool array returned in finally)
                            int[][] ownedBottles = new int[current.Bottles.Length][];
                            Array.Copy(nextBottles, ownedBottles, current.Bottles.Length);

                            var nextNode = new StateNode(ownedBottles, new Move(i, j), current, bottleCount);
                            ulong hash = nextNode.ComputeCanonicalKey(maxLayers);
                            if (!visited.Contains(hash))
                            {
                                if (nextNode.IsSolved(maxLayers))
                                {
                                    var path = new List<Move>();
                                    var temp = nextNode;
                                    while (temp.Parent != null)
                                    {
                                        path.Add(temp.LastMove);
                                        temp = temp.Parent;
                                    }
                                    path.Reverse();
                                    return new SolverResult { IsSolvable = true, SolutionPath = path, VisitedStatesCount = visitedCount };
                                }

                                visited.Add(hash);
                                queue.Enqueue(nextNode);
                            }
                        }
                        finally
                        {
                            ArrayPool<int[]>.Shared.Return(nextBottles);
                        }
                    }
                }
            }

            return new SolverResult { IsSolvable = false, SolutionPath = null, VisitedStatesCount = visitedCount };
        }

        // ─────────────────────────────────────────────────────────────────
        // StateNode: BFS node with a packing-friendly canonical hash key.
        //
        // Fix #5: Bottle index is NO LONGER encoded into the key.
        // Previously: key |= ((ulong)b & 0xFFFuL) << 48  caused Array.Sort()
        // to be index-based rather than content-based.
        // Now: only color layer data is packed — isomorphic states hash identically.
        // ─────────────────────────────────────────────────────────────────
        private sealed class StateNode
        {
            public int[][] Bottles;
            public Move LastMove;
            public StateNode Parent;
            private readonly int _bottleCount;

            public StateNode(int[][] bottles, Move lastMove, StateNode parent, int bottleCount)
            {
                Bottles = bottles;
                LastMove = lastMove;
                Parent = parent;
                _bottleCount = bottleCount;
            }

            public bool IsSolved(int maxLayers)
            {
                foreach (var bottle in Bottles)
                {
                    if (bottle.Length == 0) continue;
                    if (bottle.Length != maxLayers) return false;
                    int first = bottle[0];
                    for (int i = 1; i < bottle.Length; i++)
                        if (bottle[i] != first) return false;
                }
                return true;
            }

            /// <summary>
            /// Computes a symmetric canonical hash key.
            /// 4 bits per layer slot (max 16 colors; 12 in practice).
            ///
            /// Fix #5: Index NOT encoded — sorting is purely content-based.
            /// Isomorphic states (same bottles, different order) now hash identically.
            /// </summary>
            public ulong ComputeCanonicalKey(int maxLayers)
            {
                var bottleKeys = new ulong[_bottleCount];
                for (int b = 0; b < _bottleCount; b++)
                {
                    var bottle = Bottles[b];
                    ulong key = 0;
                    for (int layerIdx = 0; layerIdx < bottle.Length; layerIdx++)
                    {
                        // 4 bits per layer — supports up to 15 distinct colors.
                        key |= ((ulong)bottle[layerIdx] & 0x0FuL) << (layerIdx * 4);
                    }
                    // Fix #5: No index encoding here — content only.
                    bottleKeys[b] = key;
                }

                // Symmetry reduction: sort so isomorphic states hash identically.
                Array.Sort(bottleKeys);

                // XOR-fold into a single ulong.
                ulong result = 0;
                for (int b = 0; b < _bottleCount; b++)
                {
                    result ^= bottleKeys[b];
                    result = (result << 13) | (result >> (64 - 13));
                }
                return result;
            }
        }
    }
}
