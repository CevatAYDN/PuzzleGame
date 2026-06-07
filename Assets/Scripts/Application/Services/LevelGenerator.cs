using System;
using System.Collections.Generic;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Logging;
using UnityEngine;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// WARNING (Fix #2): This file was a stale copy of <c>DifficultyBasedLevelGenerator</c>
    /// that did not implement <c>ILevelGenerator</c> despite declaring it. The real
    /// implementation lives in <c>PuzzleGame.Domain.Services.DifficultyBasedLevelGenerator</c>
    /// (Assets/Scripts/Domain/Services/DifficultyBasedLevelGenerator.cs).
    ///
    /// This Application-layer copy was abandoned mid-refactor and has been throwing
    /// CS0535 since <c>ILevelGenerator</c> gained <c>Generate()</c> /
    /// <c>GenerateSolvable()</c> with the Domain type signatures. Keeping it would
    /// require maintaining two identical generators in two assemblies.
    ///
    /// The correct flow is:
    ///   1. <c>GameInstaller</c> registers <c>ILevelGenerator</c> →
    ///      <c>Domain.Services.DifficultyBasedLevelGenerator</c>
    ///   2. <c>LevelSetupService</c> (Application) calls <c>ILevelGenerator.GenerateSolvable()</c>
    ///   3. The Domain generator handles solvability guarantee via <c>OreSortSolver</c>
    ///
    /// This file is preserved as a reference only. Delete it when the migration is
    /// complete and DI registration is updated.
    /// </summary>
    [Obsolete("Use PuzzleGame.Domain.Services.DifficultyBasedLevelGenerator instead")]
    public class LegacyLevelGenerator : ILevelGenerator
    {
        private readonly LevelConfig _config;
        private readonly System.Random _random;

        public LegacyLevelGenerator(LevelConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _random = new System.Random();
        }

        /// <summary>
        /// ILevelGenerator.Generate implementation — delegates to the Domain
        /// generator for solvability guarantee.
        /// </summary>
        public List<List<OreLayer>> Generate(
            int MoldCount,
            int maxLayers,
            int emptyMoldCount,
            DomainColor[] colorPalette,
            Difficulty difficulty,
            int seed = 0)
        {
            var domainGen = new Domain.Services.DifficultyBasedLevelGenerator();
            return domainGen.Generate(MoldCount, maxLayers, emptyMoldCount, colorPalette, difficulty, seed);
        }

        /// <summary>
        /// ILevelGenerator.GenerateSolvable implementation.
        /// </summary>
        public (List<List<OreLayer>> Molds, bool IsSolvable) GenerateSolvable(
            int MoldCount,
            int maxLayers,
            int emptyMoldCount,
            DomainColor[] colorPalette,
            Difficulty difficulty,
            int seed = 0,
            int maxAttempts = 8)
        {
            var domainGen = new Domain.Services.DifficultyBasedLevelGenerator();
            return domainGen.GenerateSolvable(MoldCount, maxLayers, emptyMoldCount, colorPalette, difficulty, seed, maxAttempts);
        }
    }
}