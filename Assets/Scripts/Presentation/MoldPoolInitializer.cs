using System;
using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Logging;
using UnityEngine;

namespace PuzzleGame
{
    /// <summary>
    /// Discovers MoldController objects in the scene, activates the correct count for a level,
    /// and wires them into the game systems (history, input, level setup, camera).
    /// Extracted from GameManager for SRP (single responsibility = Mold pool initialization).
    /// </summary>
    public sealed class MoldPoolInitializer : IActiveMoldsProvider
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
        private readonly WobbleConfig _wobbleConfig;
        private readonly OptionalMoldActivator _optionalMoldActivator;

        private readonly List<MoldController> _gameplayMoldsPool = new List<MoldController>();
        private readonly List<MoldController> _optionalMoldsPool = new List<MoldController>();
        private IMoldView[] _Molds;

        public IMoldView[] Molds => _Molds;

        public int MaxGameplayMolds
        {
            get
            {
                if (_gameplayMoldsPool.Count == 0 && _optionalMoldsPool.Count == 0) CacheMolds();
                return _gameplayMoldsPool.Count;
            }
        }

        public MoldPoolInitializer(
            ILevelSetupService levelSetupService,
            IRendererService rendererService,
            IMoldValidator validator,
            IAnimationService animationService,
            IInputHandlerService inputHandlerService,
            IGameHistoryManager historyManager,
            IUpdateManager updateManager,
            Camera camera,
            IErrorIndicatorService errorIndicator,
            WobbleConfig wobbleConfig)
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
            _wobbleConfig = wobbleConfig;
            _optionalMoldActivator = new OptionalMoldActivator(
                _rendererService,
                _validator,
                _animationService,
                _inputHandlerService,
                _historyManager,
                _updateManager,
                _errorIndicator,
                _wobbleConfig);
        }

        /// <summary>
        /// Discovers Molds in scene (once), activates the correct count for the level,
        /// creates the IMoldView array, and wires it into game systems.
        /// </summary>
        public void InitializeForLevel(LevelData level)
        {
            if (_gameplayMoldsPool.Count == 0 && _optionalMoldsPool.Count == 0) CacheMolds();

            // 1. Hide all optional molds first
            foreach (var mold in _optionalMoldsPool)
            {
                if (mold != null) mold.gameObject.SetActive(false);
            }

            // 2. Setup standard gameplay molds count
            int targetCount = _gameplayMoldsPool.Count;
            if (level != null)
            {
                targetCount = level.MoldCount;
            }
            targetCount = Mathf.Clamp(targetCount, ForgeConstants.MinMoldsPerLevel, _gameplayMoldsPool.Count);

            for (int i = 0; i < _gameplayMoldsPool.Count; i++)
            {
                if (_gameplayMoldsPool[i] != null)
                {
                    _gameplayMoldsPool[i].gameObject.SetActive(i < targetCount);
                }
            }

            // 3. Collect active gameplay molds
            int activeCount = 0;
            for (int i = 0; i < _gameplayMoldsPool.Count; i++)
            {
                var b = _gameplayMoldsPool[i];
                if (b != null && b.gameObject.activeSelf)
                {
                    activeCount++;
                }
            }

            _Molds = new IMoldView[activeCount];
            int index = 0;
            for (int i = 0; i < _gameplayMoldsPool.Count; i++)
            {
                var b = _gameplayMoldsPool[i];
                if (b != null && b.gameObject.activeSelf)
                {
                    b.MoldIndex = index;
                    _Molds[index++] = b;
                }
            }

            // Wire Wobble components to the update manager and inject WobbleConfig
            for (int i = 0; i < _gameplayMoldsPool.Count; i++)
            {
                var wobble = _gameplayMoldsPool[i]?.GetComponent<Wobble>();
                if (wobble != null)
                {
                    wobble.config = _wobbleConfig;
                    wobble.SetUpdateManager(_updateManager);
                }
            }

            _historyManager.Initialize(_Molds);
            _inputHandlerService.SetMolds(_Molds);
            _inputHandlerService.SetLevelData(level);

            _levelSetupService.SetupMolds(_Molds, level, _rendererService, _validator, _animationService);
            _errorIndicator?.Initialize(_Molds);

            ConfigureCamera();
        }

        public void ActivateOptionalMolds(LevelData level)
        {
            // Fix #18: Optional target activation is now handled by a focused
            // collaborator. This keeps MoldPoolInitializer responsible for base
            // scene mold discovery/setup and moves optional-target wiring into
            // OptionalMoldActivator.
            //
            // Fix #29: NOTE - OptionalMoldActivator.Activate() now handles all dependent
            // service sync internally (historyManager, inputHandlerService, errorIndicator).
            // No need for redundant calls here.
            _Molds = _optionalMoldActivator.Activate(level, _optionalMoldsPool, _Molds);
        }

        private static readonly MoldNameComparer Comparer = new MoldNameComparer();

        private void CacheMolds()
        {
            var temp = UnityEngine.Object.FindObjectsByType<MoldController>(FindObjectsInactive.Include);
            Array.Sort(temp, Comparer);
            
            _gameplayMoldsPool.Clear();
            _optionalMoldsPool.Clear();

            foreach (var mold in temp)
            {
                if (mold == null) continue;
                if (mold.isOptionalTarget)
                {
                    _optionalMoldsPool.Add(mold);
                }
                else
                {
                    _gameplayMoldsPool.Add(mold);
                }
            }

            if (MoldLogger.IsInfoEnabled)
            {
                MoldLogger.LogInfo($"Found {_gameplayMoldsPool.Count} gameplay molds and {_optionalMoldsPool.Count} optional molds in master pool.");
            }

            if (_gameplayMoldsPool.Count == 0)
                MoldLogger.LogWarning("No gameplay MoldController found in scene.");
        }

        private void ConfigureCamera()
        {
            if (_camera == null) return;
            _camera.backgroundColor = new Color(0.08f, 0.05f, 0.16f, 1.0f);
            _camera.clearFlags = CameraClearFlags.SolidColor;

            var adapter = _camera.GetComponent<PuzzleGame.Presentation.CameraResolutionAdapter>();
            if (adapter == null)
            {
                adapter = _camera.gameObject.AddComponent<PuzzleGame.Presentation.CameraResolutionAdapter>();
            }
            adapter.Initialize(this);
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
