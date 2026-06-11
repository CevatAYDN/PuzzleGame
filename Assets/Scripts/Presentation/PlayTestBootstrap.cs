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

        public bool TryEnterPlayTestMode(bool isFallbackMenuActive)
        {
            if (!isFallbackMenuActive)
                return false;

            var moldsInScene = FindObjectsByType<MoldController>(FindObjectsInactive.Exclude);
            if (moldsInScene.Length == 0)
                return false;

            MoldLogger.LogInfo("[PlayTest] Fallback Menu detected with Molds in scene. Initializing Play-Test mode via PlayTestBootstrap.");
            _moldPoolInitializer?.InitializeForLevel(null);
            _stateMachine?.TransitionTo(GameState.Playing);
            return true;
        }
    }
}
