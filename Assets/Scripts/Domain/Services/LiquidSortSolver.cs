using System;
using System.Collections.Generic;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Domain.Services
{
    /// <summary>
    /// Evaluates playability and solves Liquid Sort puzzles using Breadth-First Search.
    /// Employs symmetry reduction (bottle sorting) for high-performance execution.
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

        private class StateNode
        {
            public int[][] Bottles;
            public Move LastMove;
            public StateNode Parent;

            public StateNode(int[][] bottles, Move lastMove, StateNode parent)
            {
                Bottles = bottles;
                LastMove = lastMove;
                Parent = parent;
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

            public string GetHashKey()
            {
                var keys = new List<string>(Bottles.Length);
                foreach (var bottle in Bottles)
                {
                    if (bottle.Length == 0)
                    {
                        keys.Add("");
                    }
                    else
                    {
                        keys.Add(string.Join(",", bottle));
                    }
                }
                keys.Sort();
                return string.Join("|", keys);
            }
        }

        /// <summary>
        /// Solves the puzzle starting from assignments.
        /// </summary>
        public static SolverResult Solve(List<List<LiquidLayer>> initialBottles, int maxLayers, float colorTolerance = 0.05f)
        {
            if (initialBottles == null || initialBottles.Count == 0)
            {
                return new SolverResult { IsSolvable = false, SolutionPath = null, VisitedStatesCount = 0 };
            }

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

            int[][] initial = new int[initialBottles.Count][];
            for (int i = 0; i < initialBottles.Count; i++)
            {
                var layers = initialBottles[i];
                initial[i] = new int[layers.Count];
                for (int j = 0; j < layers.Count; j++)
                {
                    initial[i][j] = GetColorId(layers[j].Color);
                }
            }

            // 2. Perform BFS to find the shortest path
            var startNode = new StateNode(initial, new Move(-1, -1), null);
            if (startNode.IsSolved(maxLayers))
            {
                return new SolverResult { IsSolvable = true, SolutionPath = new List<Move>(), VisitedStatesCount = 1 };
            }

            var queue = new Queue<StateNode>();
            queue.Enqueue(startNode);

            var visited = new HashSet<string>();
            visited.Add(startNode.GetHashKey());

            const int maxVisited = 10000;
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
                                nextBottles[k] = new int[source.Length - countToPour];
                                Array.Copy(source, nextBottles[k], source.Length - countToPour);
                            }
                            else if (k == j)
                            {
                                nextBottles[k] = new int[target.Length + countToPour];
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

                        var nextNode = new StateNode(nextBottles, new Move(i, j), current);
                        string hash = nextNode.GetHashKey();
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
    }
}
