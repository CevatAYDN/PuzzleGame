using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;
using VContainer;

namespace PuzzleGame.Presentation.UI
{
    /// <summary>
    /// World map screen — shows 2 biome cards side by side (Crystal Mines + Volcanic Forge).
    /// Each card displays biome name, description, and completion progress.
    /// Clicking a card publishes ShowLevelSelectRequestEvent with biome filter.
    /// Back button publishes HideWorldMapRequestEvent to return to MainMenu.
    /// SRP: only owns world map UI state and navigation.
    /// </summary>
    public class WorldMapController : MonoBehaviour
    {
        private const string LogTag = "[WorldMap]";

        [Header("Biome Cards")]
        [SerializeField] private BiomeCardView crystalMinesCard;
        [SerializeField] private BiomeCardView volcanicForgeCard;

        [Header("Navigation")]
        [SerializeField] private Button backButton;

        private IEventAggregator _events;
        private ILevelProgressService _progress;

        [Inject]
        public void Construct(IEventAggregator events, ILevelProgressService progress)
        {
            _events = events;
            _progress = progress;
        }

        private void OnEnable()
        {
            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
            if (crystalMinesCard != null) crystalMinesCard.OnClicked += OnBiomeCardClicked;
            if (volcanicForgeCard != null) volcanicForgeCard.OnClicked += OnBiomeCardClicked;
            RefreshCards();
        }

        private void OnDisable()
        {
            if (backButton != null) backButton.onClick.RemoveListener(OnBackClicked);
            if (crystalMinesCard != null) crystalMinesCard.OnClicked -= OnBiomeCardClicked;
            if (volcanicForgeCard != null) volcanicForgeCard.OnClicked -= OnBiomeCardClicked;
        }

        private void OnBackClicked()
        {
            MoldLogger.LogInfo($"{LogTag} Back clicked — returning to main menu.");
            _events?.Publish(new HideWorldMapRequestEvent());
        }

        private void OnBiomeCardClicked(Biome biome)
        {
            MoldLogger.LogInfo($"{LogTag} Biome card clicked: {biome}");
            _events?.Publish(new ShowLevelSelectRequestEvent { BiomeFilter = biome });
        }

        private void RefreshCards()
        {
            if (crystalMinesCard != null)
            {
                int completed = BiomeProgress.GetCompletedCount(_progress, Biome.CrystalMines);
                int total = BiomeProgress.GetTotalLevels(Biome.CrystalMines);
                int stars = BiomeProgress.GetStarCount(_progress, Biome.CrystalMines);
                int maxStars = BiomeProgress.GetMaxStars(Biome.CrystalMines);
                crystalMinesCard.Setup(Biome.CrystalMines, completed, total, stars, maxStars);
            }
            if (volcanicForgeCard != null)
            {
                int completed = BiomeProgress.GetCompletedCount(_progress, Biome.VolcanicForge);
                int total = BiomeProgress.GetTotalLevels(Biome.VolcanicForge);
                int stars = BiomeProgress.GetStarCount(_progress, Biome.VolcanicForge);
                int maxStars = BiomeProgress.GetMaxStars(Biome.VolcanicForge);
                volcanicForgeCard.Setup(Biome.VolcanicForge, completed, total, stars, maxStars);
            }
        }
    }

    /// <summary>
    /// Published by WorldMapController when "Back" is clicked.
    /// MainMenuController subscribes to reactivate its root panel.
    /// </summary>
    public class HideWorldMapRequestEvent { }

}
