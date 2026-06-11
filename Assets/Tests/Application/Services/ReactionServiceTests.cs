using System.Collections.Generic;
using NUnit.Framework;
using PuzzleGame.Application.Services;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Configuration.FeatureSystem;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Logging;
using PuzzleGame.Tests.Fakes;
using PuzzleGame.Application.Interfaces;

namespace PuzzleGame.Tests.Application.Services
{
    public class ReactionServiceTests
    {
        private ReactionService _sut;
        private EventAggregator _eventAggregator;
        private IColorAdapter _colorAdapter;

        [SetUp]
        public void SetUp()
        {
            MoldLogger.SetLevel(MoldLogger.Level.Error, false);
            _eventAggregator = new EventAggregator();
            _colorAdapter = new PuzzleGame.Infrastructure.ColorAdapter();
            _sut = new ReactionService(_colorAdapter, _eventAggregator);
        }

        [TearDown]
        public void TearDown()
        {
            _eventAggregator?.Clear();
        }

        [Test]
        public void CheckReactions_NullMolds_ThrowsArgumentNullException()
        {
            var config = CreateTestConfig();
            Assert.Throws<System.ArgumentNullException>(() => _sut.CheckReactions(null, config));
        }

        [Test]
        public void CheckReactions_NullConfig_ReturnsEmpty()
        {
            var Molds = new IMoldView[] { CreateMoldWithLayers(OreColor.Red, OreColor.Blue) };
            var results = _sut.CheckReactions(Molds, null);
            Assert.That(results, Is.EqualTo(0));
        }

        [Test]
        public void CheckReactions_DisabledReactions_ReturnsEmpty()
        {
            var Molds = new IMoldView[] { CreateMoldWithLayers(OreColor.Red, OreColor.Blue) };
            var config = new ReactionSystemData { enableReactions = false };

            var results = _sut.CheckReactions(Molds, config);

            Assert.That(results, Is.EqualTo(0));
        }

        [Test]
        public void CheckReactions_SingleLayer_NoReaction()
        {
            var Molds = new IMoldView[] { CreateMoldWithLayers(OreColor.Red) };
            var config = CreateTestConfig();

            var results = _sut.CheckReactions(Molds, config);

            Assert.That(results, Is.EqualTo(0));
        }

        [Test]
        public void CheckReactions_MatchingColors_TriggersReaction()
        {
            var Molds = new IMoldView[] { CreateMoldWithLayers(OreColor.Red, OreColor.Blue) };
            var config = CreateTestConfig();

            var results = _sut.CheckReactions(Molds, config);

            Assert.That(results, Is.EqualTo(1));
        }

        [Test]
        public void CheckReactions_NoMatchingRule_NoReaction()
        {
            var Molds = new IMoldView[] { CreateMoldWithLayers(OreColor.Green, OreColor.Yellow) };
            var config = CreateTestConfig(); // Rule is Red+Blue only

            var results = _sut.CheckReactions(Molds, config);

            Assert.That(results, Is.EqualTo(0));
        }

        [Test]
        public void CheckReactions_Transform_CombinesLayers()
        {
            var Mold = new MoldState(4);
            Mold.AddLayer(new OreLayer(new DomainColor(0.9f, 0.2f, 0.2f), 1f, OreColor.Red));
            Mold.AddLayer(new OreLayer(new DomainColor(0.5f, 0.2f, 0.9f), 1f, OreColor.Purple));

            var view = new FakeMoldView(Mold);
            view.GameObject = new UnityEngine.GameObject("TestMold");
            view.Transform = view.GameObject.transform;

            var config = new ReactionSystemData
            {
                enableReactions = true,
                reactionRules = new List<ReactionRule>
                {
                    new ReactionRule
                    {
                        colorA = OreColor.Red,
                        colorB = OreColor.Purple,
                        resultColor = OreColor.Pink,
                        reactionType = ReactionRule.ReactionType.Transform
                    }
                }
            };

            var results = _sut.CheckReactions(new IMoldView[] { view }, config);

            Assert.That(results, Is.EqualTo(1));
            // After transform: two layers become one
            Assert.That(Mold.LayerCount, Is.EqualTo(1));
        }

        [Test]
        public void CheckReactions_Explode_ClearsMold()
        {
            var Mold = new MoldState(4);
            Mold.AddLayer(new OreLayer(new DomainColor(0.9f, 0.2f, 0.2f), 1f, OreColor.Red));
            Mold.AddLayer(new OreLayer(new DomainColor(0.2f, 0.2f, 0.9f), 1f, OreColor.Blue));

            var view = new FakeMoldView(Mold);
            view.GameObject = new UnityEngine.GameObject("TestMold");
            view.Transform = view.GameObject.transform;

            var config = CreateTestConfig();

            var results = _sut.CheckReactions(new IMoldView[] { view }, config);

            Assert.That(results, Is.EqualTo(1));
            Assert.That(Mold.IsEmpty, Is.True);
        }

