using UnityEngine;
using UnityEngine.UI;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Events;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Application.Configuration;

namespace PuzzleGame.Presentation.UI
{
    /// <summary>
    /// Handles the Pause screen logic exclusively.
    /// Pushes focus to 'Resume' button for controller navigation.
    /// </summary>
    public class PausePanelPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject _pausePanelRoot;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _pauseMainMenuButton;
        [SerializeField] private Button _pauseRestartButton;

        private IEventAggregator _events;
        private IGameStateMachine _stateMachine;
        private LevelData _currentLevel;

        [VContainer.Inject]
        public void Construct(IEventAggregator events, IGameStateMachine stateMachine)
        {
            _events = events;
            _stateMachine = stateMachine;
        }

        private void OnEnable()
        {
            if (_events != null)
            {
                _events.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
                _events.Subscribe<LevelLoadedEvent>(OnLevelLoaded);
            }

            if (_resumeButton != null) _resumeButton.onClick.AddListener(OnResume);
            if (_pauseMainMenuButton != null) _pauseMainMenuButton.onClick.AddListener(OnMainMenu);
            if (_pauseRestartButton != null) _pauseRestartButton.onClick.AddListener(OnReplay);
        }

        private void OnDisable()
        {
            if (_events != null)
            {
                _events.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
                _events.Unsubscribe<LevelLoadedEvent>(OnLevelLoaded);
            }
        }

        private void OnLevelLoaded(LevelLoadedEvent e)
        {
            _currentLevel = e.Level;
        }

        private void OnGameStateChanged(GameStateChangedEvent e)
        {
            if (_pausePanelRoot == null) return;
            
            bool isPaused = e.Current == GameState.Paused;
            _pausePanelRoot.SetActive(isPaused);

            if (isPaused)
            {
                if (_resumeButton != null)
                {
                    UINavigationManager.Instance.PushFocus(_resumeButton.gameObject);
                }
            }
            else
            {
                UINavigationManager.Instance.PopFocus();
            }
        }

        private void OnResume()
        {
            _stateMachine?.TransitionTo(GameState.Playing);
        }

        private void OnReplay()
        {
            _stateMachine?.TransitionTo(GameState.Playing);
            _events?.Publish(new LevelSelectedEvent(_currentLevel?.levelNumber ?? 1));
        }

        private void OnMainMenu()
        {
            _stateMachine?.TransitionTo(GameState.Menu);
        }
    }
}
