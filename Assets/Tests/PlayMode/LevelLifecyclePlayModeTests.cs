using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer;
using VContainer.Unity;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Events;
using PuzzleGame.Presentation;

namespace PuzzleGame.Tests.PlayMode
{
    public class LevelLifecyclePlayModeTests
    {
        private IObjectResolver _resolver;
        private LevelFlowController _flowController;
        private ILevelRepository _levelRepository;
        private IEventPublisher _eventPublisher;
        private ISaveStorage _saveStorage;
        
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Initialize DI container for isolated test environment
            var builder = new ContainerBuilder();
            
            // Register production modules and services (similar to GameInstaller)
            builder.Register<IEventPublisher, EventBus>(Lifetime.Singleton);
            builder.Register<IAnalyticsService, NoOpAnalyticsService>(Lifetime.Singleton);
            builder.Register<ISaveStorage, SecureFileLevelProgressService>(Lifetime.Singleton);
            builder.Register<ILevelRepository, ResourcesLevelRepository>(Lifetime.Singleton); // Using real data
            builder.Register<WinLoseEvaluator>(Lifetime.Singleton);
            builder.Register<LevelFlowController>(Lifetime.Singleton);
            
            _resolver = builder.Build();
            
            _flowController = _resolver.Resolve<LevelFlowController>();
            _levelRepository = _resolver.Resolve<ILevelRepository>();
            _eventPublisher = _resolver.Resolve<IEventPublisher>();
            _saveStorage = _resolver.Resolve<ISaveStorage>();
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator Load_Play_Win_Save_Reload_EndToEnd_Flow()
        {
            // Load real Level 1 data
            var levelData = _levelRepository.GetByNumber(1);
            Assert.IsNotNull(levelData, "Level 1 Data not found! Real data test failed.");
            
            // Start the level
            _flowController.StartLevel(levelData);
            yield return new WaitForSeconds(0.1f); // Wait for state machine transition
            
            // Simulate winning by directly publishing event
            // (WinLoseEvaluator will listen and call CheckWin())
            _eventPublisher.Publish(new CastCompletedEvent { IsLevelComplete = true });
            yield return new WaitForSeconds(0.1f); // Wait for win animation/state update
            
            // Verify save was written to disk
            bool isSaved = _saveStorage.HasSaveData();
            Assert.IsTrue(isSaved, "WinLoseEvaluator detected win but SaveStorage failed to write to disk!");
            
            // Verify progress was loaded correctly
            var progress = _saveStorage.LoadProgress();
            Assert.AreEqual(2, progress.UnlockedLevel, "Saved progress is incorrect. After completing Level 1, UnlockedLevel should be 2.");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _resolver?.Dispose();
            yield return null;
        }
    }
}
