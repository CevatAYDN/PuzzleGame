using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Logging;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Application.Configuration;

namespace PuzzleGame.Presentation.UI
{
    /// <summary>
    /// Handles the pure in-game HUD (moves, undo, hint).
    /// Uses PrimeTween for animation feedback.
    /// </summary>
    public class HUDWidgetPresenter : MonoBehaviour
    {
        [Header("HUD Elements")]
        [SerializeField] private TextMeshProUGUI _moveCountText;
        [SerializeField] private TextMeshProUGUI _levelTitleText;
        [SerializeField] private Button _undoButton;
        [SerializeField] private Button _hintButton;
        
        [Header("Styling")]
        [SerializeField] private UIStyleConfig _styleConfig;

        private IGameHistoryManager _history;
        private ILocalizationService _localization;
        private IEventAggregator _events;
        private IUndoService _undoService;
        private IHintService _hintService;
        private IAnimationService _animationService;
        private IActiveMoldsProvider _moldsProvider;
        
        private LevelData _currentLevel;
        private readonly StringBuilder _sb = new StringBuilder(64);
        private int _lastMoveCount = -1;
        private int _lastLevelNumber = -1;

        [VContainer.Inject]
        public void Construct(
            IGameHistoryManager history,
            ILocalizationService localization,
            IEventAggregator events,
            IUndoService undoService,
            IHintService hintService,
            IAnimationService animationService,
            IActiveMoldsProvider moldsProvider)
        {
            _history = history;
            _localization = localization;
            _events = events;
            _undoService = undoService;
            _hintService = hintService;
            _animationService = animationService;
            _moldsProvider = moldsProvider;
        }

        private void OnEnable()
        {
            if (_events != null)
            {
                _events.Subscribe<LevelLoadedEvent>(OnLevelLoaded);
                _events.Subscribe<HintHighlightEvent>(OnHintHighlight);
            }

            if (_history != null) _history.OnMoveCountChanged += UpdateMoveCount;
            if (_undoButton != null) _undoButton.onClick.AddListener(OnUndoClicked);
            if (_hintButton != null) _hintButton.onClick.AddListener(OnHintClicked);
        }

        private void OnDisable()
        {
            if (_events != null)
            {
                _events.Unsubscribe<LevelLoadedEvent>(OnLevelLoaded);
                _events.Unsubscribe<HintHighlightEvent>(OnHintHighlight);
            }

            if (_history != null) _history.OnMoveCountChanged -= UpdateMoveCount;
        }

        private void Start()
        {
            UpdateMoveCount(0);
        }

        private void OnLevelLoaded(LevelLoadedEvent e)
        {
            _currentLevel = e.Level;
            _lastMoveCount = -1;
            UpdateMoveCount(0);
            UpdateLevelTitle();
        }

        private void UpdateMoveCount(int count)
        {
            if (_moveCountText == null || _history == null) return;
            if (count == _lastMoveCount) return;
            _lastMoveCount = count;

            string movesLabel = _localization != null ? _localization.GetString("moves_text") : "Moves";
            _sb.Clear();
            _sb.Append(movesLabel).Append(": ").Append(_history.CurrentMoveCount);
            _moveCountText.SetText(_sb);
        }

        private void UpdateLevelTitle()
        {
            if (_levelTitleText == null || _currentLevel == null) return;
            if (_currentLevel.levelNumber == _lastLevelNumber) return;
            _lastLevelNumber = _currentLevel.levelNumber;
            _sb.Clear();
            string levelLabel = _localization != null ? _localization.GetString("level_title") : "Level";
            _sb.Append(levelLabel).Append(" ").Append(_currentLevel.levelNumber);
            _levelTitleText.SetText(_sb);
        }

        private void OnUndoClicked()
        {
            if (_animationService != null && _animationService.IsAnimating)
            {
                MoldLogger.LogWarning("[HUDWidgetPresenter] Undo click ignored: Animation is active.");
                return;
            }
            
            AnimateButton(_undoButton);

            if (_undoService != null)
            {
                _undoService.TryUndo();
            }
        }

        private void OnHintClicked()
        {
            if (_animationService != null && _animationService.IsAnimating)
            {
                MoldLogger.LogWarning("[HUDWidgetPresenter] Hint click ignored: Animation is active.");
                return;
            }
            
            AnimateButton(_hintButton);

            if (_hintService != null && _currentLevel != null)
            {
                if (_hintService.TryGetHint(_currentLevel, out int srcIndex, out int dstIndex))
                {
                    _events?.Publish(new HintHighlightEvent(srcIndex, dstIndex));
                    MoldLogger.LogInfo($"[HUDWidgetPresenter] Hint suggested: {srcIndex} -> {dstIndex}");
                }
            }
        }

        private void AnimateButton(Button btn)
        {
            if (btn == null || (_styleConfig != null && _styleConfig.reduceMotion)) return;
            
            // PrimeTween is used to add "juice" to the button presses
            PrimeTween.Tween.Scale(btn.transform, 0.9f, 0.1f)
                .OnComplete(() => PrimeTween.Tween.Scale(btn.transform, 1f, 0.2f, PrimeTween.Ease.OutBounce));
        }

        private void OnHintHighlight(HintHighlightEvent e)
        {
            var molds = _moldsProvider?.Molds;
            if (molds == null) return;

            if (e.SourceIndex >= 0 && e.SourceIndex < molds.Length)
                molds[e.SourceIndex].SetSelectionHighlight(true);
            if (e.TargetIndex >= 0 && e.TargetIndex < molds.Length && e.TargetIndex != e.SourceIndex)
                molds[e.TargetIndex].SetSelectionHighlight(true);

            PrimeTween.Tween.Delay(1.5f).OnComplete(() =>
            {
                if (e.SourceIndex >= 0 && e.SourceIndex < molds.Length)
                    molds[e.SourceIndex].SetSelectionHighlight(false);
                if (e.TargetIndex >= 0 && e.TargetIndex < molds.Length && e.TargetIndex != e.SourceIndex)
                    molds[e.TargetIndex].SetSelectionHighlight(false);
            });
        }
    }
}
