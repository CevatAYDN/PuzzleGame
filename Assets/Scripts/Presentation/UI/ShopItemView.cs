using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PuzzleGame.Application.Configuration;

namespace PuzzleGame.Presentation.UI
{
    public sealed class ShopItemView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Image previewImage;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private GameObject ownedBadge;
        [SerializeField] private GameObject equippedBadge;
        [SerializeField] private Button clickButton;

        public CosmeticItemData ItemData { get; private set; }
        private System.Action<CosmeticItemData> _onClick;

        private void Awake()
        {
            if (clickButton != null)
                clickButton.onClick.AddListener(OnClicked);
        }

        public void Setup(CosmeticItemData data, System.Action<CosmeticItemData> onClick)
        {
            ItemData = data;
            _onClick = onClick;

            if (nameText != null)
                nameText.text = data.id;
            if (previewImage != null && data.previewIcon != null)
                previewImage.sprite = data.previewIcon;
        }

        public void Refresh(bool owned, bool equipped)
        {
            if (priceText != null)
                priceText.text = owned ? "" : ItemData.coinCost.ToString();
            if (ownedBadge != null)
                ownedBadge.SetActive(owned && !equipped);
            if (equippedBadge != null)
                equippedBadge.SetActive(equipped);
        }

        private void OnClicked()
        {
            _onClick?.Invoke(ItemData);
        }
    }
}
