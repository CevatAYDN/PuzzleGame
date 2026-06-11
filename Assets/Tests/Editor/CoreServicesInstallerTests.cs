using NUnit.Framework;
using VContainer;
using VContainer.Unity;
using UnityEngine;
using PuzzleGame.Installers;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Services;
using PuzzleGame.Domain.Services;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Application.Configuration;

namespace PuzzleGame.Tests.Editor
{
    /// <summary>
    /// Verifies that the core services installer module registers the
    /// non-Unity-bound services that downstream layers depend on.
    ///
    /// Tests in this fixture exercise the VContainer builder directly — the
    /// MonoBehaviour-bound installer modules (UI, scene-bound services) are
    /// not exercised here and remain a separate integration concern.
    /// </summary>
    [TestFixture]
    public class CoreServicesInstallerTests
    {
        private IObjectResolver _container;

        [TearDown]
        public void Teardown()
        {
            _container?.Dispose();
            _container = null;
        }

        [Test]
        public void CoreServices_RegistersEventAggregator()
        {
            var builder = new ContainerBuilder();
            CoreServicesInstallerModule.Configure(builder);
            _container = builder.Build();
            Assert.IsNotNull(_container.Resolve<IEventAggregator>(),
                "EventAggregator must be registered as a core service.");
        }

        [Test]
        public void CoreServices_RegistersPourDebugController()
        {
            var builder = new ContainerBuilder();
            // Provide GameConfig which IMoldValidator depends on
            builder.Register<GameConfig>(resolver => ScriptableObject.CreateInstance<GameConfig>(), Lifetime.Singleton);
            // Provide AnimationConfig which AnimationService depends on
            builder.Register<AnimationConfig>(resolver => ScriptableObject.CreateInstance<AnimationConfig>(), Lifetime.Singleton);
            // Provide AudioConfig which AudioService depends on
            builder.Register<AudioConfig>(resolver => ScriptableObject.CreateInstance<AudioConfig>(), Lifetime.Singleton);
            // Provide StreamVFXConfig which StreamRenderer depends on
            builder.Register<StreamVFXConfig>(resolver => ScriptableObject.CreateInstance<StreamVFXConfig>(), Lifetime.Singleton);
            // Provide IFeatureFlagService which AnimationService depends on
            builder.Register<IFeatureFlagService, PuzzleGame.Tests.Fakes.FakeFeatureFlagService>(Lifetime.Singleton);
            // Provide IErrorIndicatorService which CastService depends on
            builder.Register<IErrorIndicatorService, PuzzleGame.Tests.Fakes.FakeErrorIndicatorService>(Lifetime.Singleton);
            CoreServicesInstallerModule.Configure(builder); // Provides IMoldValidator needed by CastService
            GameplayInstallerModule.Configure(builder);
            // PourSystemController is a plain C# class registered in
            // GameplayInstallerModule — verify the debug interface resolves.
            _container = builder.Build();
            Assert.IsNotNull(_container.Resolve<IPourDebugController>(),
                "IPourDebugController (facade portion of IPourSystemController) " +
                "must be resolvable after the gameplay module is configured.");
        }

        [Test]
        public void CoreServices_EventAggregator_IsSingleton()
        {
            var builder = new ContainerBuilder();
            CoreServicesInstallerModule.Configure(builder);
            _container = builder.Build();

            var a = _container.Resolve<IEventAggregator>();
            var b = _container.Resolve<IEventAggregator>();
            Assert.AreSame(a, b,
                "EventAggregator must be registered as Singleton — multiple " +
                "instances would break cross-module pub/sub.");
        }
    }
}
