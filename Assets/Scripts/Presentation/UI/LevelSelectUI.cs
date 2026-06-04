using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Events;

namespace PuzzleGame.Presentation.UI
{
    /// <summary>
    /// Mobile-friendly level select screen.
    /// - Scrollable grid of level buttons (3 per row)
    /// - Shows star rating (0-3) per level
    /// - Locked levels are greyed out
    /// - Uses pooled buttons for performance
    ///
    /// Moved from Application/UI/ to Presentation/UI/ (Fix Critical #2).
    /// MonoBehaviour + SerializeField belong in Presentation layer.
    /// </summary>
    public class LevelSelectUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform buttonContainer;
        [SerializeField] private GameObject levelButtonPrefab;
        [SerializeField] private RectTransform contentRect;
        [SerializeField] private ScrollRect scrollRect;

        [Header("Layout")]
        [SerializeField] private int columns = 3;
        [SerializeField] private float buttonWidth = 100f;
        [SerializeField] private float buttonHeight = 100f;
        [SerializeField] private float spacing = 10f;
        [SerializeField] private int maxLevelCount = 100;

        private ILevelRepository _repository;
        private ILevelProgressService _progress;
        private IEventAggregator _eventAggregator;

        private LevelButtonView[] _buttons;

        [VContainer.Inject]
        public void Construct(ILevelRepository repository, ILevelProgressService progress, IEventAggregator eventAggregator)
        {
            _repository = repository;
            _progress = progress;
            _eventAggregator = eventAggregator;
        }

        private void Start()
        {
            _eventAggregator?.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnDestroy()
        {
            _eventAggregator?.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnGameStateChanged(GameStateChangedEvent e)
        {
            if (e.Current == GameState.Menu)
            {
                gameObject.SetActive(true);
                RefreshAll();
            }
        }

        public void Initialize(ILevelRepository repository, ILevelProgressService progress)
        {
            _repository = repository;
            _progress = progress;
            BuildGrid();
        }

        private void BuildGrid()
        {
            if (buttonContainer == null) return;

            foreach (Transform child in buttonContainer)
                Destroy(child.gameObject);

            int rowCount = Mathf.CeilToInt(maxLevelCount / (float)columns);
            float totalHeight = rowCount * (buttonHeight + spacing) + spacing;
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, totalHeight);

            _buttons = new LevelButtonView[maxLevelCount];

            for (int i = 0; i < maxLevelCount; i++)
            {
                var go = Instantiate(levelButtonPrefab, buttonContainer);
                var btn = go.GetComponent<LevelButtonView>();
                if (btn == null)
                    btn = go.AddComponent<LevelButtonView>();

                var rect = go.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.sizeDelta = new Vector2(buttonWidth, buttonHeight);
                    rect.anchoredPosition = new Vector2(
                        (i % columns) * (buttonWidth + spacing) + buttonWidth * 0.5f,
                        -(i / columns) * (buttonHeight + spacing) - buttonHeight * 0.5f
                    );
                }

                int levelNum = i + 1;
                bool unlocked = _progress != null && _progress.IsUnlocked(levelNum);
                int stars = unlocked ? (_progress?.GetStars(levelNum) ?? 0) : 0;

                btn.Setup(levelNum, stars, unlocked, OnLevelClicked);
                _buttons[i] = btn;
            }
        }

        private void OnLevelClicked(int levelNumber)
        {
            if (_progress == null || !_progress.IsUnlocked(levelNumber)) return;

            var levelData = _repository?.GetByNumber(levelNumber);
            if (levelData == null) return;

            _eventAggregator.Publish(new LevelSelectedEvent(levelNumber));
            gameObject.SetActive(false);
        }

        /// <summary>Refresh all button states (stars, lock status).</summary>
        public void RefreshAll()
        {
            if (_buttons == null) return;
            for (int i = 0; i < _buttons.Length; i++)
            {
                int levelNum = i + 1;
                bool unlocked = _progress != null && _progress.IsUnlocked(levelNum);
                int stars = unlocked ? (_progress?.GetStars(levelNum) ?? 0) : 0;
                _buttons[i]?.Refresh(stars, unlocked);
            }
        }

        /// <summary>Scroll to a specific level button.</summary>
        public void ScrollToLevel(int levelNumber)
        {
            if (scrollRect == null || levelNumber < 1 || levelNumber > maxLevelCount) return;

            int rowIndex = (levelNumber - 1) / columns;
            float totalHeight = contentRect.sizeDelta.y;
            float viewportHeight = scrollRect.viewport.rect.height;
            float targetY = 1f - (rowIndex * (buttonHeight + spacing) + buttonHeight * 0.5f) / (totalHeight - viewportHeight);
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(targetY);
        }
    }

    /// <summary>
    /// Individual level button component. Shows level number, stars, and lock state.
    /// Moved to Presentation/UI/ with LevelSelectUI (Fix Critical #2).
    /// </summary>
    public class LevelButtonView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI levelNumberText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image lockIcon;
        [SerializeField] private GameObject starContainer;
        [SerializeField] private GameObject star1, star2, star3;

        [Header("Colors")]
        [SerializeField] private Color unlockedColor = new Color(0.2f, 0.6f, 0.9f);
        [SerializeField] private Color lockedColor   = new Color(0.3f, 0.3f, 0.3f);
        [SerializeField] private Color completedColor = new Color(0.2f, 0.85f, 0.35f);

        private int _levelNumber;
        private Action<int> _onClick;
        private Button _cachedButton;

        private void Awake()
        {
            _cachedButton = GetComponent<Button>();
            if (_cachedButton != null)
            {
                _cachedButton.onClick.RemoveAllListeners();
                _cachedButton.onClick.AddListener(OnButtonClicked);
            }
        }

        public void Setup(int levelNumber, int stars, bool unlocked, Action<int> onClick)
        {
            _levelNumber = levelNumber;
            _onClick = onClick;
            Refresh(stars, unlocked);
        }

        public void Refresh(int stars, bool unlocked)
        {
            if (levelNumberText != null)
                levelNumberText.text = _levelNumber.ToString();

            if (backgroundImage != null)
            {
                backgroundImage.color = !unlocked ? lockedColor
                    : stars >= 1 ? completedColor
                    : unlockedColor;
            }

            if (lockIcon != null)
                lockIcon.gameObject.SetActive(!unlocked);

            if (starContainer != null)
            {
                starContainer.SetActive(unlocked);
                if (star1 != null) star1.SetActive(stars >= 1);
                if (star2 != null) star2.SetActive(stars >= 2);
                if (star3 != null) star3.SetActive(stars >= 3);
            }

            if (_cachedButton != null)
                _cachedButton.interactable = unlocked;
        }

        private void OnButtonClicked() => _onClick?.Invoke(_levelNumber);
    }
}
