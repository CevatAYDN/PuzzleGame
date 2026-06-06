using System;
using NUnit.Framework;
using PuzzleGame.Application.Services;
using UnityEngine;

namespace PuzzleGame.Tests.Application.Services
{
    public class AgeGateServiceTests
    {
        private const string YearKey = "agegate_birth_year";
        private const string MonthKey = "agegate_birth_month";

        private AgeGateService _sut;

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(YearKey);
            PlayerPrefs.DeleteKey(MonthKey);
            _sut = new AgeGateService();
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(YearKey);
            PlayerPrefs.DeleteKey(MonthKey);
        }

        // ── Initial state ────────────────────────────────────────────────

        [Test]
        public void IsVerified_InitialState_IsFalse()
        {
            Assert.That(_sut.IsVerified, Is.False);
        }

        [Test]
        public void BirthDate_InitialState_IsNull()
        {
            Assert.That(_sut.BirthDate, Is.Null);
        }

        [Test]
        public void IsUnder13_InitialState_IsFalse()
        {
            Assert.That(_sut.IsUnder13, Is.False);
        }

        // ── Verify ───────────────────────────────────────────────────────

        [Test]
        public void Verify_SetsIsVerified()
        {
            _sut.Verify(new DateTime(2010, 6, 1));

            Assert.That(_sut.IsVerified, Is.True);
        }

        [Test]
        public void Verify_SetsBirthDate()
        {
            _sut.Verify(new DateTime(2010, 6, 1));

            Assert.That(_sut.BirthDate, Is.EqualTo(new DateTime(2010, 6, 1)));
        }

        [Test]
        public void Verify_OnlyStoresYearAndMonth()
        {
            _sut.Verify(new DateTime(2010, 6, 15));

            // Service forces day=1 when reconstructing the date.
            Assert.That(_sut.BirthDate, Is.EqualTo(new DateTime(2010, 6, 1)));
        }

        [Test]
        public void Verify_PersistsAcrossInstances()
        {
            _sut.Verify(new DateTime(2010, 6, 1));

            var fresh = new AgeGateService();
            Assert.That(fresh.IsVerified, Is.True);
            Assert.That(fresh.BirthDate, Is.EqualTo(new DateTime(2010, 6, 1)));
        }

        // ── IsUnder13 calculation ────────────────────────────────────────

        [Test]
        public void IsUnder13_Child_ReturnsTrue()
        {
            // 10 years old this year, birth month already passed.
            int childYear = DateTime.UtcNow.Year - 10;
            _sut.Verify(new DateTime(childYear, 1, 1));

            Assert.That(_sut.IsUnder13, Is.True);
        }

        [Test]
        public void IsUnder13_Adult_ReturnsFalse()
        {
            int adultYear = DateTime.UtcNow.Year - 30;
            _sut.Verify(new DateTime(adultYear, 1, 1));

            Assert.That(_sut.IsUnder13, Is.False);
        }

        [Test]
        public void IsUnder13_Exactly13YearsOld_ReturnsFalse()
        {
            // Born exactly 13 years ago, in the same month (or earlier) — should be NOT under 13.
            int year = DateTime.UtcNow.Year - 13;
            int month = System.Math.Max(1, DateTime.UtcNow.Month - 1);
            _sut.Verify(new DateTime(year, month, 1));

            Assert.That(_sut.IsUnder13, Is.False);
        }

        [Test]
        public void IsUnder13_12YearsOld_ReturnsTrue()
        {
            int year = DateTime.UtcNow.Year - 12;
            int month = System.Math.Max(1, DateTime.UtcNow.Month - 1);
            _sut.Verify(new DateTime(year, month, 1));

            Assert.That(_sut.IsUnder13, Is.True);
        }

        [Test]
        public void IsUnder13_BirthdayLaterThisYear_ReturnsTrue()
        {
            // Born 12 years ago, birthday later in the year → still under 13.
            int year = DateTime.UtcNow.Year - 12;
            int birthMonth = System.Math.Min(12, DateTime.UtcNow.Month + 1);
            _sut.Verify(new DateTime(year, birthMonth, 1));

            Assert.That(_sut.IsUnder13, Is.True);
        }

        [Test]
        public void IsUnder13_BirthdayEarlierThisYear_ReturnsFalseAt13()
        {
            // Born 13 years ago, birthday earlier in the year → not under 13.
            int year = DateTime.UtcNow.Year - 13;
            int birthMonth = System.Math.Max(1, DateTime.UtcNow.Month - 1);
            _sut.Verify(new DateTime(year, birthMonth, 1));

            Assert.That(_sut.IsUnder13, Is.False);
        }

        // ── ReVerify ─────────────────────────────────────────────────────

        [Test]
        public void ReVerify_OverwritesPreviousValue()
        {
            _sut.Verify(new DateTime(2010, 6, 1));
            _sut.ReVerify(new DateTime(2005, 1, 1));

            Assert.That(_sut.BirthDate, Is.EqualTo(new DateTime(2005, 1, 1)));
        }

        // ── Clear ────────────────────────────────────────────────────────

        [Test]
        public void Clear_RemovesAllKeys()
        {
            _sut.Verify(new DateTime(2010, 6, 1));
            _sut.Clear();

            Assert.That(PlayerPrefs.HasKey(YearKey), Is.False);
            Assert.That(PlayerPrefs.HasKey(MonthKey), Is.False);
        }

        [Test]
        public void Clear_ResetsIsVerified()
        {
            _sut.Verify(new DateTime(2010, 6, 1));
            _sut.Clear();

            Assert.That(_sut.IsVerified, Is.False);
            Assert.That(_sut.BirthDate, Is.Null);
            Assert.That(_sut.IsUnder13, Is.False);
        }

        // ── Stored value validation ──────────────────────────────────────

        [Test]
        public void BirthDate_StoredYearBelow1900_ReturnsNull()
        {
            PlayerPrefs.SetInt(YearKey, 1850);
            PlayerPrefs.SetInt(MonthKey, 6);
            PlayerPrefs.Save();

            var fresh = new AgeGateService();
            Assert.That(fresh.BirthDate, Is.Null);
            Assert.That(fresh.IsVerified, Is.False);
        }

        [Test]
        public void BirthDate_StoredYearInFuture_ReturnsNull()
        {
            PlayerPrefs.SetInt(YearKey, DateTime.UtcNow.Year + 1);
            PlayerPrefs.SetInt(MonthKey, 6);
            PlayerPrefs.Save();

            var fresh = new AgeGateService();
            Assert.That(fresh.BirthDate, Is.Null);
            Assert.That(fresh.IsVerified, Is.False);
        }

        [Test]
        public void BirthDate_StoredMonthBelow1_ReturnsNull()
        {
            PlayerPrefs.SetInt(YearKey, 2010);
            PlayerPrefs.SetInt(MonthKey, 0);
            PlayerPrefs.Save();

            var fresh = new AgeGateService();
            Assert.That(fresh.BirthDate, Is.Null);
            Assert.That(fresh.IsVerified, Is.False);
        }

        [Test]
        public void BirthDate_StoredMonthAbove12_ReturnsNull()
        {
            PlayerPrefs.SetInt(YearKey, 2010);
            PlayerPrefs.SetInt(MonthKey, 13);
            PlayerPrefs.Save();

            var fresh = new AgeGateService();
            Assert.That(fresh.BirthDate, Is.Null);
            Assert.That(fresh.IsVerified, Is.False);
        }
    }
}
