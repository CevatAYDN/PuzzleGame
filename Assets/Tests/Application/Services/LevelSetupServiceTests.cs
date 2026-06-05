using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Services;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain;
using PuzzleGame.Infrastructure;
using PuzzleGame.Application.Logging;
using PuzzleGame.Tests.Fakes;

namespace PuzzleGame.Tests.Application.Services
{
    public class LevelSetupServiceTests
    {
        private LevelSetupService _sut;
        private GameConfig _gameConfig;
        private LevelConfig _levelConfig;
        private FakeLevelGenerator _levelGenerator;

        [SetUp]
        public void SetUp()
        {
            MoldLogger.SetLevel(MoldLogger.Level.Error, false);

            _gameConfig = ScriptableObject.CreateInstance<GameConfig>();
            _gameConfig.maxLayersPerMold = 4;

            _levelConfig = ScriptableObject.CreateInstance<LevelConfig>();
            _levelConfig.palette = new Color[]
            {
                new Color(1f, 0.2f, 0.2f),
                new Color(0.2f, 0.6f, 0.9f),
            };

            _levelGenerator = new FakeLevelGenerator();
            _sut = new LevelSetupService(_gameConfig, _levelConfig, _levelGenerator);
        }

        [TearDown]
        public void TearDown()
        {
            if (_gameConfig != null) ScriptableObject.DestroyImmediate(_gameConfig);
            if (_levelConfig != null) ScriptableObject.DestroyImmediate(_levelConfig);
        }

        // ── Null / Edge cases ─────────────────────────────────────────────────

        [Test]
        public void GenerateLevelAssignments_NullMolds_ReturnsEmpty()
        {
            var levelData = CreateLevelData();
            var result = _sut.GenerateLevelAssignments(null, levelData);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void GenerateLevelAssignments_EmptyMolds_ReturnsEmpty()
        {
            var levelData = CreateLevelData();
            var result = _sut.GenerateLevelAssignments(Array.Empty<IMoldView>(), levelData);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void GenerateLevelAssignments_NullLevelData_ThrowsArgumentNullException()
        {
            var Molds = new IMoldView[] { CreateMoldView() };

            Assert.Throws<ArgumentNullException>(() =>
                _sut.GenerateLevelAssignments(Molds, null));
        }

        // ── Auto-generate ─────────────────────────────────────────────────────

        [Test]
        public void GenerateLevelAssignments_AutoGenerate_DelegatesToGenerator()
        {
            var levelData = CreateLevelData(autoGenerate: true, MoldCount: 5, emptyCount: 2);
            var Molds = CreateMoldViews(5);

            _levelGenerator.SetSimpleAssignment(5, 2);

            var result = _sut.GenerateLevelAssignments(Molds, levelData);

            Assert.That(_levelGenerator.GenerateCallCount, Is.EqualTo(1));
            Assert.That(_levelGenerator.LastMoldCount, Is.EqualTo(5));
            Assert.That(_levelGenerator.LastMaxLayers, Is.EqualTo(4));
            Assert.That(_levelGenerator.LastEmptyMoldCount, Is.EqualTo(2));
            Assert.That(result.Count, Is.EqualTo(5));
        }

        // ── Pre-built level ───────────────────────────────────────────────────

        [Test]
        public void GenerateLevelAssignments_PreBuiltLevel_ConvertsCorrectly()
        {
            var levelData = CreateLevelData(autoGenerate: false, MoldCount: 2);
            levelData.Molds = new List<LevelMoldData>
            {
                new LevelMoldData
                {
                    isEmpty = false,
                    layers = new List<LevelLayerData>
                    {
                        new LevelLayerData { color = Color.red, amount = 1f }
                    }
                },
                new LevelMoldData
                {
                    isEmpty = true,
                    layers = new List<LevelLayerData>()
                }
            };

            var Molds = CreateMoldViews(2);
            var result = _sut.GenerateLevelAssignments(Molds, levelData);

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].Count, Is.EqualTo(1)); // One layer
            Assert.That(result[1].Count, Is.EqualTo(0)); // Empty
        }

        // ── SetupMolds integration ──────────────────────────────────────────

        [Test]
        public void SetupMolds_CallsInitializeOnEachMold()
        {
            var levelData = CreateLevelData(autoGenerate: true, MoldCount: 3, emptyCount: 1);
            var Molds = CreateMoldViews(3);
            _levelGenerator.SetSimpleAssignment(3, 1);

            _sut.SetupMolds(Molds, levelData, null, null, null);

            foreach (var Mold in Molds)
            {
                // FakeMoldView.Initialize was called
                Assert.That(((FakeMoldView)Mold).InitializeCallCount, Is.EqualTo(1));
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static FakeMoldView CreateMoldView()
        {
            var state = new MoldState(4);
            var go = new GameObject("TestMold");
            return new FakeMoldView(state) { GameObject = go, Transform = go.transform };
        }

        private static IMoldView[] CreateMoldViews(int count)
        {
            var views = new IMoldView[count];
            for (int i = 0; i < count; i++)
                views[i] = CreateMoldView();
            return views;
        }

        private static LevelData CreateLevelData(bool autoGenerate = true,
            int MoldCount = 5, int emptyCount = 2, int maxLayers = 4)
        {
            var data = ScriptableObject.CreateInstance<LevelData>();
            data.levelNumber = 1;
            data.autoGenerate = autoGenerate;
            data.MoldCount = MoldCount;
            data.emptyMoldCount = emptyCount;
            data.maxLayersPerMold = maxLayers;
            data.randomSeed = 42;
            data.difficulty = Difficulty.Easy;
            return data;
        }
    }
}
