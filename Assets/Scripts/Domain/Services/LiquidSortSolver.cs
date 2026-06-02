using System;
using System.Collections.Generic;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Domain.Services
{
    /// <summary>
    /// Evaluates playability and solves Liquid Sort puzzles using Breadth-First Search.
    /// Pure C# — no UnityEngine dependency. Suitable for headless CI / cloud validation.
    /// 
    /// Limits (see <see cref="BottleConstants.SolverMaxVisitedStates"/>) are
    /// config-overridable via the GameConfig passed in.
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
            {
                throw new ArgumentNullException(nameof(initialBottles));
            }
            if (initialBottles.Count == 0)
            {
                return new SolverResult { IsSolvable = false, SolutionPath = null, VisitedStatesCount = 0 };
            }
            if (maxLayers < 1 || maxLayers > BottleConstants.MaxLayers)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxLayers), maxLayers,
                    $"maxLayers must be in [1, {BottleConstants.MaxLayers}].");
            }

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
                    {
                        return i + 1; // 1-based IDs
                    }
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
                {
                    throw new ArgumentException(
                        $"initialBottles[{i}] has {layers.Count} layers but maxLayers is {maxLayers}.",
                        nameof(initialBottles));
                }
                initial[i] = new int[layers.Count];
                for (int j = 0; j < layers.Count; j++)
                {
                    initial[i][j] = GetColorId(layers[j].Color);
                }
            }

            // 2. Perform BFS to find the shortest path
            var startNode = new StateNode(initial, new Move(-1, -1), null, bottleCount);
            if (startNode.IsSolved(maxLayers))
            {
                return new SolverResult { IsSolvable = true, SolutionPath = new List<Move>(), VisitedStatesCount = 1 };
            }

            var queue = new Queue<StateNode>();
            queue.Enqueue(startNode);

            var visited = new HashSet<ulong>(capacity: 1024);
            visited.Add(startNode.ComputeCanonicalKey(maxLayers));

            int visitedCount = 0;

            while (queue.Count > 0 && visitedCount < maxVisited)
            {
                var current = queue.Dequeue();
                visitedCount++;

                // Generate possible moves
                for (int i = 0; i < current.Bottles.Length; i++)
                {
                    var source = current.Bottles[i];
                    if (source.Length == 0) continue;

                    // Optimization: If a bottle is already complete (full and all one color), don't pour from it.
                    if (source.Length == maxLayers)
                    {
                        bool allSame = true;
                        for (int k = 1; k < source.Length; k++)
                        {
                            if (source[k] != source[0]) { allSame = false; break; }
                        }
                        if (allSame) continue;
                    }

                    for (int j = 0; j < current.Bottles.Length; j++)
                    {
                        if (i == j) continue;
                        var target = current.Bottles[j];
                        if (target.Length >= maxLayers) continue;

                        // Can pour? Target is empty OR target's top color matches source's top color
                        int sourceTopColor = source[source.Length - 1];
                        if (target.Length > 0 && target[target.Length - 1] != sourceTopColor) continue;

                        // How many layers can we pour?
                        // Under standard pour rules, we pop layers of the SAME color from source and push to target
                        // until either color changes, target is full, or source is empty.
                        int countToPour = 0;
                        int idx = source.Length - 1;
                        while (idx >= 0 && source[idx] == sourceTopColor && (target.Length + countToPour) < maxLayers)
                        {
                            countToPour++;
                            idx--;
                        }

                        if (countToPour == 0) continue;

                        // Create new state
                        int[][] nextBottles = new int[current.Bottles.Length][];
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
                                {
                                    nextBottles[k][target.Length + p] = sourceTopColor;
                                }
                            }
                            else
                            {
                                nextBottles[k] = current.Bottles[k];
                            }
                        }

                        var nextNode = new StateNode(nextBottles, new Move(i, j), current, bottleCount);
                        ulong hash = nextNode.ComputeCanonicalKey(maxLayers);
                        if (!visited.Contains(hash))
                        {
                            if (nextNode.IsSolved(maxLayers))
                            {
                                // Build path
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
                }
            }

            return new SolverResult { IsSolvable = false, SolutionPath = null, VisitedStatesCount = visitedCount };
        }

        // ─────────────────────────────────────────────────────────────────
        // StateNode: BFS node with a packing-friendly canonical hash key.
        // The hash packs each bottle's color IDs (4 bits each, since we have
        // max 12 colors in a standard puzzle) into a ulong. Empty slots
        // are encoded as 0. Bottles are sorted lexicographically to remove
        // symmetries, and the resulting key is XOR-folded to keep ulong-sized.
        // This eliminates all string allocations from the BFS hot path.
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
                    {
                        if (bottle[i] != first) return false;
                    }
                }
                return true;
            }

            /// <summary>
            /// Computes a symmetric canonical hash key.
            /// 4 bits per layer slot (max 16 colors supported; 12 in practice).
            /// Allocates a small sorted array per call (max <see cref="BottleConstants.MaxBottlesPerLevel"/> ulongs).
            /// </summary>
            public ulong ComputeCanonicalKey(int maxLayers)
            {
                // Encode each bottle as its own ulong, then sort lexicographically.
                var bottleKeys = new ulong[_bottleCount];
                for (int b = 0; b < _bottleCount; b++)
                {
                    var bottle = Bottles[b];
                    ulong key = 0;
                    for (int layerIdx = 0; layerIdx < bottle.Length; layerIdx++)
                    {
                        key |= ((ulong)bottle[layerIdx] & 0x0FuL) << (layerIdx * 4);
                    }
                    key |= ((ulong)b & 0xFFFuL) << 48;
                    bottleKeys[b] = key;
                }

                // Symmetry reduction: sort bottle keys so that isomorphic states
                // hash to the same value. O(n log n) with small n (max 16).
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