        [Test]
        public void CheckReactions_MultipleMolds_FindsAll()
        {
            var b1 = new MoldState(4);
            b1.AddLayer(new OreLayer(new DomainColor(0.9f, 0.2f, 0.2f), 1f, OreColor.Red));
            b1.AddLayer(new OreLayer(new DomainColor(0.2f, 0.2f, 0.9f), 1f, OreColor.Blue));

            var b2 = new MoldState(4);
            b2.AddLayer(new OreLayer(new DomainColor(0.9f, 0.2f, 0.2f), 1f, OreColor.Red));
            b2.AddLayer(new OreLayer(new DomainColor(0.2f, 0.2f, 0.9f), 1f, OreColor.Blue));

            var v1 = new FakeMoldView(b1) { GameObject = new UnityEngine.GameObject("B1"), Transform = new UnityEngine.GameObject("B1").transform };
            var v2 = new FakeMoldView(b2) { GameObject = new UnityEngine.GameObject("B2"), Transform = new UnityEngine.GameObject("B2").transform };

            var config = CreateTestConfig();

            var results = _sut.CheckReactions(new IMoldView[] { v1, v2 }, config);

            Assert.That(results, Is.EqualTo(2));
        }

        // ── Chain Reaction + Undo QA (TODO Step 4) ────────────────────────

        [Test]
        public void CheckReactions_ChainReaction_ExplosionTriggersSubsequentReactions()
        {
            var b1 = new MoldState(4);
            b1.AddLayer(new OreLayer(new DomainColor(0.9f, 0.2f, 0.2f), 1f, OreColor.Red));
            b1.AddLayer(new OreLayer(new DomainColor(0.2f, 0.2f, 0.9f), 1f, OreColor.Blue));

            var b2 = new MoldState(4);
            b2.AddLayer(new OreLayer(new DomainColor(0.9f, 0.2f, 0.2f), 1f, OreColor.Red));
            b2.AddLayer(new OreLayer(new DomainColor(0.2f, 0.2f, 0.9f), 1f, OreColor.Blue));

            var v1 = new FakeMoldView(b1) { GameObject = new UnityEngine.GameObject("B1"), Transform = new UnityEngine.GameObject("B1").transform };
            var v2 = new FakeMoldView(b2) { GameObject = new UnityEngine.GameObject("B2"), Transform = new UnityEngine.GameObject("B2").transform };

            var config = CreateTestConfig();
            var results = _sut.CheckReactions(new IMoldView[] { v1, v2 }, config);

            Assert.That(results, Is.EqualTo(2));
            Assert.That(b1.IsEmpty, Is.True);
            Assert.That(b2.IsEmpty, Is.True);
        }

        [Test]
        public void CheckReactions_AfterExplosion_StateIsStableForUndo()
        {
            var mold = new MoldState(4);
            mold.AddLayer(new OreLayer(new DomainColor(0.9f, 0.2f, 0.2f), 1f, OreColor.Red));
            mold.AddLayer(new OreLayer(new DomainColor(0.2f, 0.2f, 0.9f), 1f, OreColor.Blue));

            var view = new FakeMoldView(mold) { GameObject = new UnityEngine.GameObject("TestMold"), Transform = new UnityEngine.GameObject("TestMold").transform };

            var config = CreateTestConfig();
            int result = _sut.CheckReactions(new IMoldView[] { view }, config);

            Assert.That(result, Is.EqualTo(1));
            Assert.That(mold.IsEmpty, Is.True);
            Assert.That(mold.LayerCount, Is.EqualTo(0));
        }

        [Test]
        public void CheckReactions_NoReactionWhenMoldAlreadyEmpty()
        {
            var mold = new MoldState(4);
            var view = new FakeMoldView(mold) { GameObject = new UnityEngine.GameObject("Empty"), Transform = new UnityEngine.GameObject("Empty").transform };

            var config = CreateTestConfig();
            int result = _sut.CheckReactions(new IMoldView[] { view }, config);

            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void CheckReactions_SingleMoldWithOneLayer_NoReaction()
        {
            var mold = new MoldState(4);
            mold.AddLayer(new OreLayer(new DomainColor(0.9f, 0.2f, 0.2f), 1f, OreColor.Red));
            var view = new FakeMoldView(mold) { GameObject = new UnityEngine.GameObject("Single"), Transform = new UnityEngine.GameObject("Single").transform };

            var config = CreateTestConfig();
            int result = _sut.CheckReactions(new IMoldView[] { view }, config);

            Assert.That(result, Is.EqualTo(0));
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static FakeMoldView CreateMoldWithLayers(params OreColor[] colors)
        {
            var state = new MoldState(4);
            foreach (var c in colors)
            {
                var domainColor = c.ToDefaultDomainColor();
                var unityColor = PuzzleGame.Infrastructure.ColorAdapter.ToUnityStatic(domainColor);
                state.AddLayer(new OreLayer(domainColor, 1f, c));
            }
            var go = new UnityEngine.GameObject("TestMold");
            return new FakeMoldView(state) { GameObject = go, Transform = go.transform };
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
                        colorA = OreColor.Red,
                        colorB = OreColor.Blue,
                        resultColor = OreColor.Purple,
                        reactionType = ReactionRule.ReactionType.Explode
                    }
                }
            };
        }
    }
}
