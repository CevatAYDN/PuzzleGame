using NUnit.Framework;
using PuzzleGame.Application.Services;
using PuzzleGame.Domain.Models;
using PuzzleGame.Infrastructure;
using UnityEngine;

namespace PuzzleGame.Application.Tests
{
    public class BottleSelectionServiceTests
    {
        private BottleSelectionService _service;
        private BottleState _bottleA;
        private BottleState _bottleB;

        [SetUp]
        public void Setup()
        {
            _service = new BottleSelectionService();
            _bottleA = new BottleState(4);
            _bottleA.AddLayer(new LiquidLayer(ColorAdapter.FromUnityStatic(Color.red), 0.25f));
            _bottleB = new BottleState(4);
            _bottleB.AddLayer(new LiquidLayer(ColorAdapter.FromUnityStatic(Color.blue), 0.25f));
        }

        [Test]
        public void Initially_NothingSelected()
        {
            Assert.That(_service.SelectedBottle, Is.Null);
        }

        // ── Select ──────────────────────────────────────────────────────────

        [Test]
        public void Select_SetsSelectedBottle()
        {
            _service.Select(_bottleA);

            Assert.That(_service.SelectedBottle, Is.EqualTo(_bottleA));
        }

        [Test]
        public void Select_Null_DoesNothing()
        {
            _service.Select(null);

            Assert.That(_service.SelectedBottle, Is.Null);
        }

        [Test]
        public void Select_SameBottle_Deselects()
        {
            _service.Select(_bottleA);
            _service.Select(_bottleA);

            Assert.That(_service.SelectedBottle, Is.Null);
        }

        [Test]
        public void Select_DifferentBottle_ReplacesSelection()
        {
            _service.Select(_bottleA);
            _service.Select(_bottleB);

            Assert.That(_service.SelectedBottle, Is.EqualTo(_bottleB));
        }

        // ── Deselect ────────────────────────────────────────────────────────

        [Test]
        public void Deselect_ClearsSelection()
        {
            _service.Select(_bottleA);
            _service.Deselect();

            Assert.That(_service.SelectedBottle, Is.Null);
        }

        [Test]
        public void Deselect_WhenNothingSelected_DoesNothing()
        {
            _service.Deselect();

            Assert.That(_service.SelectedBottle, Is.Null);
        }

        // ── Events ──────────────────────────────────────────────────────────

        [Test]
        public void Select_FiresOnBottleSelected()
        {
            BottleState selected = null;
            _service.OnBottleSelected += b => selected = b;

            _service.Select(_bottleA);

            Assert.That(selected, Is.EqualTo(_bottleA));
        }

        [Test]
        public void Select_SameBottle_FiresDeselectNotSelect()
        {
            bool selected = false;
            bool deselected = false;
            _service.OnBottleSelected += b => selected = true;
            _service.OnBottleDeselected += b => deselected = true;

            _service.Select(_bottleA);
            selected = false;
            deselected = false;

            _service.Select(_bottleA);

            Assert.That(selected, Is.False, "OnBottleSelected should not fire");
            Assert.That(deselected, Is.True, "OnBottleDeselected should fire");
        }

        [Test]
        public void Select_DifferentBottle_FiresDeselectThenSelect()
        {
            BottleState deselectedBottle = null;
            BottleState selectedBottle = null;
            _service.OnBottleSelected += b => selectedBottle = b;
            _service.OnBottleDeselected += b => deselectedBottle = b;

            _service.Select(_bottleA);

            _service.Select(_bottleB);

            Assert.That(deselectedBottle, Is.EqualTo(_bottleA));
            Assert.That(selectedBottle, Is.EqualTo(_bottleB));
        }

        [Test]
        public void Deselect_FiresOnBottleDeselected()
        {
            BottleState deselected = null;
            _service.OnBottleDeselected += b => deselected = b;

            _service.Select(_bottleA);
            _service.Deselect();

            Assert.That(deselected, Is.EqualTo(_bottleA));
        }

        [Test]
        public void Deselect_WhenNothingSelected_FiresNothing()
        {
            bool fired = false;
            _service.OnBottleDeselected += b => fired = true;

            _service.Deselect();

            Assert.That(fired, Is.False);
        }

        [Test]
        public void Select_Null_FiresNoEvents()
        {
            bool selected = false;
            bool deselected = false;
            _service.OnBottleSelected += b => selected = true;
            _service.OnBottleDeselected += b => deselected = true;

            _service.Select(null);

            Assert.That(selected, Is.False);
            Assert.That(deselected, Is.False);
        }

        [Test]
        public void MultipleSelect_CorrectlyTracksEvents()
        {
            int selectCount = 0;
            int deselectCount = 0;
            _service.OnBottleSelected += b => selectCount++;
            _service.OnBottleDeselected += b => deselectCount++;

            _service.Select(_bottleA);
            _service.Select(_bottleB);
            _service.Deselect();
            _service.Select(_bottleA);

            Assert.That(selectCount, Is.EqualTo(3));
            Assert.That(deselectCount, Is.EqualTo(2));
        }
    }
}