using NUnit.Framework;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Domain.Models;
using PuzzleGame.Presentation;
using PuzzleGame.Tests.Fakes;
using System.Collections.Generic;
using UnityEngine;

namespace PuzzleGame.Tests.Presentation
{
    public class WinLoseEvaluatorTests
    {
        private WinLoseEvaluator _sut;
        private FakeGameStateMachine _state;
        private FakeMoldValidator _validator;
        private FakeAudioService _audio;
        private FakeLevelProgressService _progress;
        private FakeHistoryManager _history;
        private FakeTweenService _tween;
        private EventAggregator _events;
        private FakeActiveMoldsProvider _pool;

        private LevelData _level;
        private readonly List<GameObject> _tempGameObjects = new List<GameObject>();

        [SetUp]
        public void Setup()
        {
            _state = new FakeGameStateMachine { Current = GameState.Playing };
            _validator = new FakeMoldValidator();
            _audio = new FakeAudioService();
            _progress = new FakeLevelProgressService();
            _history = new FakeHistoryManager();
            _tween = new FakeTweenService();
            _events = new EventAggregator();
            _pool = new FakeActiveMoldsProvider();

            _level = ScriptableObject.CreateInstance<LevelData>();
            _level.levelNumber = 1;
            _level.parMoves = 5;
            _level.goodMoves = 10;

            _sut = new WinLoseEvaluator(
                _state, _validator, _audio, _progress,
                _history, _tween, _events, _pool);
        }

