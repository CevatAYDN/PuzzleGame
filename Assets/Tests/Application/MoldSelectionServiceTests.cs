using NUnit.Framework;
using PuzzleGame.Application.Services;
using PuzzleGame.Domain.Models;
using PuzzleGame.Infrastructure;
using UnityEngine;

namespace PuzzleGame.Application.Tests
{
    public class MoldSelectionServiceTests
    {
        private MoldSelectionService _service;
        private MoldState _MoldA;
        private MoldState _MoldB;

        [SetUp]
        public void Setup()
        {
            _service = new MoldSelectionService();
            _MoldA = new MoldState(4);
            _MoldA.AddLayer(new OreLayer(ColorAdapter.FromUnityStatic(Color.red), 0.25f));
            _MoldB = new MoldState(4);
            _MoldB.AddLayer(new OreLayer(ColorAdapter.FromUnityStatic(Color.blue), 0.25f));
        }

        [Test]
        public void Initially_NothingSelected()
        {
            Assert.That(_service.SelectedMold, Is.Null);
        }

        // ── Select ──────────────────────────────────────────────────────────

        [Test]
        public void Select_SetsSelectedMold()
        {
            _service.Select(_MoldA);

            Assert.That(_service.SelectedMold, Is.EqualTo(_MoldA));
        }

        [Test]
        public void Select_Null_DoesNothing()
        {
            _service.Select(null);

            Assert.That(_service.SelectedMold, Is.Null);
        }

        [Test]
        public void Select_SameMold_Deselects()
        {
            _service.Select(_MoldA);
            _service.Select(_MoldA);

            Assert.That(_service.SelectedMold, Is.Null);
        }

        [Test]
        public void Select_DifferentMold_ReplacesSelection()
        {
            _service.Select(_MoldA);
            _service.Select(_MoldB);

            Assert.That(_service.SelectedMold, Is.EqualTo(_MoldB));
        }

        // ── Deselect ────────────────────────────────────────────────────────

        [Test]
        public void Deselect_ClearsSelection()
        {
            _service.Select(_MoldA);
            _service.Deselect();

            Assert.That(_service.SelectedMold, Is.Null);
        }

        [Test]
        public void Deselect_WhenNothingSelected_DoesNothing()
        {
            _service.Deselect();

            Assert.That(_service.SelectedMold, Is.Null);
        }

        // ── Events ──────────────────────────────────────────────────────────

        [Test]
        public void Select_FiresOnMoldSelected()
        {
            MoldState selected = null;
            _service.OnMoldSelected += b => selected = b;

            _service.Select(_MoldA);

            Assert.That(selected, Is.EqualTo(_MoldA));
        }

        [Test]
        public void Select_SameMold_FiresDeselectNotSelect()
        {
            bool selected = false;
            bool deselected = false;
            _service.OnMoldSelected += b => selected = true;
            _service.OnMoldDeselected += b => deselected = true;

            _service.Select(_MoldA);
            selected = false;
            deselected = false;

            _service.Select(_MoldA);

            Assert.That(selected, Is.False, "OnMoldSelected should not fire");
            Assert.That(deselected, Is.True, "OnMoldDeselected should fire");
        }

        [Test]
        public void Select_DifferentMold_FiresDeselectThenSelect()
        {
            MoldState deselectedMold = null;
            MoldState selectedMold = null;
            _service.OnMoldSelected += b => selectedMold = b;
            _service.OnMoldDeselected += b => deselectedMold = b;

            _service.Select(_MoldA);

            _service.Select(_MoldB);

            Assert.That(deselectedMold, Is.EqualTo(_MoldA));
            Assert.That(selectedMold, Is.EqualTo(_MoldB));
        }

        [Test]
        public void Deselect_FiresOnMoldDeselected()
        {
            MoldState deselected = null;
            _service.OnMoldDeselected += b => deselected = b;

            _service.Select(_MoldA);
            _service.Deselect();

            Assert.That(deselected, Is.EqualTo(_MoldA));
        }

        [Test]
        public void Deselect_WhenNothingSelected_FiresNothing()
        {
            bool fired = false;
            _service.OnMoldDeselected += b => fired = true;

            _service.Deselect();

            Assert.That(fired, Is.False);
        }

        [Test]
        public void Select_Null_FiresNoEvents()
        {
            bool selected = false;
            bool deselected = false;
            _service.OnMoldSelected += b => selected = true;
            _service.OnMoldDeselected += b => deselected = true;

            _service.Select(null);

            Assert.That(selected, Is.False);
            Assert.That(deselected, Is.False);
        }

        [Test]
        public void MultipleSelect_CorrectlyTracksEvents()
        {
            int selectCount = 0;
            int deselectCount = 0;
            _service.OnMoldSelected += b => selectCount++;
            _service.OnMoldDeselected += b => deselectCount++;

            _service.Select(_MoldA);
            _service.Select(_MoldB);
            _service.Deselect();
            _service.Select(_MoldA);

            Assert.That(selectCount, Is.EqualTo(3));
            Assert.That(deselectCount, Is.EqualTo(2));
        }
    }
}