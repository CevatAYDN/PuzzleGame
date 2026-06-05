using System;
using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Application.Logging;
// IRendererService now in PuzzleGame.Application.Interfaces
using UnityEngine;

namespace PuzzleGame
{
    /// <summary>
    /// Discovers MoldController objects in the scene, activates the correct count for a level,
    /// and wires them into the game systems (history, input, level setup, camera).
    /// Extracted from GameManager for SRP (single responsibility = Mold pool initialization).
    /// </summary>
    public sealed class MoldPoolInitializer
    {
        private readonly ILevelSetupService _levelSetupService;
        private readonly IRendererService _rendererService;
        private readonly IMoldValidator _validator;
        private readonly IAnimationService _animationService;
        private readonly IInputHandlerService _inputHandlerService;
        private readonly IGameHistoryManager _historyManager;
        private readonly IUpdateManager _updateManager;
        private readonly Camera _camera;
        private readonly IErrorIndicatorService _errorIndicator;

        private MoldController[] _allMoldsPool;
        private IMoldView[] _Molds;

        public IMoldView[] Molds => _Molds;

        public MoldPoolInitializer(
            ILevelSetupService levelSetupService,
            IRendererService rendererService,
            IMoldValidator validator,
            IAnimationService animationService,
            IInputHandlerService inputHandlerService,
            IGameHistoryManager historyManager,
            IUpdateManager updateManager,
            Camera camera,
            IErrorIndicatorService errorIndicator)
        {
            _levelSetupService = levelSetupService;
            _rendererService = rendererService;
            _validator = validator;
            _animationService = animationService;
            _inputHandlerService = inputHandlerService;
            _historyManager = historyManager;
            _updateManager = updateManager;
            _camera = camera;
            _errorIndicator = errorIndicator;
        }

        /// <summary>
        /// Discovers Molds in scene (once), activates the correct count for the level,
        /// creates the IMoldView array, and wires it into game systems.
        /// </summary>
        public void InitializeForLevel(LevelData level)
        {
            if (_allMoldsPool == null) CacheMolds();
            if (_allMoldsPool == null || _allMoldsPool.Length == 0)
            {
                MoldLogger.LogError("No Molds cached in master pool, cannot setup level.");
                return;
            }

            int targetCount = _allMoldsPool.Length;
            if (level != null)
            {
                targetCount = level.MoldCount;
            }
            targetCount = Mathf.Clamp(targetCount, ForgeConstants.MinMoldsPerLevel, _allMoldsPool.Length);

            for (int i = 0; i < _allMoldsPool.Length; i++)
            {
                if (_allMoldsPool[i] != null)
                {
                    _allMoldsPool[i].gameObject.SetActive(i < targetCount);
                }
            }

            int activeCount = 0;
            for (int i = 0; i < _allMoldsPool.Length; i++)
            {
                var b = _allMoldsPool[i];
                if (b != null && b.gameObject.activeSelf)
                {
                    activeCount++;
                }
            }

            _Molds = new IMoldView[activeCount];
            int index = 0;
            for (int i = 0; i < _allMoldsPool.Length; i++)
            {
                var b = _allMoldsPool[i];
                if (b != null && b.gameObject.activeSelf)
                {
                    b.MoldIndex = index;
                    _Molds[index++] = b;
                }
            }

            // Wire Wobble components to the update manager
            for (int i = 0; i < _allMoldsPool.Length; i++)
            {
                var wobble = _allMoldsPool[i]?.GetComponent<Wobble>();
                if (wobble != null)
                    wobble.SetUpdateManager(_updateManager);
            }

            _historyManager.Initialize(_Molds);
            _inputHandlerService.SetMolds(_Molds);
            _inputHandlerService.SetLevelData(level);

            _levelSetupService.SetupMolds(_Molds, level, _rendererService, _validator, _animationService);
            _errorIndicator?.Initialize(_Molds);

            ConfigureCamera();
        }

        private static readonly MoldNameComparer Comparer = new MoldNameComparer();

        private void CacheMolds()
        {
            // One-time scene scan: FindObjectsByType is O(scene) and must NOT run per-level
            // (call site gates via `if (_allMoldsPool == null) CacheMolds()`).
            var temp = UnityEngine.Object.FindObjectsByType<MoldController>(FindObjectsInactive.Include);
            Array.Sort(temp, Comparer);
            _allMoldsPool = temp;

            if (MoldLogger.IsInfoEnabled)
            {
                MoldLogger.LogInfo($"Found {_allMoldsPool.Length} Molds in master pool.");
            }

            if (_allMoldsPool.Length == 0)
                MoldLogger.LogWarning("No MoldController found in scene.");
        }

        private void ConfigureCamera()
        {
            if (_camera == null) return;
            _camera.backgroundColor = new Color(0.08f, 0.05f, 0.16f, 1.0f);
            _camera.clearFlags = CameraClearFlags.SolidColor;
        }

        private class MoldNameComparer : IComparer<MoldController>
        {
            public int Compare(MoldController x, MoldController y)
            {
                if (x == null && y == null) return 0;
                if (x == null) return -1;
                if (y == null) return 1;
                return string.CompareOrdinal(x.name, y.name);
            }
        }
    }
}
