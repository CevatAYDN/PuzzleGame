using System;
using TMPro;
using UnityEngine;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Events;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Presentation.UI
{
    /// <summary>
    /// Coordinates global HUD overlays such as Loading and DI Error panels.
    /// In-game widgets, pause, and win panels have been moved to their respective
    /// specialized presenters (HUDWidgetPresenter, PausePanelPresenter, WinPanelPresenter).
    /// </summary>
    public sealed class HudPresenter : MonoBehaviour, IDisposable
    {
        [Header("Global Overlays")]
        [SerializeField] private GameObject _loadingPanel;
        [SerializeField] private GameObject _diErrorPanel;
        [SerializeField] private TextMeshProUGUI _diErrorText;

        private IEventAggregator _events;

        [VContainer.Inject]
        public void Construct(IEventAggregator events)
        {
            _events = events;
        }

        private void OnEnable()
        {
            if (_events == null) return;
            _events.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnDisable()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (_events == null) return;
            _events.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
            _events = null;
        }

        private void Start()
        {
            if (_loadingPanel != null) _loadingPanel.SetActive(false);
            if (_diErrorPanel != null) _diErrorPanel.SetActive(false);
        }

        public void ShowDIError(string message)
        {
            if (_diErrorPanel != null) _diErrorPanel.SetActive(true);
            if (_diErrorText != null) _diErrorText.text = message ?? "VContainer DI failed.";
        }

        private void OnGameStateChanged(GameStateChangedEvent e)
        {
            if (_loadingPanel != null)
            {
                _loadingPanel.SetActive(e.Current == GameState.LevelLoading);
            }
        }
    }
}
