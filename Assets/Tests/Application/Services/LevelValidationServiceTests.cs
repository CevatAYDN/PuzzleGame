using NUnit.Framework;
using PuzzleGame.Application.Services;
using PuzzleGame.Domain.Models;
using PuzzleGame.Logging;

namespace PuzzleGame.Tests.Application.Services
{
    public class LevelValidationServiceTests
    {
        private LevelValidationService _sut;

        [SetUp]
        public void SetUp()
        {
            BottleLogger.SetLevel(BottleLogger.Level.Error, false);
            _sut = new LevelValidationService();
        }

        [Test]
        public void ValidateLevel_NullLevelData_ReturnsFalse()
        {
            Assert.That(_sut.ValidateLevel(null, 10), Is.False);
        }

        [Test]
        public void ValidateLevel_TooManyBottles_ReturnsFalse()
        {
            var levelData = new LevelData
            {
                levelNumber = 1,
                bottleCount = 20,
                emptyBottleCount = 2,
                maxLayersPerBottle = 4,
                autoGenerate = true
            };

            Assert.That(_sut.ValidateLevel(levelData, 5), Is.False);
        }

        [Test]
        public void ValidateLevel_ValidLevel_ReturnsTrue()
        {
            var levelData = new LevelData
            {
                levelNumber = 1,
                bottleCount = 5,
                emptyBottleCount = 2,
                maxLayersPerBottle = 4,
                autoGenerate = true
            };

            Assert.That(_sut.ValidateLevel(levelData, 10), Is.True);
        }

        [Test]
        public void ValidateLevel_DifficultyBoundaries_Work()
        {
            foreach (Difficulty diff in System.Enum.GetValues(typeof(Difficulty)))
            {
                var levelData = new LevelData
                {
                    levelNumber = 1,
                    bottleCount = 5,
                    emptyBottleCount = 2,
                    maxLayersPerBottle = 4,
                    autoGenerate = true,
                    difficulty = diff
                };

                // Should not throw regardless of difficulty
                Assert.DoesNotThrow(() =>
                    _sut.ValidateLevel(levelData, 10));
            }
        }
    }
}
