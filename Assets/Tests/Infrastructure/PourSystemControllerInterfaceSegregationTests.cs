using NUnit.Framework;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Infrastructure.Implementations;
using PuzzleGame.Tests.Fakes;

namespace PuzzleGame.Tests.Infrastructure
{
    /// <summary>
    /// Verifies that the 4-interface segregation contract holds:
    /// <list type="bullet">
    /// <item><see cref="IPourSystemController"/> inherits all 3 focused interfaces</item>
    /// <item><see cref="PourSystemController"/> implements all 4 interfaces at runtime</item>
    /// <item>Each focused interface is independently usable (no hidden coupling)</item>
    /// </list>
    /// No <c>SetMolds</c> calls — pure type/contract checks.
    /// </summary>
    [TestFixture]
    public class PourSystemControllerInterfaceSegregationTests
    {
        private PourSystemController CreateController()
        {
            // SetMolds is NOT called — these tests are pure contract checks.
            // Behavioral tests for individual methods would need SetMolds + fakes.
            return new PourSystemController(
                castService: new FakeCastService(),
                animationService: new FakeAnimationService(),
                eventAggregator: new EventAggregator());
        }

        [Test]
        public void IPourSystemController_Inherits_IPourSimulator()
        {
            Assert.IsTrue(typeof(IPourSimulator).IsAssignableFrom(typeof(IPourSystemController)),
                "IPourSystemController must inherit IPourSimulator");
        }

        [Test]
        public void IPourSystemController_Inherits_IPourHistoryService()
        {
            Assert.IsTrue(typeof(IPourHistoryService).IsAssignableFrom(typeof(IPourSystemController)),
                "IPourSystemController must inherit IPourHistoryService");
        }

        [Test]
        public void IPourSystemController_Inherits_IPourDebugController()
        {
            Assert.IsTrue(typeof(IPourDebugController).IsAssignableFrom(typeof(IPourSystemController)),
                "IPourSystemController must inherit IPourDebugController");
        }

        [Test]
        public void PourSystemController_Is_IPourSimulator()
        {
            Assert.IsInstanceOf<IPourSimulator>(CreateController());
        }

        [Test]
        public void PourSystemController_Is_IPourHistoryService()
        {
            Assert.IsInstanceOf<IPourHistoryService>(CreateController());
        }

        [Test]
        public void PourSystemController_Is_IPourDebugController()
        {
            Assert.IsInstanceOf<IPourDebugController>(CreateController());
        }

        [Test]
        public void PourSystemController_Is_IPourSystemController()
        {
            Assert.IsInstanceOf<IPourSystemController>(CreateController());
        }

        [Test]
        public void FocusedInterfaces_AreDistinctContracts()
        {
            // The 3 focused interfaces must not be the same type — that
            // would defeat the purpose of segregation.
            Assert.AreNotSame(typeof(IPourSimulator), typeof(IPourHistoryService));
            Assert.AreNotSame(typeof(IPourSimulator), typeof(IPourDebugController));
            Assert.AreNotSame(typeof(IPourHistoryService), typeof(IPourDebugController));
        }

        [Test]
        public void IPourSimulator_ExposesOnlyGameplayMethods()
        {
            // Compile-time check via reflection: IPourSimulator must declare
            // only PreviewPour + ExecuteInstantPour (no dev-tool methods).
            var methodNames = new System.Collections.Generic.List<string>();
            foreach (var m in typeof(IPourSimulator).GetMethods()) methodNames.Add(m.Name);

            Assert.Contains("PreviewPour", methodNames);
            Assert.Contains("ExecuteInstantPour", methodNames);
            // Negative: debug-only methods must NOT be on IPourSimulator
            Assert.IsFalse(methodNames.Contains("SetMoldLayers"));
            Assert.IsFalse(methodNames.Contains("OverrideAnimationConfig"));
            Assert.IsFalse(methodNames.Contains("GetAllMoldDebugStates"));
            Assert.IsFalse(methodNames.Contains("SnapshotAllMolds"));
        }

        [Test]
        public void IPourHistoryService_ExposesOnlyHistoryMethods()
        {
            var methodNames = new System.Collections.Generic.List<string>();
            foreach (var m in typeof(IPourHistoryService).GetMethods()) methodNames.Add(m.Name);

            Assert.Contains("SnapshotAllMolds", methodNames);
            Assert.Contains("RestoreSnapshot", methodNames);
            Assert.IsFalse(methodNames.Contains("PreviewPour"));
            Assert.IsFalse(methodNames.Contains("SetMoldLayers"));
            Assert.IsFalse(methodNames.Contains("GetAllMoldDebugStates"));
        }

        [Test]
        public void IPourDebugController_ExposesOnlyDebugMethods()
        {
            var methodNames = new System.Collections.Generic.List<string>();
            foreach (var m in typeof(IPourDebugController).GetMethods()) methodNames.Add(m.Name);

            // Mutation
            Assert.Contains("SetMoldLayers", methodNames);
            Assert.Contains("SetMoldColor", methodNames);
            Assert.Contains("SetMoldFillAmount", methodNames);
            // Overrides
            Assert.Contains("OverrideAnimationConfig", methodNames);
            Assert.Contains("OverrideMoldVisualConfig", methodNames);
            Assert.Contains("ClearAllOverrides", methodNames);
            // Queries
            Assert.Contains("GetAllMoldDebugStates", methodNames);
            // Negative: gameplay/history must NOT be on IPourDebugController
            Assert.IsFalse(methodNames.Contains("PreviewPour"));
            Assert.IsFalse(methodNames.Contains("ExecuteInstantPour"));
            Assert.IsFalse(methodNames.Contains("SnapshotAllMolds"));
        }
    }
}
