using NUnit.Framework;
using PuzzleGame.Application.Services;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain;
using PuzzleGame.Application.Logging;
using UnityEngine;

namespace PuzzleGame.Tests.Application.Services
{
    public class LevelValidationServiceTests
    {
        private LevelValidationService _sut;

        [SetUp]
        public void SetUp()
        {
            MoldLogger.SetLevel(MoldLogger.Level.Error, false);
            _sut = new LevelValidationService();
        }

        [Test]
        public void ValidateLevel_NullLevelData_ReturnsFalse()
        {
            Assert.That(_sut.ValidateLevel(null, 10), Is.False);
        }

        [Test]
        public void ValidateLevel_TooManyMolds_ReturnsFalse()
        {
            var levelData = ScriptableObject.CreateInstance<LevelData>();
            levelData.levelNumber = 1;
            levelData.MoldCount = 20;
            levelData.emptyMoldCount = 2;
            levelData.maxLayersPerMold = 4;
            levelData.autoGenerate = true;

            Assert.That(_sut.ValidateLevel(levelData, 5), Is.False);
        }

        [Test]
        public void ValidateLevel_ValidLevel_ReturnsTrue()
        {
            var levelData = ScriptableObject.CreateInstance<LevelData>();
            levelData.levelNumber = 1;
            levelData.MoldCount = 5;
            levelData.emptyMoldCount = 2;
            levelData.maxLayersPerMold = 4;
            levelData.autoGenerate = true;

            Assert.That(_sut.ValidateLevel(levelData, 10), Is.True);
        }

        [Test]
        public void ValidateLevel_DifficultyBoundaries_Work()
        {
            foreach (Difficulty diff in System.Enum.GetValues(typeof(Difficulty)))
            {
                var levelData = ScriptableObject.CreateInstance<LevelData>();
                levelData.levelNumber = 1;
                levelData.MoldCount = 5;
                levelData.emptyMoldCount = 2;
                levelData.maxLayersPerMold = 4;
                levelData.autoGenerate = true;
                levelData.difficulty = diff;

                // Should not throw regardless of difficulty
                Assert.DoesNotThrow(() =>
                    _sut.ValidateLevel(levelData, 10));
            }
        }
    }
}
