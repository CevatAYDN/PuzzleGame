using UnityEngine;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;

namespace PuzzleGame.Presentation
{
    /// <summary>
    /// Decoupled play-test bootstrapping component.
    /// SRP: Manages detection and entry of play-test mode (e.g. active Kalıp controllers but fallback menu).
    /// </summary>
    public class PlayTestBootstrap : MonoBehaviour, IFallbackMarker
    {
        public bool IsFallback { get; private set; }

        public void MarkAsFallback()
        {
            IsFallback = true;
        }

        private MoldPoolInitializer _moldPoolInitializer;
        private IGameStateMachine _stateMachine;

        public void Initialize(MoldPoolInitializer moldPoolInitializer, IGameStateMachine stateMachine)
        {
            _moldPoolInitializer = moldPoolInitializer;
            _stateMachine = stateMachine;
        }

        [SerializeField] private bool _forcePlayTestMode = false;
        public bool ForcePlayTestMode 
        {
            get => _forcePlayTestMode;
            set => _forcePlayTestMode = value;
        }

        public bool TryEnterPlayTestMode(bool isFallbackMenuActive)
        {
            if (!_forcePlayTestMode && !isFallbackMenuActive)
                return false;

            var moldsInScene = FindObjectsByType<MoldController>(FindObjectsInactive.Exclude);
            if (moldsInScene.Length == 0)
                return false;

            if (_forcePlayTestMode)
                MoldLogger.LogInfo("[PlayTest] ForcePlayTestMode is TRUE. Ignoring MainMenu and initializing Play-Test mode.");
            else
                MoldLogger.LogInfo("[PlayTest] Fallback Menu detected with Molds in scene. Initializing Play-Test mode via PlayTestBootstrap.");
            
            _moldPoolInitializer?.InitializeForLevel(null);
            _stateMachine?.TransitionTo(GameState.Playing);
            return true;
        }
    }
}
