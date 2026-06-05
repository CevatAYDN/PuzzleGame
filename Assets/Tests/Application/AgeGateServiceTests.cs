using System;
using NUnit.Framework;
using PuzzleGame.Application.Services;
using UnityEngine;

namespace PuzzleGame.Tests.Application
{
    [TestFixture]
    public class AgeGateServiceTests
    {
        private const string YearKey = "agegate_birth_year";
        private const string MonthKey = "agegate_birth_month";

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(YearKey);
            PlayerPrefs.DeleteKey(MonthKey);
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(YearKey);
            PlayerPrefs.DeleteKey(MonthKey);
        }

        [Test]
        public void NewInstance_IsNotVerified()
        {
            var service = new AgeGateService();
            Assert.IsFalse(service.IsVerified);
            Assert.IsFalse(service.IsUnder13);
            Assert.IsNull(service.BirthDate);
        }

        [Test]
        public void Verify_StoresYearAndMonthOnly()
        {
            var service = new AgeGateService();
            var birth = new DateTime(1990, 6, 15);

            service.Verify(birth);

            Assert.IsTrue(service.IsVerified);
            Assert.AreEqual(1990, PlayerPrefs.GetInt(YearKey));
            Assert.AreEqual(6, PlayerPrefs.GetInt(MonthKey));
            Assert.AreEqual(new DateTime(1990, 6, 1), service.BirthDate);
        }

        [Test]
        public void IsUnder13_TrueForChildBornRecently()
        {
            var service = new AgeGateService();
            var tenYearsAgo = DateTime.UtcNow.AddYears(-10);

            service.Verify(tenYearsAgo);

            Assert.IsTrue(service.IsUnder13);
        }

        [Test]
        public void IsUnder13_FalseForAdult()
        {
            var service = new AgeGateService();
            var twentyYearsAgo = DateTime.UtcNow.AddYears(-20);

            service.Verify(twentyYearsAgo);

            Assert.IsFalse(service.IsUnder13);
        }

        [Test]
        public void ReVerify_OverwritesPreviousDate()
        {
            var service = new AgeGateService();
            service.Verify(new DateTime(2010, 1, 1));
            Assert.IsTrue(service.IsUnder13);

            service.ReVerify(new DateTime(1990, 1, 1));
            Assert.IsFalse(service.IsUnder13);
            Assert.AreEqual(1990, service.BirthDate.Value.Year);
        }

        [Test]
        public void Clear_RemovesStoredData()
        {
            var service = new AgeGateService();
            service.Verify(new DateTime(1990, 1, 1));
            Assert.IsTrue(service.IsVerified);

            service.Clear();
            Assert.IsFalse(service.IsVerified);
            Assert.IsNull(service.BirthDate);
        }

        [Test]
        public void BirthDate_NullWhenCorrupted()
        {
            PlayerPrefs.SetInt(YearKey, 1500);
            PlayerPrefs.SetInt(MonthKey, 13);

            var service = new AgeGateService();

            Assert.IsNull(service.BirthDate);
            Assert.IsFalse(service.IsVerified);
        }
    }
}
