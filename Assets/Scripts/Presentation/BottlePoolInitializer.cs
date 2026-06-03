using System;
using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Application.Logging;
using PuzzleGame.Infrastructure.Interfaces;
using UnityEngine;

namespace PuzzleGame
{
    /// <summary>
    /// Discovers BottleController objects in the scene, activates the correct count for a level,
    /// and wires them into the game systems (history, input, level setup, camera).
    /// Extracted from GameManager for SRP (single responsibility = bottle pool initialization).
    /// </summary>
    public sealed class BottlePoolInitializer
    {
        private readonly ILevelSetupService _levelSetupService;
        private readonly IRendererService _rendererService;
        private readonly IBottleValidator _validator;
        private readonly IAnimationService _animationService;
        private readonly IInputHandlerService _inputHandlerService;
        private readonly IGameHistoryManager _historyManager;
        private readonly Camera _camera;

        private BottleController[] _allBottlesPool;
        private IBottleView[] _bottles;

        public IBottleView[] Bottles => _bottles;

        public BottlePoolInitializer(
            ILevelSetupService levelSetupService,
            IRendererService rendererService,
            IBottleValidator validator,
            IAnimationService animationService,
            IInputHandlerService inputHandlerService,
            IGameHistoryManager historyManager,
            Camera camera)
        {
            _levelSetupService = levelSetupService;
            _rendererService = rendererService;
            _validator = validator;
            _animationService = animationService;
            _inputHandlerService = inputHandlerService;
            _historyManager = historyManager;
            _camera = camera;
        }

        /// <summary>
        /// Discovers bottles in scene (once), activates the correct count for the level,
        /// creates the IBottleView array, and wires it into game systems.
        /// </summary>
        public void InitializeForLevel(LevelData level)
        {
            if (_allBottlesPool == null) CacheBottles();
            if (_allBottlesPool == null || _allBottlesPool.Length == 0)
            {
                BottleLogger.LogError("No bottles cached in master pool, cannot setup level.");
                return;
            }

            int targetCount = _allBottlesPool.Length;
            if (level != null)
            {
                targetCount = level.bottleCount;
            }
            targetCount = Mathf.Clamp(targetCount, BottleConstants.MinBottlesPerLevel, _allBottlesPool.Length);

            for (int i = 0; i < _allBottlesPool.Length; i++)
            {
                if (_allBottlesPool[i] != null)
                {
                    _allBottlesPool[i].gameObject.SetActive(i < targetCount);
                }
            }

            int activeCount = 0;
            for (int i = 0; i < _allBottlesPool.Length; i++)
            {
                var b = _allBottlesPool[i];
                if (b != null && b.gameObject.activeSelf)
                {
                    activeCount++;
                }
            }

            _bottles = new IBottleView[activeCount];
            int index = 0;
            for (int i = 0; i < _allBottlesPool.Length; i++)
            {
                var b = _allBottlesPool[i];
                if (b != null && b.gameObject.activeSelf)
                {
                    _bottles[index++] = b;
                }
            }

            _historyManager.Initialize(_bottles);
            _inputHandlerService.SetBottles(_bottles);
            _inputHandlerService.SetLevelData(level);

            _levelSetupService.SetupBottles(_bottles, level, _rendererService, _validator, _animationService);

            ConfigureCamera();
        }

        private static readonly BottleNameComparer Comparer = new BottleNameComparer();

        private void CacheBottles()
        {
            var temp = UnityEngine.Object.FindObjectsByType<BottleController>(FindObjectsInactive.Include);
            Array.Sort(temp, Comparer);
            _allBottlesPool = temp;

            if (BottleLogger.IsInfoEnabled)
            {
                BottleLogger.LogInfo($"Found {_allBottlesPool.Length} bottles in master pool.");
            }

            if (_allBottlesPool.Length == 0)
                BottleLogger.LogWarning("No BottleController found in scene.");
        }

        private void ConfigureCamera()
        {
            if (_camera == null) return;
            var camDefaultBgColor = new Color(
                BottleConstants.CameraBackgroundR,
                BottleConstants.CameraBackgroundG,
                BottleConstants.CameraBackgroundB,
                BottleConstants.CameraBackgroundA);
            _camera.backgroundColor = camDefaultBgColor;
            _camera.clearFlags = CameraClearFlags.SolidColor;
        }

        private class BottleNameComparer : IComparer<BottleController>
        {
            public int Compare(BottleController x, BottleController y)
            {
                if (x == null && y == null) return 0;
                if (x == null) return -1;
                if (y == null) return 1;
                return string.CompareOrdinal(x.name, y.name);
            }
        }
    }
}
