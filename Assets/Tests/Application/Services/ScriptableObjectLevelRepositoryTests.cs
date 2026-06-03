using NUnit.Framework;
using PuzzleGame.Application.Services;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Domain.Models;
using UnityEngine;

namespace PuzzleGame.Tests.Application.Services
{
    public class ScriptableObjectLevelRepositoryTests
    {
        private LevelData _lvl1, _lvl3, _lvl2;
        private ScriptableObjectLevelRepository _sut;

        [SetUp]
        public void Setup()
        {
            _lvl1 = ScriptableObject.CreateInstance<LevelData>(); _lvl1.levelNumber = 1;
            _lvl2 = ScriptableObject.CreateInstance<LevelData>(); _lvl2.levelNumber = 2;
            _lvl3 = ScriptableObject.CreateInstance<LevelData>(); _lvl3.levelNumber = 3;

            _sut = new ScriptableObjectLevelRepository(new[] { _lvl3, _lvl1, _lvl2 });
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(_lvl1);
            Object.DestroyImmediate(_lvl2);
            Object.DestroyImmediate(_lvl3);
        }

        [Test]
        public void AllLevels_SortedByNumber()
        {
            var all = _sut.AllLevels;
            Assert.That(all.Count, Is.EqualTo(3));
            Assert.That(all[0].levelNumber, Is.EqualTo(1));
            Assert.That(all[1].levelNumber, Is.EqualTo(2));
            Assert.That(all[2].levelNumber, Is.EqualTo(3));
        }

        [Test]
        public void GetByNumber_Existing_Returns()
        {
            Assert.That(_sut.GetByNumber(2), Is.SameAs(_lvl2));
        }

        [Test]
        public void GetByNumber_Missing_ReturnsNull()
        {
            Assert.That(_sut.GetByNumber(99), Is.Null);
        }

        [Test]
        public void TotalCount_ReportsCorrectly()
        {
            Assert.That(_sut.TotalCount, Is.EqualTo(3));
        }

        [Test]
        public void EmptyConstructor_EmptyRepository()
        {
            var empty = new ScriptableObjectLevelRepository(new LevelData[0]);
            Assert.That(empty.TotalCount, Is.EqualTo(0));
            Assert.That(empty.AllLevels, Is.Empty);
        }

        [Test]
        public void NullConstructor_EmptyRepository()
        {
            var empty = new ScriptableObjectLevelRepository(null);
            Assert.That(empty.TotalCount, Is.EqualTo(0));
        }

        [Test]
        public void Filtered_NullEntries_AreSkipped()
        {
            var levels = new[] { _lvl1, null, _lvl2 };
            var repo = new ScriptableObjectLevelRepository(levels);
            Assert.That(repo.TotalCount, Is.EqualTo(2));
        }
    }
}
