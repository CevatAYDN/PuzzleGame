using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Placeholder controller for in-app purchase events. Will be called by the
    /// future IAP integration (Unity IAP, Google Play Billing, App Store) when a
    /// purchase completes. For now, registers the analytics trigger so the
    /// board can verify the IAPPurchase event flow end-to-end.
    /// </summary>
    public sealed class PurchaseController
    {
        private readonly IAnalyticsService _analytics;

        public PurchaseController(IAnalyticsService analytics)
        {
            _analytics = analytics;
        }

        public void OnPurchaseCompleted(string productId, decimal priceUsd, string currency = "USD")
        {
            _analytics.Track(AnalyticsEvent.IAPPurchase, new Dictionary<string, object>
            {
                { "productId", productId },
                { "priceUsd", priceUsd },
                { "currency", currency }
            });
        }
    }
}
