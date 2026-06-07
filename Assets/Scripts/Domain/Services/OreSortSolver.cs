using System;
using System.Buffers;
using System.Collections.Generic;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Domain.Services
{
    /// <summary>
    /// Evaluates playability and solves Ore Sort puzzles using Breadth-First Search.
    /// Pure C# — no UnityEngine dependency. Suitable for headless CI / cloud validation.
    ///
    /// Fix #5: ComputeCanonicalKey no longer encodes Mold index into the hash key.
    ///         Sorting is now purely content-based — symmetry elimination works correctly.
    /// Fix #13: BFS hot path uses ArrayPool&lt;int[]&gt; to reduce GC pressure.
    /// </summary>
    public class OreSortSolver
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

        public class OreSortSolverOptions
        {
            public int MaxVisitedStates { get; set; } = ForgeConstants.SolverMaxVisitedStates;
            public float ColorTolerance  { get; set; } = ForgeConstants.ColorMatchEpsilon;
            public bool EnableMultiLayerCast { get; set; } = false;
            public bool CastConsecutiveOnly { get; set; } = true;
            public int MinConsecutiveForCast { get; set; } = 1;
        }

        /// <summary>
        /// Solves the puzzle starting from the given initial Mold layout.
        /// </summary>
        /// <exception cref="ArgumentNullException">If initialMolds is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If maxLayers is out of range.</exception>
        public static SolverResult Solve(
            List<List<OreLayer>> initialMolds,
            int maxLayers,
            OreSortSolverOptions options = null)
        {
            options ??= new OreSortSolverOptions();

            if (initialMolds == null)
                throw new ArgumentNullException(nameof(initialMolds));

            if (initialMolds.Count == 0)
                return new SolverResult { IsSolvable = false, SolutionPath = new List<Move>(), VisitedStatesCount = 0 };

            if (maxLayers < 1 || maxLayers > ForgeConstants.MaxLayers)
                throw new ArgumentOutOfRangeException(nameof(maxLayers), maxLayers,
                    $"maxLayers must be in [1, {ForgeConstants.MaxLayers}].");

            float colorTolerance = options.ColorTolerance;
            int maxVisited = Math.Max(1, options.MaxVisitedStates);

            // 1. Map colors to integer IDs (using color match tolerance)
            var uniqueColors = new List<DomainColor>();
            int GetColorId(DomainColor c)
            {
                // Guard against NaN/Inf components: comparisons with NaN are always false,
                // so a single NaN color would never match itself and would be re-inserted
                // into uniqueColors on every call, polluting the BFS state space.
                if (!float.IsFinite(c.R) || !float.IsFinite(c.G) ||
                    !float.IsFinite(c.B) || !float.IsFinite(c.A))
                    return 0; // sentinel: invalid color, never matches a valid one downstream

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

            int MoldCount = initialMolds.Count;
            int[][] initial = new int[MoldCount][];
            for (int i = 0; i < MoldCount; i++)
            {
                var layers = initialMolds[i] ?? throw new ArgumentException(
                    $"initialMolds[{i}] is null.", nameof(initialMolds));
                if (layers.Count > maxLayers)
                    throw new ArgumentException(
                        $"initialMolds[{i}] has {layers.Count} layers but maxLayers is {maxLayers}.",
                        nameof(initialMolds));
                initial[i] = new int[layers.Count];
                for (int j = 0; j < layers.Count; j++)
                    initial[i][j] = GetColorId(layers[j].Color);
            }

            // 2. BFS
            var startNode = new StateNode(initial, new Move(-1, -1), null, MoldCount);
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

                for (int i = 0; i < current.Molds.Length; i++)
                {
                    var source = current.Molds[i];
                    if (source.Length == 0) continue;

                    // Skip complete Molds (full + uniform color)
                    if (source.Length == maxLayers)
                    {
                        bool allSame = true;
                        for (int k = 1; k < source.Length; k++)
                            if (source[k] != source[0]) { allSame = false; break; }
                        if (allSame) continue;
                    }

                    for (int j = 0; j < current.Molds.Length; j++)
                    {
                        if (i == j) continue;
                        var target = current.Molds[j];
                        if (target.Length >= maxLayers) continue;

                        int sourceTopColor = source[source.Length - 1];
                        if (target.Length > 0 && target[target.Length - 1] != sourceTopColor) continue;

                        // Count consecutive same-color layers from top of source according to options
                        int countToCast = 0;
                        int idx = source.Length - 1;

                        if (!options.EnableMultiLayerCast)
                        {
                            if (target.Length < maxLayers)
                                countToCast = 1;
                        }
                        else
                        {
                            while (idx >= 0 && source[idx] == sourceTopColor && (target.Length + countToCast) < maxLayers)
                            {
                                countToCast++;
                                idx--;
                            }

                            if (options.CastConsecutiveOnly && countToCast < options.MinConsecutiveForCast)
                            {
                                countToCast = 0;
                            }
                        }

                        if (countToCast == 0) continue;

                        var ownedMolds = ArrayPool<int[]>.Shared.Rent(current.Molds.Length);
                        int[] srcArray = null;
                        int[] tgtArray = null;

                        try
                        {
                            for (int k = 0; k < current.Molds.Length; k++)
                            {
                                if (k == i)
                                {
                                    int newSrcLen = source.Length - countToCast;
                                    if (newSrcLen == 0)
                                    {
                                        ownedMolds[k] = Array.Empty<int>();
                                    }
                                    else
                                    {
                                        srcArray = ArrayPool<int>.Shared.Rent(newSrcLen);
                                        Array.Copy(source, srcArray, newSrcLen);
                                        ownedMolds[k] = srcArray;
                                    }
                                }
                                else if (k == j)
                                {
                                    int newTgtLen = target.Length + countToCast;
                                    tgtArray = ArrayPool<int>.Shared.Rent(newTgtLen);
                                    Array.Copy(target, tgtArray, target.Length);
                                    for (int p = 0; p < countToCast; p++)
                                        tgtArray[target.Length + p] = sourceTopColor;
                                    ownedMolds[k] = tgtArray;
                                }
                                else
                                {
                                    ownedMolds[k] = current.Molds[k];
                                }
                            }

                            var nextNode = new StateNode(ownedMolds, new Move(i, j), current, MoldCount);
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
                            if (srcArray != null && srcArray.Length > 0)
                                ArrayPool<int>.Shared.Return(srcArray);
                            if (tgtArray != null && tgtArray.Length > 0)
                                ArrayPool<int>.Shared.Return(tgtArray);
                            ArrayPool<int[]>.Shared.Return(ownedMolds);
                        }
                    }
                }
            }

            return new SolverResult { IsSolvable = false, SolutionPath = new List<Move>(), VisitedStatesCount = visitedCount };
        }

        // ─────────────────────────────────────────────────────────────────
        // StateNode: BFS node with a packing-friendly canonical hash key.
        //
        // Fix #5: Mold index is NO LONGER encoded into the key.
        // Previously: key |= ((ulong)b & 0xFFFuL) << 48  caused Array.Sort()
        // to be index-based rather than content-based.
        // Now: only color layer data is packed — isomorphic states hash identically.
        // ─────────────────────────────────────────────────────────────────
        private sealed class StateNode
        {
            public int[][] Molds;
            public Move LastMove;
            public StateNode Parent;
            private readonly int _MoldCount;

            public StateNode(int[][] molds, Move lastMove, StateNode parent, int moldCount)
            {
                this.Molds = molds;
                LastMove = lastMove;
                Parent = parent;
                _MoldCount = moldCount;
            }

            public bool IsSolved(int maxLayers)
            {
                foreach (var Mold in Molds)
                {
                    if (Mold.Length == 0) continue;
                    if (Mold.Length != maxLayers) return false;
                    int first = Mold[0];
                    for (int i = 1; i < Mold.Length; i++)
                        if (Mold[i] != first) return false;
                }
                return true;
            }

            /// <summary>
            /// Computes a symmetric canonical hash key.
            /// 4 bits per layer slot (max 16 colors; 12 in practice).
            ///
            /// Fix #5: Index NOT encoded — sorting is purely content-based.
            /// Isomorphic states (same Molds, different order) now hash identically.
            /// </summary>
            public ulong ComputeCanonicalKey(int maxLayers)
            {
                var MoldKeys = new ulong[_MoldCount];
                for (int b = 0; b < _MoldCount; b++)
                {
                    var Mold = Molds[b];
                    ulong key = 0;
                    for (int layerIdx = 0; layerIdx < Mold.Length; layerIdx++)
                    {
                        // 4 bits per layer — supports up to 15 distinct colors.
                        key |= ((ulong)Mold[layerIdx] & 0x0FuL) << (layerIdx * 4);
                    }
                    // Fix #5: No index encoding here — content only.
                    MoldKeys[b] = key;
                }

                // Symmetry reduction: sort so isomorphic states hash identically.
                Array.Sort(MoldKeys);

                // XOR-fold into a single ulong.
                ulong result = 0;
                for (int b = 0; b < _MoldCount; b++)
                {
                    result ^= MoldKeys[b];
                    result = (result << 13) | (result >> (64 - 13));
                }
                return result;
            }
        }
    }
}
