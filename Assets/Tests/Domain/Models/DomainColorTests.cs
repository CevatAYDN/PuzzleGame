using NUnit.Framework;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Models;
using System.Collections.Generic;

namespace PuzzleGame.Tests.Domain.Models
{
    /// <summary>
    /// Tests for DomainColor equality, hash stability, and Fix #16 epsilon-bucket hashing.
    /// Fix #16: Verifies that DomainColor.GetHashCode() is consistent with Equals() tolerance.
    /// </summary>
    [TestFixture]
    public class DomainColorTests
    {
        // ─── Equality ──────────────────────────────────────────────────────────────

        [Test]
        public void Equals_IdenticalColors_ReturnsTrue()
        {
            var a = new DomainColor(0.5f, 0.3f, 0.8f, 1f);
            var b = new DomainColor(0.5f, 0.3f, 0.8f, 1f);
            Assert.IsTrue(a.Equals(b));
        }

        [Test]
        public void Equals_ColorWithinEpsilon_ReturnsTrue()
        {
            float eps = ForgeConstants.DomainColorHashEpsilon / 2f;
            var a = new DomainColor(0.5f, 0.3f, 0.8f, 1f);
            var b = new DomainColor(0.5f + eps, 0.3f, 0.8f, 1f);
            Assert.IsTrue(a.Equals(b), "Colors within epsilon should be considered equal.");
        }

        [Test]
        public void Equals_ColorOutsideEpsilon_ReturnsFalse()
        {
            float aboveEps = ForgeConstants.DomainColorHashEpsilon * 2f;
            var a = new DomainColor(0.5f, 0.3f, 0.8f, 1f);
            var b = new DomainColor(0.5f + aboveEps, 0.3f, 0.8f, 1f);
            Assert.IsFalse(a.Equals(b), "Colors outside epsilon should not be equal.");
        }

        // ─── Fix #16: GetHashCode consistency with Equals() ────────────────────────

        [Test]
        public void GetHashCode_EqualColors_SameHash()
        {
            var a = new DomainColor(0.5f, 0.3f, 0.8f, 1f);
            var b = new DomainColor(0.5f, 0.3f, 0.8f, 1f);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode(),
                "Fix #16: Equal colors must produce the same hash code.");
        }

        [Test]
        public void GetHashCode_ColorsWithinEpsilon_SameHash()
        {
            // If Equals() returns true, GetHashCode() MUST return the same value (contract).
            float eps = ForgeConstants.DomainColorHashEpsilon / 2f;
            var a = new DomainColor(0.5f, 0.3f, 0.8f, 1f);
            var b = new DomainColor(0.5f + eps, 0.3f, 0.8f, 1f);
            Assert.IsTrue(a.Equals(b), "Precondition: a.Equals(b) must hold for this test.");
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode(),
                "Fix #16: When Equals() is true, GetHashCode() must match.");
        }

        [Test]
        public void GetHashCode_UsableAsHashSetKey_NoDuplicates()
        {
            // Two logically-equal DomainColors must not appear as separate keys in a HashSet.
            var set = new HashSet<DomainColor>();
            var a = new DomainColor(0.5f, 0.5f, 0.5f, 1f);
            var b = new DomainColor(0.5f, 0.5f, 0.5f, 1f);
            set.Add(a);
            set.Add(b);
            Assert.AreEqual(1, set.Count,
                "Fix #16: Logically equal colors must occupy 1 HashSet slot, not 2.");
        }

        [Test]
        public void GetHashCode_UsableAsDictionaryKey_SameBucket()
        {
            var dict = new Dictionary<DomainColor, string>();
            var key1 = new DomainColor(0.9f, 0.1f, 0.4f, 1f);
            var key2 = new DomainColor(0.9f, 0.1f, 0.4f, 1f);
            dict[key1] = "value";
            Assert.IsTrue(dict.TryGetValue(key2, out var v));
            Assert.AreEqual("value", v);
        }
    }
}
