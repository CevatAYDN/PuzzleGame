using System.Collections.Generic;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;

namespace PuzzleGame
{
    /// <summary>
    /// Activates optional target molds for levels that define optional targets and
    /// rewires the systems that need the expanded active mold array.
    ///
    /// Fix #18: extracted from <see cref="MoldPoolInitializer"/> so the initializer
    /// can stay focused on scene discovery + base gameplay mold setup while optional
    /// target activation has one dedicated, testable responsibility.
    /// </summary>
    public sealed class OptionalMoldActivator
    {
        private readonly IRendererService _rendererService;
        private readonly IMoldValidator _validator;
        private readonly IAnimationService _animationService;
        private readonly IInputHandlerService _inputHandlerService;
        private readonly IGameHistoryManager _historyManager;
        private readonly IUpdateManager _updateManager;
        private readonly IErrorIndicatorService _errorIndicator;
        private readonly WobbleConfig _wobbleConfig;

        public OptionalMoldActivator(
            IRendererService rendererService,
            IMoldValidator validator,
            IAnimationService animationService,
            IInputHandlerService inputHandlerService,
            IGameHistoryManager historyManager,
            IUpdateManager updateManager,
            IErrorIndicatorService errorIndicator,
            WobbleConfig wobbleConfig)
        {
            _rendererService = rendererService;
            _validator = validator;
            _animationService = animationService;
            _inputHandlerService = inputHandlerService;
            _historyManager = historyManager;
            _updateManager = updateManager;
            _errorIndicator = errorIndicator;
            _wobbleConfig = wobbleConfig;
        }

        /// <summary>
        /// Activates the optional molds requested by <paramref name="level"/> and returns
        /// the new combined active mold array. If the level has no optional targets,
        /// returns <paramref name="currentMolds"/> unchanged.
        /// </summary>
        public IMoldView[] Activate(
            LevelData level,
            IReadOnlyList<MoldController> optionalMoldsPool,
            IMoldView[] currentMolds)
        {
            if (level == null || level.optionalTargets == null || level.optionalTargets.Count == 0)
            {
                return currentMolds;
            }

            if (optionalMoldsPool == null || optionalMoldsPool.Count == 0)
            {
                return currentMolds;
            }

            var safeCurrentMolds = currentMolds ?? System.Array.Empty<IMoldView>();
            int requestedOptionalCount = UnityEngine.Mathf.Min(level.optionalTargets.Count, optionalMoldsPool.Count);
            var combinedActiveMolds = new List<IMoldView>(safeCurrentMolds);
            int startIndex = safeCurrentMolds.Length;

            for (int i = 0; i < optionalMoldsPool.Count; i++)
            {
                var mold = optionalMoldsPool[i];
                if (mold == null) continue;

                if (i < requestedOptionalCount)
                {
                    ActivateSingleOptionalMold(mold, startIndex + i, level.optionalTargets[i], i);
                    combinedActiveMolds.Add(mold);
                }
                else
                {
                    mold.gameObject.SetActive(false);
                }
            }

            var finalArray = combinedActiveMolds.ToArray();
            _inputHandlerService.SetMolds(finalArray);
            _historyManager.SetMolds(finalArray);
            _errorIndicator?.Initialize(finalArray);
            return finalArray;
        }

        private void ActivateSingleOptionalMold(
            MoldController mold,
            int moldIndex,
            OptionalTargetData targetConfig,
            int optionalIndex)
        {
            mold.gameObject.SetActive(true);
            mold.MoldIndex = moldIndex;

            // Optional target molds start empty; their target requirement is encoded
            // in LevelData.optionalTargets rather than as initial ore layers.
            mold.Initialize(_rendererService, _validator, _animationService, new List<OreLayer>());

            var wobble = mold.GetComponent<Wobble>();
            if (wobble != null)
            {
                wobble.config = _wobbleConfig;
                wobble.SetUpdateManager(_updateManager);
            }

            mold.gameObject.name = $"Optional_{targetConfig.name}_{optionalIndex}";
        }
    }
}
