using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Presentation.UI
{
    /// <summary>
    /// Individual biome card UI component. Shows biome name, progress text, and click handler.
    /// </summary>
    public class BiomeCardView : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private TextMeshProUGUI biomeNameText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI starProgressText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private GameObject completedBadge;
        [SerializeField] private Button button;

        [Header("Biome Colors")]
        [SerializeField] private Color crystalMinesColor = new Color(0.30f, 0.55f, 0.95f);
        [SerializeField] private Color volcanicForgeColor = new Color(0.95f, 0.50f, 0.15f);

        public event Action<Biome> OnClicked;

        private Biome _biome;

        private void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            if (button != null) button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(HandleClick);
        }

        public void Setup(Biome biome, int completed, int total, int stars, int maxStars)
        {
            _biome = biome;

            if (biomeNameText != null)
            {
                biomeNameText.text = biome == Biome.CrystalMines ? "Crystal Mines" : "Volcanic Forge";
            }
            if (progressText != null)
            {
                progressText.text = $"{completed}/{total}";
            }
            if (starProgressText != null)
            {
                starProgressText.text = $"★ {stars}/{maxStars}";
            }
            if (backgroundImage != null)
            {
                backgroundImage.color = biome == Biome.CrystalMines ? crystalMinesColor : volcanicForgeColor;
            }
            if (completedBadge != null)
            {
                completedBadge.SetActive(completed >= total);
            }
        }

        private void HandleClick()
        {
            OnClicked?.Invoke(_biome);
        }
    }
}
