using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Events;
using PuzzleGame.Presentation;

namespace PuzzleGame.Tests.PlayMode
{
    public class LevelLifecyclePlayModeTests
    {
        private IObjectResolver _resolver;
        private IEventAggregator _eventAggregator;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var builder = new ContainerBuilder();
            builder.Register<IEventAggregator, EventAggregator>(Lifetime.Singleton);
            _resolver = builder.Build();
            _eventAggregator = _resolver.Resolve<IEventAggregator>();
            yield return null;
        }

        [UnityTest]
        public IEnumerator EventAggregator_PublishSubscribe_Roundtrip()
        {
            bool received = false;
            _eventAggregator.Subscribe<LevelSelectedEvent>(e =>
            {
                received = true;
                Assert.AreEqual(1, e.LevelNumber);
            });

            _eventAggregator.Publish(new LevelSelectedEvent(1));
            yield return null;

            Assert.IsTrue(received, "Event was not received by subscriber");
        }
    }
}
