using System;
using System.Collections.Generic;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Domain.Services
{
    /// <summary>
    /// Evaluates playability and solves Ore Sort puzzles using Breadth-First Search.
    /// Pure C# — no UnityEngine dependency. Suitable for headless CI / cloud validation.
    ///
    /// Fix #5:  ComputeCanonicalKey no longer encodes Mold index into the hash key.
    ///          Sorting is now purely content-based — symmetry elimination works correctly.
    /// Fix #13: BFS hot path previously used ArrayPool&lt;int[]&gt; to reduce GC pressure.
    /// Fix #19: REMOVED ArrayPool — the original implementation rented pooled buffers and
    ///          returned them inside the `finally` block, but those same buffers were
    ///          already referenced by `nextNode` which had been enqueued (or kept as
    ///          `Parent` for future nodes). Result: a use-after-return where subsequent
    ///          `Rent()` calls could hand out the same memory to a *different* node,
    ///          corrupting the BFS state space and producing non-deterministic solvability
    ///          results. BFS state space is bounded by 16 molds × 4 layers (= 64 ints
    ///          per snapshot) and the solver only runs at level-load time, so the GC
    ///          savings were negligible. Plain allocation is correct, deterministic, and
    ///          keeps the data-flow obvious.
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
            public OreColor[] CorkColors { get; set; }
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

            static bool ColorsMatch(DomainColor a, DomainColor b, float tolerance) =>
                Math.Abs(a.R - b.R) < tolerance &&
                Math.Abs(a.G - b.G) < tolerance &&
                Math.Abs(a.B - b.B) < tolerance &&
                Math.Abs(a.A - b.A) < tolerance;

            int GetColorId(DomainColor c)
            {
                if (!float.IsFinite(c.R) || !float.IsFinite(c.G) ||
                    !float.IsFinite(c.B) || !float.IsFinite(c.A))
                    return -1;

                for (int i = 0; i < uniqueColors.Count; i++)
                {
                    if (ColorsMatch(uniqueColors[i], c, colorTolerance))
                        return i;
                }
                uniqueColors.Add(c);
                return uniqueColors.Count - 1;
            }

            int MoldCount = initialMolds.Count;

            // Map cork colors to IDs
            int?[] corkIds = null;
            if (options.CorkColors != null && options.CorkColors.Length == MoldCount)
            {
                corkIds = new int?[MoldCount];
                for (int i = 0; i < MoldCount; i++)
                {
                    if (options.CorkColors[i] != OreColor.None)
                    {
                        var domainColor = options.CorkColors[i].ToDefaultDomainColor();
                        corkIds[i] = GetColorId(domainColor);
                    }
                }
            }

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

                        // Cork check: if target has cork, source top must match cork color
                        if (corkIds != null && corkIds[j].HasValue)
                        {
                            if (sourceTopColor != corkIds[j].Value) continue;
                        }

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

                        // Fix #19: Plain allocation. Bounded by 16 molds × 4 layers
                        // (= max 16 int[] per snapshot, each ≤ 4 ints). Allocates
                        // only on the path that produces a brand-new state.
                        var nextMolds = new int[current.Molds.Length][];
                        for (int k = 0; k < current.Molds.Length; k++)
                        {
                            if (k == i)
                            {
                                int newSrcLen = source.Length - countToCast;
                                if (newSrcLen == 0)
                                {
                                    nextMolds[k] = Array.Empty<int>();
                                }
                                else
                                {
                                    var newSrc = new int[newSrcLen];
                                    Array.Copy(source, newSrc, newSrcLen);
                                    nextMolds[k] = newSrc;
                                }
                            }
                            else if (k == j)
                            {
                                int newTgtLen = target.Length + countToCast;
                                var newTgt = new int[newTgtLen];
                                Array.Copy(target, newTgt, target.Length);
                                for (int p = 0; p < countToCast; p++)
                                    newTgt[target.Length + p] = sourceTopColor;
                                nextMolds[k] = newTgt;
                            }
                            else
                            {
                                // Unchanged: share the parent's array reference. Read-only after enqueue.
                                nextMolds[k] = current.Molds[k];
                            }
                        }

                        var nextNode = new StateNode(nextMolds, new Move(i, j), current, MoldCount);
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
                        // Fix #20: Offset color ID by +1 to differentiate Color ID 0 from an empty layer!
                        key |= (((ulong)Mold[layerIdx] + 1) & 0x0FuL) << (layerIdx * 4);
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
