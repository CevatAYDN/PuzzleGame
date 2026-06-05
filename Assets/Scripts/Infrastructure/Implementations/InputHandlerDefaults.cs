using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using UnityEngine;

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// Play-test default <see cref="LevelData"/> with disabled optional
    /// features (multi-layer cast, reactions). Cached singleton instance,
    /// destroyed on <see cref="Dispose"/> to prevent editor asset leaks.
    /// </summary>
    public sealed class InputHandlerDefaults : IInputHandlerDefaults
    {
        private LevelData _playTestDefaults;

        public LevelData GetActiveLevelData(LevelData currentLevelData)
        {
            if (currentLevelData != null) return currentLevelData;

            if (_playTestDefaults == null)
            {
                _playTestDefaults = ScriptableObject.CreateInstance<LevelData>();
                _playTestDefaults.autoGenerate = false;
                _playTestDefaults.enableMultiLayerCast = false;
                _playTestDefaults.enableReactionSystem = false;
                _playTestDefaults.hideFlags = HideFlags.HideAndDontSave;
                _playTestDefaults.name = "InputHandlerService_PlayTestDefaults";
            }

            MoldLogger.LogDebug("GetActiveLevelData: no level set, using play-test defaults.");
            return _playTestDefaults;
        }

        public void Dispose()
        {
            if (_playTestDefaults != null)
            {
                Object.Destroy(_playTestDefaults);
                _playTestDefaults = null;
            }
        }
    }
}
