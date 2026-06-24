using NUnit.Framework;
using UnityEngine;
using Nexus.Core;
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
    /// </summary>
    [TestFixture]
    public class CoreServicesInstallerTests
    {
        private NexusDI _container;

        [TearDown]
        public void Teardown()
        {
            _container?.Dispose();
            _container = null;
        }

        [Test]
        public void CoreServices_RegistersEventAggregator()
        {
            var context = new Context();
            var builder = new ContextBuilder(context.Container, context.SignalBusInternal);
            CoreServicesInstallerModule.Configure(builder, context.Container);
            _container = context.Container;
            Assert.IsNotNull(_container.Resolve<IEventAggregator>(),
                "EventAggregator must be registered as a core service.");
        }

        [Test]
        public void CoreServices_RegistersPourDebugController()
        {
            var context = new Context();
            var builder = new ContextBuilder(context.Container, context.SignalBusInternal);
            
            // Provide GameConfig which IMoldValidator depends on
            context.Container.BindInstance(ScriptableObject.CreateInstance<GameConfig>());
            // Provide AnimationConfig which AnimationService depends on
            context.Container.BindInstance(ScriptableObject.CreateInstance<AnimationConfig>());
            // Provide AudioConfig which AudioService depends on
            context.Container.BindInstance(ScriptableObject.CreateInstance<AudioConfig>());
            // Provide StreamVFXConfig which StreamRenderer depends on
            context.Container.BindInstance(ScriptableObject.CreateInstance<StreamVFXConfig>());
            // Provide IFeatureFlagService which AnimationService depends on
            context.Container.Bind<IFeatureFlagService, PuzzleGame.Tests.Fakes.FakeFeatureFlagService>();
            // Provide IErrorIndicatorService which CastService depends on
            context.Container.Bind<IErrorIndicatorService, PuzzleGame.Tests.Fakes.FakeErrorIndicatorService>();
            
            CoreServicesInstallerModule.Configure(builder, context.Container); // Provides IMoldValidator needed by CastService
            GameplayInstallerModule.Configure(builder, context.Container);
            
            _container = context.Container;
            Assert.IsNotNull(_container.Resolve<IPourDebugController>(),
                "IPourDebugController (facade portion of IPourSystemController) " +
                "must be resolvable after the gameplay module is configured.");
        }

        [Test]
        public void CoreServices_EventAggregator_IsSingleton()
        {
            var context = new Context();
            var builder = new ContextBuilder(context.Container, context.SignalBusInternal);
            CoreServicesInstallerModule.Configure(builder, context.Container);
            _container = context.Container;

            var a = _container.Resolve<IEventAggregator>();
            var b = _container.Resolve<IEventAggregator>();
            Assert.AreSame(a, b,
                "EventAggregator must be registered as Singleton — multiple " +
                "instances would break cross-module pub/sub.");
        }
    }
}
