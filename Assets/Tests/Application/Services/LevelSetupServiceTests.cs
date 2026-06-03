using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Services;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain;
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
            BottleLogger.SetLevel(BottleLogger.Level.Error, false);

            _gameConfig = ScriptableObject.CreateInstance<GameConfig>();
            _gameConfig.maxLayersPerBottle = 4;

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
        public void GenerateLevelAssignments_NullBottles_ReturnsEmpty()
        {
            var levelData = CreateLevelData();
            var result = _sut.GenerateLevelAssignments(null, levelData);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void GenerateLevelAssignments_EmptyBottles_ReturnsEmpty()
        {
            var levelData = CreateLevelData();
            var result = _sut.GenerateLevelAssignments(Array.Empty<IBottleView>(), levelData);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void GenerateLevelAssignments_NullLevelData_ThrowsArgumentNullException()
        {
            var bottles = new IBottleView[] { CreateBottleView() };

            Assert.Throws<ArgumentNullException>(() =>
                _sut.GenerateLevelAssignments(bottles, null));
        }

        // ── Auto-generate ─────────────────────────────────────────────────────

        [Test]
        public void GenerateLevelAssignments_AutoGenerate_DelegatesToGenerator()
        {
            var levelData = CreateLevelData(autoGenerate: true, bottleCount: 5, emptyCount: 2);
            var bottles = CreateBottleViews(5);

            _levelGenerator.SetSimpleAssignment(5, 2);

            var result = _sut.GenerateLevelAssignments(bottles, levelData);

            Assert.That(_levelGenerator.GenerateCallCount, Is.EqualTo(1));
            Assert.That(_levelGenerator.LastBottleCount, Is.EqualTo(5));
            Assert.That(_levelGenerator.LastMaxLayers, Is.EqualTo(4));
            Assert.That(_levelGenerator.LastEmptyBottleCount, Is.EqualTo(2));
            Assert.That(result.Count, Is.EqualTo(5));
        }

        // ── Pre-built level ───────────────────────────────────────────────────

        [Test]
        public void GenerateLevelAssignments_PreBuiltLevel_ConvertsCorrectly()
        {
            var levelData = CreateLevelData(autoGenerate: false, bottleCount: 2);
            levelData.bottles = new List<LevelBottleData>
            {
                new LevelBottleData
                {
                    isEmpty = false,
                    layers = new List<LevelLayerData>
                    {
                        new LevelLayerData { color = Color.red, amount = 1f }
                    }
                },
                new LevelBottleData
                {
                    isEmpty = true,
                    layers = new List<LevelLayerData>()
                }
            };

            var bottles = CreateBottleViews(2);
            var result = _sut.GenerateLevelAssignments(bottles, levelData);

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].Count, Is.EqualTo(1)); // One layer
            Assert.That(result[1].Count, Is.EqualTo(0)); // Empty
        }

        // ── SetupBottles integration ──────────────────────────────────────────

        [Test]
        public void SetupBottles_CallsInitializeOnEachBottle()
        {
            var levelData = CreateLevelData(autoGenerate: true, bottleCount: 3, emptyCount: 1);
            var bottles = CreateBottleViews(3);
            _levelGenerator.SetSimpleAssignment(3, 1);

            _sut.SetupBottles(bottles, levelData, null, null, null);

            foreach (var bottle in bottles)
            {
                // FakeBottleView.Initialize was called
                Assert.That(((FakeBottleView)bottle).InitializeCallCount, Is.EqualTo(1));
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static FakeBottleView CreateBottleView()
        {
            var state = new BottleState(4);
            var go = new GameObject("TestBottle");
            return new FakeBottleView(state) { GameObject = go, Transform = go.transform };
        }

        private static IBottleView[] CreateBottleViews(int count)
        {
            var views = new IBottleView[count];
            for (int i = 0; i < count; i++)
                views[i] = CreateBottleView();
            return views;
        }

        private static LevelData CreateLevelData(bool autoGenerate = true,
            int bottleCount = 5, int emptyCount = 2, int maxLayers = 4)
        {
            return new LevelData
            {
                levelNumber = 1,
                autoGenerate = autoGenerate,
                bottleCount = bottleCount,
                emptyBottleCount = emptyCount,
                maxLayersPerBottle = maxLayers,
                randomSeed = 42,
                difficulty = Difficulty.Easy
            };
        }
    }
}
