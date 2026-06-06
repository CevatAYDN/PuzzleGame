using System;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using UnityEngine;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Linear step-by-step tutorial that auto-advances on game events.
    /// Persists completion flag in PlayerPrefs so the welcome dialog only shows once per install.
    /// </summary>
    public sealed class TutorialService : ITutorialService
    {
        private const string LogTag = "[Tutorial]";
        private const string CompletedKey = "PuzzleGame.TutorialCompleted";

        private readonly IEventAggregator _events;
        private readonly IMoldSelectionService _selection;

        private TutorialStep _currentStep = TutorialStep.Inactive;

        public TutorialStep CurrentStep => _currentStep;
        public bool IsActive => _currentStep != TutorialStep.Inactive && _currentStep != TutorialStep.LevelComplete;
        public string CurrentMessageKey => _currentStep switch
        {
            TutorialStep.Welcome => "tutorial_welcome",
            TutorialStep.TapToSelect => "tutorial_tap_to_select",
            TutorialStep.TapToCast => "tutorial_tap_to_cast",
            TutorialStep.LevelComplete => "tutorial_well_done",
            _ => string.Empty
        };

        public event Action<TutorialStep> OnStepChanged;
        public event Action OnTutorialCompleted;

        public TutorialService(IEventAggregator events, IMoldSelectionService selection)
        {
            _events = events;
            _selection = selection;
            _selection.OnMoldSelected += OnMoldSelected;
            _events.Subscribe<CastCompletedEvent>(OnCastCompleted);
            _events.Subscribe<LevelSelectedEvent>(OnLevelSelected);
        }

        public void Begin()
        {
            if (IsActive) return;
            if (PlayerPrefs.GetInt(CompletedKey, 0) == 1)
            {
                MoldLogger.LogInfo($"{LogTag} Already completed — not running.");
                return;
            }
            MoldLogger.LogInfo($"{LogTag} Beginning tutorial.");
            SetStep(TutorialStep.Welcome);
        }

        public void Skip()
        {
            if (!IsActive) return;
            MoldLogger.LogInfo($"{LogTag} Skipped at step {_currentStep}.");
            Complete();
            SetStep(TutorialStep.Inactive);
        }

        public void Reset()
        {
            PlayerPrefs.DeleteKey(CompletedKey);
            _currentStep = TutorialStep.Inactive;
        }

        private void OnLevelSelected(LevelSelectedEvent _)
        {
            if (_currentStep == TutorialStep.LevelComplete)
            {
                SetStep(TutorialStep.Inactive);
            }
        }

        private void OnMoldSelected(Domain.Models.MoldState _)
        {
            if (!IsActive) return;
            if (_currentStep == TutorialStep.Welcome)
            {
                SetStep(TutorialStep.TapToSelect);
            }
        }

        private void OnCastCompleted(CastCompletedEvent _)
        {
            if (!IsActive) return;
            if (_currentStep == TutorialStep.TapToSelect || _currentStep == TutorialStep.TapToCast)
            {
                SetStep(TutorialStep.LevelComplete);
            }
        }

        private void SetStep(TutorialStep next)
        {
            if (_currentStep == next) return;
            _currentStep = next;
            MoldLogger.LogInfo($"{LogTag} Step -> {next}");
            OnStepChanged?.Invoke(next);

            if (next == TutorialStep.LevelComplete)
            {
                Complete();
            }
        }

        private void Complete()
        {
            PlayerPrefs.SetInt(CompletedKey, 1);
            PlayerPrefs.Save();
            OnTutorialCompleted?.Invoke();
        }
    }
}
