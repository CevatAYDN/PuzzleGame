using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Nexus.Core;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Events;
using PuzzleGame.Presentation;

namespace PuzzleGame.Tests.PlayMode
{
    public class LevelLifecyclePlayModeTests
    {
        private NexusDI _resolver;
        private IEventAggregator _eventAggregator;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var context = new Context();
            context.Container.Bind<IEventAggregator, EventAggregator>();
            _resolver = context.Container;
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
