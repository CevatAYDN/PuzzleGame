using NUnit.Framework;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Services;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Tests.Fakes;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Tests.Application.Services
{
    public class TutorialServiceTests
    {
        private TutorialService _sut;
        private EventAggregator _events;
        private FakeMoldSelectionService _selection;

        [SetUp]
        public void Setup()
        {
            UnityEngine.PlayerPrefs.DeleteKey("PuzzleGame.TutorialCompleted");
            _events = new EventAggregator();
            _selection = new FakeMoldSelectionService();
            _sut = new TutorialService(_events, _selection);
        }

        [TearDown]
        public void Teardown()
        {
            UnityEngine.PlayerPrefs.DeleteKey("PuzzleGame.TutorialCompleted");
            _events?.Clear();
        }

        [Test]
        public void InitialState_IsInactive()
        {
            Assert.That(_sut.IsActive, Is.False);
            Assert.That(_sut.CurrentStep, Is.EqualTo(TutorialStep.Inactive));
        }

        [Test]
        public void Begin_StartsTutorialAtWelcome()
        {
            _sut.Begin();
            Assert.That(_sut.IsActive, Is.True);
            Assert.That(_sut.CurrentStep, Is.EqualTo(TutorialStep.Welcome));
        }

        [Test]
        public void Begin_AlreadyCompleted_DoesNotStart()
        {
            UnityEngine.PlayerPrefs.SetInt("PuzzleGame.TutorialCompleted", 1);
            UnityEngine.PlayerPrefs.Save();
            _sut.Begin();
            Assert.That(_sut.IsActive, Is.False);
        }

        [Test]
        public void MoldSelected_AdvancesFromWelcomeToTapToSelect()
        {
            _sut.Begin();
            _selection.RaiseSelected(new MoldState(4));
            Assert.That(_sut.CurrentStep, Is.EqualTo(TutorialStep.TapToSelect));
        }

        [Test]
        public void MoldSelected_WhileNotActive_Ignored()
        {
            _selection.RaiseSelected(new MoldState(4));
            Assert.That(_sut.CurrentStep, Is.EqualTo(TutorialStep.Inactive));
        }

        [Test]
        public void CastCompleted_AdvancesToLevelComplete()
        {
            _sut.Begin();
            _selection.RaiseSelected(new MoldState(4));
            _events.Publish(new CastCompletedEvent(null, null));
            Assert.That(_sut.CurrentStep, Is.EqualTo(TutorialStep.LevelComplete));
        }

        [Test]
        public void LevelCompleted_PersistsCompletion()
        {
            _sut.Begin();
            _selection.RaiseSelected(new MoldState(4));
            _events.Publish(new CastCompletedEvent(null, null));

            Assert.That(UnityEngine.PlayerPrefs.GetInt("PuzzleGame.TutorialCompleted", 0), Is.EqualTo(1));
        }

        [Test]
        public void Skip_MarksCompletedAndDeactivates()
        {
            _sut.Begin();
            _sut.Skip();
            Assert.That(_sut.IsActive, Is.False);
            Assert.That(UnityEngine.PlayerPrefs.GetInt("PuzzleGame.TutorialCompleted", 0), Is.EqualTo(1));
        }

        [Test]
        public void Begin_AfterCompletion_DoesNotReactivate()
        {
            _sut.Begin();
            _selection.RaiseSelected(new MoldState(4));
            _events.Publish(new CastCompletedEvent(null, null));
            _sut.Begin();
            Assert.That(_sut.IsActive, Is.False);
        }

        [Test]
        public void Reset_ClearsPersistedCompletion()
        {
            _sut.Begin();
            _selection.RaiseSelected(new MoldState(4));
            _events.Publish(new CastCompletedEvent(null, null));
            _sut.Reset();
            Assert.That(UnityEngine.PlayerPrefs.GetInt("PuzzleGame.TutorialCompleted", 0), Is.EqualTo(0));
            Assert.That(_sut.CurrentStep, Is.EqualTo(TutorialStep.Inactive));
        }

        [Test]
        public void CurrentMessageKey_MapsStepToLocalizationKey()
        {
            _sut.Begin();
            Assert.That(_sut.CurrentMessageKey, Is.EqualTo("tutorial_welcome"));
            _selection.RaiseSelected(new MoldState(4));
            Assert.That(_sut.CurrentMessageKey, Is.EqualTo("tutorial_tap_to_select"));
            _events.Publish(new CastCompletedEvent(null, null));
            Assert.That(_sut.CurrentMessageKey, Is.EqualTo("tutorial_well_done"));
        }

        [Test]
        public void OnStepChanged_FiresOnTransition()
        {
            _sut.Begin();
            TutorialStep? observed = null;
            _sut.OnStepChanged += step => observed = step;
            _selection.RaiseSelected(new MoldState(4));
            Assert.That(observed, Is.EqualTo(TutorialStep.TapToSelect));
        }
    }
}
