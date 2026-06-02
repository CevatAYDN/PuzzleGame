using System.Collections.Generic;
using NUnit.Framework;
using PuzzleGame.Application.Services;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain.Models.FeatureSystem;
using PuzzleGame.Events;
using PuzzleGame.Logging;
using PuzzleGame.Tests.Fakes;
using PuzzleGame.Application.Interfaces;

namespace PuzzleGame.Tests.Application.Services
{
    public class ReactionServiceTests
    {
        private ReactionService _sut;

        [SetUp]
        public void SetUp()
        {
            BottleLogger.SetLevel(BottleLogger.Level.Error, false);
            EventAggregator.Clear();
            _sut = new ReactionService();
        }

        [TearDown]
        public void TearDown()
        {
            EventAggregator.Clear();
        }

        [Test]
        public void CheckReactions_NullBottles_ReturnsEmpty()
        {
            var config = CreateTestConfig();
            var results = _sut.CheckReactions(null, config);
            Assert.That(results.Count, Is.EqualTo(0));
        }

        [Test]
        public void CheckReactions_NullConfig_ReturnsEmpty()
        {
            var bottles = new IBottleView[] { CreateBottleWithLayers(LiquidColor.Red, LiquidColor.Blue) };
            var results = _sut.CheckReactions(bottles, null);
            Assert.That(results.Count, Is.EqualTo(0));
        }

        [Test]
        public void CheckReactions_DisabledReactions_ReturnsEmpty()
        {
            var bottles = new IBottleView[] { CreateBottleWithLayers(LiquidColor.Red, LiquidColor.Blue) };
            var config = new ReactionSystemData { enableReactions = false };

            var results = _sut.CheckReactions(bottles, config);

            Assert.That(results.Count, Is.EqualTo(0));
        }

        [Test]
        public void CheckReactions_SingleLayer_NoReaction()
        {
            var bottles = new IBottleView[] { CreateBottleWithLayers(LiquidColor.Red) };
            var config = CreateTestConfig();

            var results = _sut.CheckReactions(bottles, config);

            Assert.That(results.Count, Is.EqualTo(0));
        }

        [Test]
        public void CheckReactions_MatchingColors_TriggersReaction()
        {
            var bottles = new IBottleView[] { CreateBottleWithLayers(LiquidColor.Red, LiquidColor.Blue) };
            var config = CreateTestConfig();

            var results = _sut.CheckReactions(bottles, config);

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Rule.reactionType, Is.EqualTo(ReactionRule.ReactionType.Explode));
        }

        [Test]
        public void CheckReactions_NoMatchingRule_NoReaction()
        {
            var bottles = new IBottleView[] { CreateBottleWithLayers(LiquidColor.Green, LiquidColor.Yellow) };
            var config = CreateTestConfig(); // Rule is Red+Blue only

            var results = _sut.CheckReactions(bottles, config);

            Assert.That(results.Count, Is.EqualTo(0));
        }

        [Test]
        public void CheckReactions_Transform_CombinesLayers()
        {
            var bottle = new BottleState(4);
            bottle.AddLayer(new LiquidLayer(new DomainColor(0.9f, 0.2f, 0.2f), 1f, LiquidColor.Red));
            bottle.AddLayer(new LiquidLayer(new DomainColor(0.5f, 0.2f, 0.9f), 1f, LiquidColor.Purple));

            var view = new FakeBottleView(bottle);
            view.GameObject = new UnityEngine.GameObject("TestBottle");
            view.Transform = view.GameObject.transform;

            var config = new ReactionSystemData
            {
                enableReactions = true,
                reactionRules = new List<ReactionRule>
                {
                    new ReactionRule
                    {
                        colorA = LiquidColor.Red,
                        colorB = LiquidColor.Purple,
                        resultColor = LiquidColor.Pink,
                        reactionType = ReactionRule.ReactionType.Transform
                    }
                }
            };

            var results = _sut.CheckReactions(new IBottleView[] { view }, config);

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Rule.reactionType, Is.EqualTo(ReactionRule.ReactionType.Transform));
            // After transform: two layers become one
            Assert.That(bottle.LayerCount, Is.EqualTo(1));
        }

        [Test]
        public void CheckReactions_Explode_ClearsBottle()
        {
            var bottle = new BottleState(4);
            bottle.AddLayer(new LiquidLayer(new DomainColor(0.9f, 0.2f, 0.2f), 1f, LiquidColor.Red));
            bottle.AddLayer(new LiquidLayer(new DomainColor(0.2f, 0.2f, 0.9f), 1f, LiquidColor.Blue));

            var view = new FakeBottleView(bottle);
            view.GameObject = new UnityEngine.GameObject("TestBottle");
            view.Transform = view.GameObject.transform;

            var config = CreateTestConfig();

            var results = _sut.CheckReactions(new IBottleView[] { view }, config);

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(bottle.IsEmpty, Is.True);
        }

        [Test]
        public void CheckReactions_MultipleBottles_FindsAll()
        {
            var b1 = new BottleState(4);
            b1.AddLayer(new LiquidLayer(new DomainColor(0.9f, 0.2f, 0.2f), 1f, LiquidColor.Red));
            b1.AddLayer(new LiquidLayer(new DomainColor(0.2f, 0.2f, 0.9f), 1f, LiquidColor.Blue));

            var b2 = new BottleState(4);
            b2.AddLayer(new LiquidLayer(new DomainColor(0.9f, 0.2f, 0.2f), 1f, LiquidColor.Red));
            b2.AddLayer(new LiquidLayer(new DomainColor(0.2f, 0.2f, 0.9f), 1f, LiquidColor.Blue));

            var v1 = new FakeBottleView(b1) { GameObject = new UnityEngine.GameObject("B1"), Transform = new UnityEngine.GameObject("B1").transform };
            var v2 = new FakeBottleView(b2) { GameObject = new UnityEngine.GameObject("B2"), Transform = new UnityEngine.GameObject("B2").transform };

            var config = CreateTestConfig();

            var results = _sut.CheckReactions(new IBottleView[] { v1, v2 }, config);

            Assert.That(results.Count, Is.EqualTo(2));
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static FakeBottleView CreateBottleWithLayers(params LiquidColor[] colors)
        {
            var state = new BottleState(4);
            foreach (var c in colors)
            {
                var unityColor = c.ToDefaultColor();
                var domainColor = new DomainColor(unityColor.r, unityColor.g, unityColor.b, unityColor.a);
                state.AddLayer(new LiquidLayer(domainColor, 1f, c));
            }
            var go = new UnityEngine.GameObject("TestBottle");
            return new FakeBottleView(state) { GameObject = go, Transform = go.transform };
        }

        private static ReactionSystemData CreateTestConfig()
        {
            return new ReactionSystemData
            {
                enableReactions = true,
                reactionRules = new List<ReactionRule>
                {
                    new ReactionRule
                    {
                        colorA = LiquidColor.Red,
                        colorB = LiquidColor.Blue,
                        resultColor = LiquidColor.Purple,
                        reactionType = ReactionRule.ReactionType.Explode
                    }
                }
            };
        }
    }
}