        [TearDown]
        public void Teardown()
        {
            _sut?.Dispose();
            _events?.Clear();
            if (_level != null) Object.DestroyImmediate(_level);
            foreach (var go in _tempGameObjects)
                if (go != null) Object.DestroyImmediate(go);
            _tempGameObjects.Clear();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test helpers
        // ─────────────────────────────────────────────────────────────────────

        private FakeMoldView BuildNonEmptyView(int maxLayers = 4)
        {
            var state = new MoldState(maxLayers);
            state.AddLayer(new OreLayer(new DomainColor(1f, 0f, 0f), 0.25f));
            return new FakeMoldView(state);
        }

        private FakeMoldView BuildEmptyView(int maxLayers = 4)
        {
            return new FakeMoldView(new MoldState(maxLayers));
        }

        private FakeMoldView BuildNonEmptyViewNamed(string name, int maxLayers = 4)
        {
            var view = BuildNonEmptyView(maxLayers);
            var go = new GameObject(name);
            _tempGameObjects.Add(go);
            view.GameObject = go;
            return view;
        }

        private FakeMoldView BuildEmptyViewNamed(string name, int maxLayers = 4)
        {
            var view = BuildEmptyView(maxLayers);
            var go = new GameObject(name);
            _tempGameObjects.Add(go);
            view.GameObject = go;
            return view;
        }

        private void PublishCastCompleted()
        {
            _events.Publish(new CastCompletedEvent(new MoldState(4), new MoldState(4)));
        }

        // ─────────────────────────────────────────────────────────────────────
        // Lifecycle
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void Dispose_UnsubscribesFromCastCompleted()
        {
            _sut.Dispose();
            PublishCastCompleted();
            Assert.That(_tween.DelayCallCount, Is.EqualTo(0));
        }

        [Test]
        public void Dispose_CanBeCalledTwice_WithoutException()
        {
            _sut.Dispose();
            Assert.DoesNotThrow(() => _sut.Dispose());
        }

        [Test]
        public void OnLevelLoaded_StoresLevel_UsedDuringCheckWin()
        {
            _events.Publish(new LevelLoadedEvent(_level));
            _history.CurrentMoveCount = 3;
            _validator.IsCompleteResult = true;
            _pool.Molds = new IMoldView[] { BuildNonEmptyView() };

            PublishCastCompleted();

            Assert.That(_progress.LastRecordedLevel, Is.EqualTo(1));
            Assert.That(_progress.LastRecordedMoves, Is.EqualTo(3));
        }

        // ─────────────────────────────────────────────────────────────────────
        // CompleteWithOptionalRewards guard
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void CompleteWithOptionalRewards_NotInOptionalCastingState_DoesNothing()
        {
            _state.Current = GameState.Playing;
            _validator.IsCompleteResult = true;
            _pool.Molds = new IMoldView[] { BuildNonEmptyView() };

            _sut.CompleteWithOptionalRewards();

            Assert.That(_state.TransitionToCallCount, Is.EqualTo(0));
            Assert.That(_progress.RecordCompletionCallCount, Is.EqualTo(0));
        }

        [Test]
        public void CompleteWithOptionalRewards_InOptionalCastingState_FinalizesLevel()
        {
            _events.Publish(new LevelLoadedEvent(_level));
            _level.optionalTargets.Add(new OptionalTargetData());
            _validator.IsCompleteResult = true;
            _pool.Molds = new IMoldView[] { BuildNonEmptyView() };

            PublishCastCompleted();
            Assert.That(_state.Current, Is.EqualTo(GameState.OptionalCasting));

            _sut.CompleteWithOptionalRewards();

            Assert.That(_progress.RecordCompletionCallCount, Is.EqualTo(1));
            Assert.That(_state.TransitionToCallCount, Is.EqualTo(2),
                "Expected one transition into OptionalCasting then one into LevelComplete.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // OnCastCompleted trigger
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void OnCastCompleted_SchedulesDelayedCheckWin()
        {
            _pool.Molds = System.Array.Empty<IMoldView>();

            PublishCastCompleted();

            Assert.That(_tween.DelayCallCount, Is.EqualTo(1));
        }

        // ─────────────────────────────────────────────────────────────────────
        // CheckWin early-exit branches
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void CheckWin_PoolMoldsIsNull_DoesNotFinalize()
        {
            _pool.Molds = null;

            PublishCastCompleted();

            Assert.That(_state.TransitionToCallCount, Is.EqualTo(0));
            Assert.That(_audio.PlaySfxCallCount, Is.EqualTo(0));
        }

        [Test]
        public void CheckWin_PoolMoldsIsEmpty_DoesNotFinalize()
        {
            _pool.Molds = System.Array.Empty<IMoldView>();

            PublishCastCompleted();

            Assert.That(_state.TransitionToCallCount, Is.EqualTo(0));
            Assert.That(_audio.PlaySfxCallCount, Is.EqualTo(0));
        }

        [Test]
        public void CheckWin_AllMoldsEmpty_DoesNotFinalize()
        {
            _pool.Molds = new IMoldView[] { BuildEmptyView(), BuildEmptyView() };

            PublishCastCompleted();

            Assert.That(_state.TransitionToCallCount, Is.EqualTo(0));
            Assert.That(_audio.PlaySfxCallCount, Is.EqualTo(0));
        }

        [Test]
        public void CheckWin_AnyMoldIncomplete_DoesNotFinalize()
        {
            _validator.IsCompleteResult = false;
            _pool.Molds = new IMoldView[] { BuildNonEmptyView() };

            PublishCastCompleted();

            Assert.That(_state.TransitionToCallCount, Is.EqualTo(0));
            Assert.That(_audio.PlaySfxCallCount, Is.EqualTo(0));
        }

        // ─────────────────────────────────────────────────────────────────────
        // CheckWin winning branch — no optional targets
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void CheckWin_AllCompleteNoOptionalTargets_TransitionsToLevelComplete()
        {
            _events.Publish(new LevelLoadedEvent(_level));
            _validator.IsCompleteResult = true;
            _pool.Molds = new IMoldView[] { BuildNonEmptyView(), BuildNonEmptyView() };

            PublishCastCompleted();

            Assert.That(_state.LastTransitionTo, Is.EqualTo(GameState.LevelComplete));
            Assert.That(_state.TransitionToCallCount, Is.EqualTo(1));
        }

        [Test]
        public void CheckWin_AllCompleteNoOptionalTargets_PlaysLevelCompleteSfx()
        {
            _events.Publish(new LevelLoadedEvent(_level));
            _validator.IsCompleteResult = true;
            _pool.Molds = new IMoldView[] { BuildNonEmptyView() };

            PublishCastCompleted();

            Assert.That(_audio.LastSfxId, Is.EqualTo(AudioClipId.LevelComplete));
            Assert.That(_audio.PlaySfxCallCount, Is.EqualTo(1));
        }

        [Test]
        public void CheckWin_AllComplete_AnimatesCompletionOnNonCappedMolds()
        {
            _events.Publish(new LevelLoadedEvent(_level));
            _validator.IsCompleteResult = true;

            var capped = BuildNonEmptyView();
            capped.IsCapped = true;
            var open = BuildNonEmptyView();
            _pool.Molds = new IMoldView[] { capped, open };

            PublishCastCompleted();

            Assert.That(capped.AnimateCompletionCallCount, Is.EqualTo(0),
                "Capped molds should not re-animate completion.");
            Assert.That(open.AnimateCompletionCallCount, Is.EqualTo(1));
        }

        // ─────────────────────────────────────────────────────────────────────
        // CheckWin winning branch — with optional targets
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void CheckWin_AllCompleteWithOptionalTargets_TransitionsToOptionalCasting()
        {
            _events.Publish(new LevelLoadedEvent(_level));
            _level.optionalTargets.Add(new OptionalTargetData());
            _validator.IsCompleteResult = true;
            _pool.Molds = new IMoldView[] { BuildNonEmptyView() };

            PublishCastCompleted();

            Assert.That(_state.LastTransitionTo, Is.EqualTo(GameState.OptionalCasting));
        }

        [Test]
        public void CheckWin_AllCompleteWithOptionalTargets_ActivatesOptionalMolds()
        {
            _events.Publish(new LevelLoadedEvent(_level));
            _level.optionalTargets.Add(new OptionalTargetData());
            _validator.IsCompleteResult = true;
            _pool.Molds = new IMoldView[] { BuildNonEmptyView() };

            PublishCastCompleted();

            Assert.That(_pool.ActivateOptionalMoldsCallCount, Is.EqualTo(1));
            Assert.That(_pool.LastActivatedLevel, Is.SameAs(_level));
        }

        [Test]
        public void CheckWin_AllCompleteWithOptionalTargets_DoesNotRecordProgressYet()
        {
            _events.Publish(new LevelLoadedEvent(_level));
            _level.optionalTargets.Add(new OptionalTargetData());
            _validator.IsCompleteResult = true;
            _pool.Molds = new IMoldView[] { BuildNonEmptyView() };

            PublishCastCompleted();

            Assert.That(_progress.RecordCompletionCallCount, Is.EqualTo(0),
                "Progress should be recorded only on CompleteWithOptionalRewards.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // FinalizeLevel — completion + event
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void FinalizeLevel_RecordsCompletionWithMoveCountAndStars()
        {
            _events.Publish(new LevelLoadedEvent(_level));
            _history.CurrentMoveCount = 3;
            _validator.IsCompleteResult = true;
            _pool.Molds = new IMoldView[] { BuildNonEmptyView() };

            PublishCastCompleted();

            Assert.That(_progress.LastRecordedLevel, Is.EqualTo(1));
            Assert.That(_progress.LastRecordedMoves, Is.EqualTo(3));
            Assert.That(_progress.LastRecordedStars, Is.EqualTo(3),
                "3 moves <= parMoves=5 should yield 3 stars.");
        }

        [Test]
        public void FinalizeLevel_PublishesLevelCompletedEvent()
        {
            _events.Publish(new LevelLoadedEvent(_level));
            _history.CurrentMoveCount = 7;
            _validator.IsCompleteResult = true;
            _pool.Molds = new IMoldView[] { BuildNonEmptyView() };

            LevelCompletedEvent? observed = null;
            _events.Subscribe<LevelCompletedEvent>(e => observed = e);

            PublishCastCompleted();

            Assert.That(observed, Is.Not.Null);
            Assert.That(observed.Value.MoveCount, Is.EqualTo(7));
        }

        [Test]
        public void FinalizeLevel_StarsReflectMoveCount_BeyondGoodThreshold_OneStar()
        {
            _events.Publish(new LevelLoadedEvent(_level));
            _history.CurrentMoveCount = 20;
            _validator.IsCompleteResult = true;
            _pool.Molds = new IMoldView[] { BuildNonEmptyView() };

            PublishCastCompleted();

            Assert.That(_progress.LastRecordedStars, Is.EqualTo(1));
        }

        [Test]
        public void FinalizeLevel_NoLevelLoaded_DefaultsToThreeStars()
        {
            _validator.IsCompleteResult = true;
            _pool.Molds = new IMoldView[] { BuildNonEmptyView() };

            PublishCastCompleted();

            Assert.That(_state.LastTransitionTo, Is.EqualTo(GameState.LevelComplete));
            Assert.That(_progress.LastRecordedStars, Is.EqualTo(3));
            Assert.That(_progress.RecordCompletionCallCount, Is.EqualTo(0),
                "RecordCompletion must not be called when no level was loaded.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Perfect Forge bonus
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void FinalizeLevel_OptionalTargetFilled_AwardsPerfectForgeBonus()
        {
            _events.Publish(new LevelLoadedEvent(_level));
            _level.optionalTargets.Add(new OptionalTargetData());
            _history.CurrentMoveCount = 8;
            _validator.IsCompleteResult = true;

            var regular = BuildNonEmptyViewNamed("Mold_0");
            var optional = BuildNonEmptyViewNamed("Optional_Sword_0");
            _pool.Molds = new IMoldView[] { regular, optional };

            PublishCastCompleted();
            _sut.CompleteWithOptionalRewards();

            Assert.That(_progress.LastRecordedStars, Is.EqualTo(3),
                "2 stars (8 moves > par=5) + 1 perfect-forge bonus, capped at 3.");
        }

        [Test]
        public void FinalizeLevel_OptionalTargetNotFilled_DoesNotAwardBonus()
        {
            _events.Publish(new LevelLoadedEvent(_level));
            _level.optionalTargets.Add(new OptionalTargetData());
            _history.CurrentMoveCount = 8;
            _validator.IsCompleteResult = true;

            var regular = BuildNonEmptyViewNamed("Mold_0");
            var emptyOptional = BuildEmptyViewNamed("Optional_Sword_0");
            _pool.Molds = new IMoldView[] { regular, emptyOptional };

            PublishCastCompleted();
            _sut.CompleteWithOptionalRewards();

            Assert.That(_progress.LastRecordedStars, Is.EqualTo(2),
                "2 stars (8 moves > par=5), no bonus when optional target is empty.");
        }
    }
}
