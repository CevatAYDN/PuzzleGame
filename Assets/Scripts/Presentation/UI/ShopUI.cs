using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using PuzzleGame.Domain.Interfaces;

namespace PuzzleGame.Presentation.UI
{
    public sealed class ShopUI : MonoBehaviour
    {
        private const string LogTag = "[ShopUI]";

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI coinBalanceText;
        [SerializeField] private Button closeButton;

        [Header("Item List")]
        [SerializeField] private RectTransform itemContainer;
        [SerializeField] private GameObject shopItemPrefab;
        [SerializeField] private ScrollRect scrollRect;

        [Header("Item Detail")]
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private TextMeshProUGUI detailNameText;
        [SerializeField] private TextMeshProUGUI detailDescriptionText;
        [SerializeField] private TextMeshProUGUI detailPriceText;
        [SerializeField] private Button purchaseButton;
        [SerializeField] private Button equipButton;
        [SerializeField] private Image detailPreviewImage;

        private ICosmeticShopService _shop;
        private ICoinWallet _wallet;
        private ILocalizationService _localization;
        private IEventAggregator _events;

        private readonly List<ShopItemView> _itemViews = new List<ShopItemView>();
        private CosmeticItemData _selectedItem;

        [VContainer.Inject]
        public void Construct(
            ICosmeticShopService shop,
            ICoinWallet wallet,
            ILocalizationService localization,
            IEventAggregator events)
        {
            _shop = shop;
            _wallet = wallet;
            _localization = localization;
            _events = events;
        }

        private void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
            if (purchaseButton != null) purchaseButton.onClick.AddListener(OnPurchaseClicked);
            if (equipButton != null) equipButton.onClick.AddListener(OnEquipClicked);

            if (_wallet != null) _wallet.OnBalanceChanged += OnBalanceChanged;
            if (_shop != null) _shop.OnInventoryChanged += OnInventoryChanged;

            BuildItemGrid();
            UpdateCoinDisplay();
        }

        private void OnDestroy()
        {
            if (_wallet != null) _wallet.OnBalanceChanged -= OnBalanceChanged;
            if (_shop != null) _shop.OnInventoryChanged -= OnInventoryChanged;
        }

        private void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            UpdateCoinDisplay();
            RefreshItemViews();
            UpdateDetailPanel();
        }

        private void BuildItemGrid()
        {
            if (itemContainer == null || _shop == null) return;

            foreach (Transform child in itemContainer)
                Destroy(child.gameObject);
            _itemViews.Clear();

            var items = _shop.GetAllItems();
            foreach (var item in items)
            {
                var go = Instantiate(shopItemPrefab, itemContainer);
                var view = go.GetComponent<ShopItemView>();
                if (view == null)
                    view = go.AddComponent<ShopItemView>();

                view.Setup(item, OnItemClicked);
                view.Refresh(_shop.IsOwned(item.id), _shop.GetEquipped(item.cosmeticType) == item.id);
                _itemViews.Add(view);
            }
        }

        private void RefreshItemViews()
        {
            if (_shop == null) return;
            foreach (var view in _itemViews)
            {
                var data = view.ItemData;
                if (data == null) continue;
                view.Refresh(_shop.IsOwned(data.id), _shop.GetEquipped(data.cosmeticType) == data.id);
            }
        }

        private void OnItemClicked(CosmeticItemData item)
        {
            _selectedItem = item;
            UpdateDetailPanel();
        }

        private void UpdateDetailPanel()
        {
            if (detailPanel == null) return;
            if (_selectedItem == null)
            {
                detailPanel.SetActive(false);
                return;
            }

            detailPanel.SetActive(true);

            if (detailNameText != null)
                detailNameText.text = _localization?.GetString(_selectedItem.displayNameKey)
                    ?? _selectedItem.id;

            if (detailDescriptionText != null)
                detailDescriptionText.text = _localization?.GetString(_selectedItem.descriptionKey)
                    ?? "";

            bool owned = _shop != null && _shop.IsOwned(_selectedItem.id);
            bool equipped = owned && _shop != null
                && _shop.GetEquipped(_selectedItem.cosmeticType) == _selectedItem.id;

            if (detailPriceText != null)
            {
                detailPriceText.text = owned
                    ? (equipped ? _localization?.GetString("equipped_label") ?? "Equipped"
                                : _localization?.GetString("owned_label") ?? "Owned")
                    : $"{_selectedItem.coinCost} {_localization?.GetString("coins_currency") ?? "Coins"}";
            }

            if (purchaseButton != null)
            {
                purchaseButton.gameObject.SetActive(!owned);
                purchaseButton.interactable = !owned && _wallet != null
                    && _wallet.CanAfford(_selectedItem.coinCost);
            }

            if (equipButton != null)
            {
                equipButton.gameObject.SetActive(owned && !equipped);
            }

            if (detailPreviewImage != null && _selectedItem.previewIcon != null)
            {
                detailPreviewImage.sprite = _selectedItem.previewIcon;
                detailPreviewImage.enabled = true;
            }
        }

        private void OnPurchaseClicked()
        {
            if (_selectedItem == null || _shop == null || _wallet == null) return;

            if (_shop.TryPurchase(_selectedItem.id, _wallet))
            {
                MoldLogger.LogInfo($"{LogTag} Purchased {_selectedItem.id}");
                UpdateDetailPanel();
                RefreshItemViews();
                UpdateCoinDisplay();
            }
            else
            {
                MoldLogger.LogWarning($"{LogTag} Failed to purchase {_selectedItem.id}");
            }
        }

        private void OnEquipClicked()
        {
            if (_selectedItem == null || _shop == null) return;
            _shop.Equip(_selectedItem.id);
            UpdateDetailPanel();
            RefreshItemViews();
        }

        private void OnCloseClicked()
        {
            gameObject.SetActive(false);
        }

        private void OnBalanceChanged(int newBalance)
        {
            UpdateCoinDisplay();
            UpdateDetailPanel();
        }

        private void OnInventoryChanged(string itemId)
        {
            Refresh();
        }

        private void UpdateCoinDisplay()
        {
            if (coinBalanceText != null && _wallet != null)
                coinBalanceText.text = _wallet.Balance.ToString();
        }
    }

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
