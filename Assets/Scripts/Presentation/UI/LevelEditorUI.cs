using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;

namespace PuzzleGame.Presentation.UI
{
    public sealed class LevelEditorUI : MonoBehaviour
    {
        private const string LogTag = "[LevelEditorUI]";

        [Header("Level List")]
        [SerializeField] private RectTransform levelListContainer;
        [SerializeField] private GameObject levelEntryPrefab;
        [SerializeField] private Button refreshListButton;

        [Header("Create Panel")]
        [SerializeField] private TMP_InputField newLevelNameInput;
        [SerializeField] private Slider moldCountSlider;
        [SerializeField] private TextMeshProUGUI moldCountLabel;
        [SerializeField] private Slider colorCountSlider;
        [SerializeField] private TextMeshProUGUI colorCountLabel;
        [SerializeField] private Slider emptyMoldSlider;
        [SerializeField] private TextMeshProUGUI emptyMoldLabel;
        [SerializeField] private Button createButton;

        [Header("Edit Panel")]
        [SerializeField] private GameObject editPanel;
        [SerializeField] private TextMeshProUGUI editTitleText;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button playtestButton;

        [Header("Feedback")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private float statusDuration = 3f;

        private ILevelEditorService _editor;
        private IEventAggregator _events;

        private float _statusTimer;
        private readonly List<EditorLevelEntryView> _entryViews = new List<EditorLevelEntryView>();

        [VContainer.Inject]
        public void Construct(ILevelEditorService editor, IEventAggregator events)
        {
            _editor = editor;
            _events = events;
        }

        private void Start()
        {
            if (refreshListButton != null) refreshListButton.onClick.AddListener(OnRefreshList);
            if (createButton != null) createButton.onClick.AddListener(OnCreateLevel);
            if (saveButton != null) saveButton.onClick.AddListener(OnSaveLevel);
            if (deleteButton != null) deleteButton.onClick.AddListener(OnDeleteLevel);
            if (playtestButton != null) playtestButton.onClick.AddListener(OnPlaytest);

            if (moldCountSlider != null) moldCountSlider.onValueChanged.AddListener(v =>
            {
                if (moldCountLabel != null) moldCountLabel.text = $"Molds: {(int)v}";
            });
            if (colorCountSlider != null) colorCountSlider.onValueChanged.AddListener(v =>
            {
                if (colorCountLabel != null) colorCountLabel.text = $"Colors: {(int)v}";
            });
            if (emptyMoldSlider != null) emptyMoldSlider.onValueChanged.AddListener(v =>
            {
                if (emptyMoldLabel != null) emptyMoldLabel.text = $"Empty: {(int)v}";
            });

            if (newLevelNameInput != null)
                newLevelNameInput.text = "My Level";

            SetStatus("");
            RefreshLevelList();
        }

        private void Update()
        {
            if (_statusTimer > 0f)
            {
                _statusTimer -= Time.deltaTime;
                if (_statusTimer <= 0f && statusText != null)
                    statusText.text = "";
            }
        }

        private void OnEnable()
        {
            RefreshLevelList();
        }

        private void RefreshLevelList()
        {
            if (levelListContainer == null || _editor == null) return;

            foreach (Transform child in levelListContainer)
                Destroy(child.gameObject);
            _entryViews.Clear();

            _editor.RefreshSavedLevels();
            var levels = _editor.ListSavedLevels();
            if (levels == null || levels.Count == 0)
            {
                SetStatus("No saved levels yet. Create one!");
                return;
            }

            foreach (var level in levels)
            {
                var go = Instantiate(levelEntryPrefab, levelListContainer);
                var view = go.GetComponent<EditorLevelEntryView>();
                if (view == null)
                    view = go.AddComponent<EditorLevelEntryView>();

                view.Setup(level.levelName, () => OnLoadLevel(level.levelName));
                _entryViews.Add(view);
            }
        }

        private void OnRefreshList()
        {
            RefreshLevelList();
            SetStatus("Level list refreshed.");
        }

        private void OnCreateLevel()
        {
            if (_editor == null) return;

            string name = newLevelNameInput != null
                ? newLevelNameInput.text.Trim()
                : "My Level";

            if (string.IsNullOrEmpty(name))
            {
                SetStatus("Level name cannot be empty.");
                return;
            }

            int moldCount = moldCountSlider != null ? (int)moldCountSlider.value : 5;
            int colorCount = colorCountSlider != null ? (int)colorCountSlider.value : 4;
            int emptyMolds = emptyMoldSlider != null ? (int)emptyMoldSlider.value : 2;

            _editor.CreateNewLevel(name, moldCount, colorCount, emptyMolds);
            _editor.SaveCurrentLevel();
            SetStatus($"Level '{name}' created and saved.");
            RefreshLevelList();

            if (editPanel != null)
            {
                editPanel.SetActive(true);
                if (editTitleText != null)
                    editTitleText.text = $"Editing: {name}";
            }
        }

        private void OnLoadLevel(string levelName)
        {
            if (_editor == null) return;
            if (_editor.LoadLevel(levelName))
            {
                SetStatus($"Loaded: {levelName}");
                if (editPanel != null)
                {
                    editPanel.SetActive(true);
                    if (editTitleText != null)
                        editTitleText.text = $"Editing: {levelName}";
                }
            }
            else
            {
                SetStatus($"Failed to load: {levelName}");
            }
        }

        private void OnSaveLevel()
        {
            if (_editor == null || !_editor.HasActiveEdit)
            {
                SetStatus("No level loaded for editing.");
                return;
            }

            _editor.SaveCurrentLevel();
            SetStatus("Level saved.");
            RefreshLevelList();
        }

        private void OnDeleteLevel()
        {
            if (_editor == null || !_editor.HasActiveEdit)
            {
                SetStatus("No level loaded for editing.");
                return;
            }

            string name = _editor.CurrentEdit?.levelName;
            if (string.IsNullOrEmpty(name))
            {
                SetStatus("Cannot determine level name.");
                return;
            }

            if (_editor.DeleteLevel(name))
            {
                SetStatus($"Deleted: {name}");
                if (editPanel != null) editPanel.SetActive(false);
                RefreshLevelList();
            }
            else
            {
                SetStatus($"Failed to delete: {name}");
            }
        }

        private void OnPlaytest()
        {
            if (_editor == null || !_editor.HasActiveEdit)
            {
                SetStatus("No level loaded for playtesting.");
                return;
            }

            _editor.SaveCurrentLevel();
            MoldLogger.LogInfo($"{LogTag} Playtest requested for {_editor.CurrentEdit?.levelName}");

            if (_events != null)
            {
                string currentName = _editor.CurrentEdit?.levelName;
                var allLevels = _editor.ListSavedLevels();
                int levelNumber = 0;
                for (int i = 0; i < allLevels.Count; i++)
                {
                    if (allLevels[i].levelName == currentName)
                    {
                        levelNumber = i;
                        break;
                    }
                }
                _events.Publish(new Application.Events.LevelSelectedEvent(levelNumber));
                gameObject.SetActive(false);
            }
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
                _statusTimer = string.IsNullOrEmpty(message) ? 0f : statusDuration;
            }
        }
    }

    public sealed class EditorLevelEntryView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI levelNameText;
        [SerializeField] private Button loadButton;

        private System.Action _onLoad;

        private void Awake()
        {
            if (loadButton != null)
                loadButton.onClick.AddListener(() => _onLoad?.Invoke());
        }

        public void Setup(string name, System.Action onLoad)
        {
            _onLoad = onLoad;
            if (levelNameText != null)
                levelNameText.text = name;
        }
    }
}
