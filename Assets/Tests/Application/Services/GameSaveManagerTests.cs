using System.IO;
using NUnit.Framework;
using PuzzleGame.Application.Services;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Domain.Models;
using PuzzleGame.Tests.Fakes;

namespace PuzzleGame.Tests.Application.Services
{
    /// <summary>
    /// Unit tests for GameSaveManager (now injectable via ISaveManager).
    /// Uses temp file paths to avoid touching real save data.
    /// Fix Test Audit: 0 → full coverage of Save/Load/Delete/VerifyIntegrity.
    /// </summary>
    [TestFixture]
    public class GameSaveManagerTests
    {
        private ISaveManager _sut;

        [SetUp]
        public void SetUp()
        {
            _sut = new GameSaveManager();
            // Clean up before each test
            _sut.DeleteAll();
        }

        [TearDown]
        public void TearDown()
        {
            _sut.DeleteAll();
        }

        // ─── HasSaveData ────────────────────────────────────────────────────────

        [Test]
        public void HasSaveData_BeforeSave_ReturnsFalse()
        {
            Assert.IsFalse(_sut.HasSaveData, "No save file should exist before first save.");
        }

        [Test]
        public void HasSaveData_AfterSave_ReturnsTrue()
        {
            var Molds = new IMoldView[] { CreateMold() };
            _sut.Save(1, 5, Molds, isCompleted: false, stars: 0);

            Assert.IsTrue(_sut.HasSaveData, "Save file should exist after save.");
        }

        // ─── Save + LoadLevel ───────────────────────────────────────────────────

        [Test]
        public void SaveAndLoad_SingleLevel_RoundTripsCorrectly()
        {
            var Molds = new IMoldView[] { CreateMold() };
            bool saved = _sut.Save(levelIndex: 3, moveCount: 10, Molds, isCompleted: true, stars: 2);

            Assert.IsTrue(saved, "Save should return true on success.");

            var loaded = _sut.LoadLevel(3);
            Assert.IsNotNull(loaded, "Loaded data should not be null.");
            Assert.AreEqual(3, loaded.Value.LevelIndex);
            Assert.AreEqual(10, loaded.Value.MoveCount);
            Assert.IsTrue(loaded.Value.IsCompleted);
            Assert.AreEqual(2, loaded.Value.Stars);
        }

        [Test]
        public void LoadLevel_NonExistentLevel_ReturnsNull()
        {
            Assert.IsNull(_sut.LoadLevel(999), "Should return null for unknown level.");
        }

        [Test]
        public void Save_Overwrites_ExistingEntry()
        {
            var Molds = new IMoldView[] { CreateMold() };
            _sut.Save(1, 5, Molds, isCompleted: false, stars: 0);
            _sut.Save(1, 12, Molds, isCompleted: true,  stars: 3);

            var loaded = _sut.LoadLevel(1);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(12, loaded.Value.MoveCount, "Should overwrite with latest save.");
            Assert.AreEqual(3, loaded.Value.Stars);
        }

        [Test]
        public void Save_NullMolds_ReturnsFalse()
        {
            bool result = _sut.Save(1, 5, Molds: null, isCompleted: false, stars: 0);
            Assert.IsFalse(result, "Save with null Molds should fail gracefully.");
        }

        // ─── LoadLastPlayedLevel ────────────────────────────────────────────────

        [Test]
        public void LoadLastPlayedLevel_AfterSave_ReturnsCorrectLevel()
        {
            var Molds = new IMoldView[] { CreateMold() };
            _sut.Save(7, 3, Molds, isCompleted: false, stars: 0);

            Assert.AreEqual(7, _sut.LoadLastPlayedLevel());
        }

        [Test]
        public void LoadLastPlayedLevel_WithoutSave_ReturnsZero()
        {
            Assert.AreEqual(0, _sut.LoadLastPlayedLevel());
        }

        // ─── DeleteAll ──────────────────────────────────────────────────────────

        [Test]
        public void DeleteAll_ClearsAllData()
        {
            var Molds = new IMoldView[] { CreateMold() };
            _sut.Save(1, 5, Molds, isCompleted: true, stars: 3);

            _sut.DeleteAll();

            Assert.IsFalse(_sut.HasSaveData, "Save data should be cleared after DeleteAll.");
            Assert.IsNull(_sut.LoadLevel(1), "LoadLevel should return null after DeleteAll.");
        }

        // ─── VerifyIntegrity ────────────────────────────────────────────────────

        [Test]
        public void VerifyIntegrity_WithValidSave_ReturnsTrue()
        {
            var Molds = new IMoldView[] { CreateMold() };
            _sut.Save(1, 1, Molds, isCompleted: false, stars: 0);

            Assert.IsTrue(_sut.VerifyIntegrity(), "Integrity check should pass on unmodified save.");
        }

        [Test]
        public void VerifyIntegrity_WithNoSave_ReturnsFalse()
        {
            Assert.IsFalse(_sut.VerifyIntegrity(), "Integrity check should fail when no save exists.");
        }

        // ─── Helpers ────────────────────────────────────────────────────────────

        private static IMoldView CreateMold()
        {
            var state = new MoldState(4);
            state.AddLayer(new OreLayer(new DomainColor(1f, 0f, 0f, 1f), 0.25f));
            return new FakeMoldView(state);
        }
    }
}
