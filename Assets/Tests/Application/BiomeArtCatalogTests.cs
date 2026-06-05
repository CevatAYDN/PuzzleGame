using System.Reflection;
using NUnit.Framework;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Domain.Models;
using UnityEngine;

namespace PuzzleGame.Tests.Application
{
    [TestFixture]
    public class BiomeArtCatalogTests
    {
        private static BiomeArtCatalog CreateCatalogWithEntries(params BiomeArtCatalog.BiomeArtEntry[] entries)
        {
            var catalog = ScriptableObject.CreateInstance<BiomeArtCatalog>();
            var field = typeof(BiomeArtCatalog).GetField("entries", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(catalog, entries);
            return catalog;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_createdCatalog);
        }

        private BiomeArtCatalog _createdCatalog;

        [Test]
        public void GetEntry_NullEntries_ReturnsNull()
        {
            _createdCatalog = CreateCatalogWithEntries(null);
            Assert.IsNull(_createdCatalog.GetEntry(Biome.CrystalMines));
        }

        [Test]
        public void GetEntry_EmptyEntries_ReturnsNull()
        {
            _createdCatalog = CreateCatalogWithEntries(new BiomeArtCatalog.BiomeArtEntry[0]);
            Assert.IsNull(_createdCatalog.GetEntry(Biome.CrystalMines));
        }

        [Test]
        public void GetEntry_SingleEntry_ReturnsMatch()
        {
            var entry = new BiomeArtCatalog.BiomeArtEntry { biome = Biome.CrystalMines };
            _createdCatalog = CreateCatalogWithEntries(entry);
            Assert.AreSame(entry, _createdCatalog.GetEntry(Biome.CrystalMines));
        }

        [Test]
        public void GetEntry_MultipleEntries_ReturnsCorrectOne()
        {
            var crystalEntry = new BiomeArtCatalog.BiomeArtEntry { biome = Biome.CrystalMines };
            var forgeEntry = new BiomeArtCatalog.BiomeArtEntry { biome = Biome.VolcanicForge };
            _createdCatalog = CreateCatalogWithEntries(crystalEntry, forgeEntry);
            Assert.AreSame(crystalEntry, _createdCatalog.GetEntry(Biome.CrystalMines));
            Assert.AreSame(forgeEntry, _createdCatalog.GetEntry(Biome.VolcanicForge));
        }

        [Test]
        public void GetEntry_NullEntryInArray_SkipsIt()
        {
            var forgeEntry = new BiomeArtCatalog.BiomeArtEntry { biome = Biome.VolcanicForge };
            _createdCatalog = CreateCatalogWithEntries(null, forgeEntry, null);
            Assert.AreSame(forgeEntry, _createdCatalog.GetEntry(Biome.VolcanicForge));
        }

        [Test]
        public void GetEntry_NoMatch_ReturnsNull()
        {
            var entry = new BiomeArtCatalog.BiomeArtEntry { biome = Biome.CrystalMines };
            _createdCatalog = CreateCatalogWithEntries(entry);
            Assert.IsNull(_createdCatalog.GetEntry(Biome.VolcanicForge));
        }

        [Test]
        public void GetEntry_AccentColor_PreservesValue()
        {
            var entry = new BiomeArtCatalog.BiomeArtEntry
            {
                biome = Biome.CrystalMines,
                accentColor = new Color(0.3f, 0.6f, 0.9f, 1f)
            };
            _createdCatalog = CreateCatalogWithEntries(entry);
            var actual = _createdCatalog.GetEntry(Biome.CrystalMines);
            Assert.AreEqual(0.3f, actual.accentColor.r, 0.001f);
            Assert.AreEqual(0.6f, actual.accentColor.g, 0.001f);
            Assert.AreEqual(0.9f, actual.accentColor.b, 0.001f);
        }

        [Test]
        public void GetEntry_AccentColor_DefaultIsWhite()
        {
            var entry = new BiomeArtCatalog.BiomeArtEntry { biome = Biome.CrystalMines };
            Assert.AreEqual(Color.white, entry.accentColor);
        }
    }
}
