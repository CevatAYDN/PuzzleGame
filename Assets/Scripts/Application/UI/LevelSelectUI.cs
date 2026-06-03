using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Events;

namespace PuzzleGame.Application.UI
{
    /// <summary>
    /// Mobile-friendly level select screen.
    /// - Scrollable grid of level buttons (3 per row)
    /// - Shows star rating (0-3) per level
    /// - Locked levels are greyed out
    /// - Uses pooled buttons for performance
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

        private LevelButtonView[] _buttons;

        private void Start()
        {
            EventAggregator.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnDestroy()
        {
            EventAggregator.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
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

            // Clear existing
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
                {
                    btn = go.AddComponent<LevelButtonView>();
                }

                // Set button size dynamically
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

            EventAggregator.Publish(new LevelSelectedEvent(levelNumber));

            // Hide this UI after selection
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Refresh all button states (stars, lock status).
        /// Call when returning to level select after completing a level.
        /// </summary>
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

        /// <summary>
        /// Scroll to a specific level button.
        /// </summary>
        public void ScrollToLevel(int levelNumber)
        {
            if (scrollRect == null || levelNumber < 1 || levelNumber > maxLevelCount) return;

            int rowIndex = (levelNumber - 1) / columns;
            float totalHeight = contentRect.sizeDelta.y;
            float viewportHeight = scrollRect.viewport.rect.height;
            float targetY = 1f - (rowIndex * (buttonHeight + spacing) + buttonHeight * 0.5f) / (totalHeight - viewportHeight);
            targetY = Mathf.Clamp01(targetY);

            scrollRect.verticalNormalizedPosition = targetY;
        }
    }

    /// <summary>
    /// Individual level button component. Shows level number, stars, and lock state.
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
        [SerializeField] private Color lockedColor = new Color(0.3f, 0.3f, 0.3f);
        [SerializeField] private Color completedColor = new Color(0.2f, 0.85f, 0.35f);

        private int _levelNumber;
        private Action<int> _onClick;

        private void Awake()
        {
            var button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnButtonClicked);
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

            var button = GetComponent<Button>();
            if (button != null)
                button.interactable = unlocked;
        }

        private void OnButtonClicked()
        {
            _onClick?.Invoke(_levelNumber);
        }
    }
}
