using NUnit.Framework;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Infrastructure.Implementations;

namespace PuzzleGame.Tests.Infrastructure
{
    [TestFixture]
    public class ConsentManagerTests
    {
        private ConsentManager _consent;

        [SetUp]
        public void SetUp()
        {
            _consent = new ConsentManager();
        }

        [Test]
        public void NewInstance_NotReadyAndUnknown()
        {
            Assert.IsFalse(_consent.IsReady);
            Assert.AreEqual(AdConsentState.Unknown, _consent.ConsentState);
        }

        [Test]
        public void Initialize_NoPackage_AcceptsFallback()
        {
            _consent.Initialize(isUnder13: false);

            Assert.IsTrue(_consent.IsReady);
        }

        [Test]
        public void Initialize_Under13Flag_Persists()
        {
            _consent.Initialize(isUnder13: true);
            Assert.IsTrue(_consent.IsUnder13);
        }

        [Test]
        public void RequestConsentIfNeeded_NoPackage_InvokesAcceptedCallback()
        {
            AdConsentState? received = null;
            _consent.RequestConsentIfNeeded(state => received = state);

            Assert.AreEqual(AdConsentState.Accepted, received);
            Assert.AreEqual(AdConsentState.Accepted, _consent.ConsentState);
        }

        [Test]
        public void SetConsent_UpdatesState()
        {
            _consent.SetConsent(AdConsentState.Rejected, false);
            Assert.AreEqual(AdConsentState.Rejected, _consent.ConsentState);
        }

        [Test]
        public void ResetConsent_ClearsToUnknown()
        {
            _consent.SetConsent(AdConsentState.Accepted, true);
            _consent.ResetConsent();
            Assert.AreEqual(AdConsentState.Unknown, _consent.ConsentState);
        }
    }
}
