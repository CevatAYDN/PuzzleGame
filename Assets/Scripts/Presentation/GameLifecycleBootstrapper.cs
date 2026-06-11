using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;

namespace PuzzleGame.Presentation
{
    public class GameLifecycleBootstrapper : IInitializable
    {
        private readonly GameConfig _gameConfig;
        private readonly AudioConfig _audioConfig;
        private readonly IShaderOptimizer _shaderOptimizer;

        public GameLifecycleBootstrapper(GameConfig gameConfig, AudioConfig audioConfig, IShaderOptimizer shaderOptimizer)
        {
            _gameConfig = gameConfig;
            _audioConfig = audioConfig;
            _shaderOptimizer = shaderOptimizer;
        }

        public void Initialize()
        {
            MoldLogger.LogInfo("GameLifecycleBootstrapper: initializing game systems.");
            
            double refreshRate = Screen.currentResolution.refreshRateRatio.value;
            UnityEngine.Application.targetFrameRate = refreshRate > 0
                ? (int)Math.Round(refreshRate)
                : 60;
            MoldLogger.LogInfo($"Target frame rate set to: {UnityEngine.Application.targetFrameRate} FPS");

            _shaderOptimizer?.Initialize(_gameConfig != null && _gameConfig.applyMobileShaderDefaults);
            InitAudio();
        }

        private void InitAudio()
        {
            if (_audioConfig == null)
            {
                MoldLogger.LogWarning("AudioConfig is null — audio init skipped.");
            }
        }
    }
}
