using System;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;

namespace PuzzleGame.Presentation
{
    /// <summary>
    /// Observes gameplay events (cast complete, cast rejected, level complete)
    /// and triggers the appropriate haptic feedback intensity.
    /// POCO managed by DI container.
    /// </summary>
    public sealed class HapticObserver : IDisposable
    {
        private const string LogTag = "[HapticObserver]";

        private readonly IEventAggregator _eventAggregator;
        private readonly IHapticFeedbackService _hapticService;
        private bool _isSubscribed;

        public HapticObserver(IEventAggregator eventAggregator, IHapticFeedbackService hapticService)
        {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _hapticService = hapticService ?? throw new ArgumentNullException(nameof(hapticService));

            _eventAggregator.Subscribe<CastCompletedEvent>(OnCastCompleted);
            _eventAggregator.Subscribe<CastRejectedEvent>(OnCastRejected);
            _eventAggregator.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
            _isSubscribed = true;

            MoldLogger.LogInfo($"{LogTag} Subscribed to gameplay events.");
        }

        private void OnCastCompleted(CastCompletedEvent evt)
        {
            _hapticService.Trigger(HapticIntensity.PourComplete);
        }

        private void OnCastRejected(CastRejectedEvent evt)
        {
            _hapticService.Trigger(HapticIntensity.Error);
        }

        private void OnLevelCompleted(LevelCompletedEvent evt)
        {
            _hapticService.Trigger(HapticIntensity.Success);
        }

        public void Dispose()
        {
            if (_isSubscribed && _eventAggregator != null)
            {
                _eventAggregator.Unsubscribe<CastCompletedEvent>(OnCastCompleted);
                _eventAggregator.Unsubscribe<CastRejectedEvent>(OnCastRejected);
                _eventAggregator.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
                _isSubscribed = false;
                
                MoldLogger.LogInfo($"{LogTag} Unsubscribed and disposed.");
            }
        }
    }
}
