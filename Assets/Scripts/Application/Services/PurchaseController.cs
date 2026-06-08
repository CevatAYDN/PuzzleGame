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
    /// <remarks>
    /// SECURITY: This controller records analytics for completed purchases but
    /// DOES NOT verify receipts. The real IAP integration MUST add server-side
    /// receipt validation before granting entitlements. The recommended pipeline:
    ///   1. Store-front returns a signed receipt (JWS for Google Play, AppReceipt for iOS).
    ///   2. Forward receipt to your server endpoint over TLS.
    ///   3. Server validates with the store's API, persists the entitlement, and
    ///      notifies the client to grant the in-game item.
    ///   4. Client never grants items based on local receipt verification alone —
    ///      it can be bypassed by a rooted device or a mock store.
    /// <see cref="OnPurchaseCompleted"/> should only be called after the server
    /// has confirmed the purchase; otherwise the analytics event is a lie and
    /// downstream coin grants may unlock paid content for free.
    /// </remarks>
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
