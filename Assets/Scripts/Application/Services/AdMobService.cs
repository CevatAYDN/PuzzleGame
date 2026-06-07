using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using PuzzleGame.Domain.Interfaces;
using System;

namespace PuzzleGame.Application.Services
{
    public class AdMobService : IAdService, IDisposable
    {
        private readonly IGameStateMachine _stateMachine;
        private readonly IAnalyticsService _analytics;
        private readonly IAudioService _audio;
        private bool _isConsentFormLoaded = false;
        private bool _isMobileAdsInitialized = false;
        private bool _isInterstitialReady = false;
        private bool _isPersonalizedAdsEnabled = true;
        private AdConsentState _consentState = AdConsentState.Unknown;
        private InterstitialAd _interstitialAd;
        private Action _onAdClosedCallback;

        // IAdService implementation
        public bool IsInitialized => _isMobileAdsInitialized && _isConsentFormLoaded;
        public bool IsPersonalizedAdsEnabled
        {
            get => _isPersonalizedAdsEnabled;
            set => _isPersonalizedAdsEnabled = value;
        }
        public AdConsentState ConsentState => _consentState;

        public AdMobService(IGameStateMachine stateMachine, IAnalyticsService analytics, IAudioService audio)
        {
            _stateMachine = stateMachine;
            _analytics = analytics;
            _audio = audio;

            // Initialize the Google Mobile Ads SDK
            MobileAds.Initialize((initStatus) => {
                _isMobileAdsInitialized = true;
                MoldLogger.LogInfo("Google Mobile Ads SDK initialized.");
            });

            // Load the UMP consent form
            LoadConsentForm();
        }

        private void LoadConsentForm()
        {
            // Create a UMP consent request parameters and load all available forms.
            var consentRequestParameters = new ConsentRequestParameters
            {
                TagForUnderAgeOfConsent = false,
                ConsentDebugSettings = new ConsentDebugSettings
                {
                    DebugGeography = DebugGeography.EEA,
                    TestDeviceHashedIds = new List<string> { "TEST-DEVICE-HASHED-ID" }
                }
            };

            ConsentInformation.Update(consentRequestParameters, OnConsentInfoUpdated);
        }

        private void OnConsentInfoUpdated(FormError error)
        {
            if (error != null)
            {
                MoldLogger.LogError($"Consent form error: {error.Message}");
                return;
            }

            if (ConsentInformation.CanRequestAds())
            {
                _isConsentFormLoaded = true;
                MoldLogger.LogInfo("Consent form loaded successfully.");
            }
            else if (ConsentInformation.IsConsentFormAvailable())
            {
                // Load the consent form
                ConsentForm.Load(OnConsentFormLoaded);
            }
        }

        private void OnConsentFormLoaded(ConsentForm consentForm, FormError error)
        {
            if (error != null)
            {
                MoldLogger.LogError($"Consent form error: {error.Message}");
                return;
            }

            if (consentForm != null)
            {
                _isConsentFormLoaded = true;
                MoldLogger.LogInfo("Consent form loaded successfully.");

                // Show the consent form
                consentForm.Show((formError) =>
                {
                    if (formError != null)
                    {
                        MoldLogger.LogError($"Consent form error: {formError.Message}");
                    }
                    else
                    {
                        MoldLogger.LogInfo("Consent form shown successfully.");
                    }
                });
            }
        }

        public bool IsInterstitialReady()
        {
            return _isInterstitialReady;
        }

        public void ShowInterstitialAd(Action onAdClosed)
        {
            if (!_isMobileAdsInitialized || !_isConsentFormLoaded)
            {
                MoldLogger.LogWarning("AdMob not initialized or consent form not loaded.");
                onAdClosed?.Invoke();
                return;
            }

            if (_interstitialAd != null && _interstitialAd.CanShowAd())
            {
                _onAdClosedCallback = onAdClosed;
                _interstitialAd.Show();
            }
            else
            {
                MoldLogger.LogWarning("Interstitial ad not ready.");
                onAdClosed?.Invoke();
            }
        }

        public void LoadInterstitialAd(string adUnitId)
        {
            if (!_isMobileAdsInitialized || !_isConsentFormLoaded)
            {
                MoldLogger.LogWarning("AdMob not initialized or consent form not loaded.");
                return;
            }

            if (_interstitialAd != null)
            {
                _interstitialAd.Destroy();
                _interstitialAd = null;
            }

            // SDK 11+ API: Use static Load method
            var adRequest = new AdRequest();
            InterstitialAd.Load(adUnitId, adRequest, (InterstitialAd ad, LoadAdError error) =>
            {
                if (error != null)
                {
                    _isInterstitialReady = false;
                    MoldLogger.LogError($"Interstitial ad failed to load: {error.GetMessage()}");
                    return;
                }

                _interstitialAd = ad;
                _isInterstitialReady = true;
                MoldLogger.LogInfo("Interstitial ad loaded successfully.");

                // Register event handlers
                _interstitialAd.OnAdFullScreenContentClosed += HandleOnAdClosed;
                _interstitialAd.OnAdFullScreenContentFailed += (AdError err) =>
                {
                    _isInterstitialReady = false;
                    MoldLogger.LogError($"Interstitial ad failed to show: {err.GetMessage()}");
                };
            });
        }

        private void HandleOnAdClosed()
        {
            _isInterstitialReady = false;
            _onAdClosedCallback?.Invoke();
            _onAdClosedCallback = null;
        }

        public void Dispose()
        {
            if (_interstitialAd != null)
            {
                _interstitialAd.Destroy();
                _interstitialAd = null;
            }
        }

        // IAdService implementation - explicit interface methods
        public void Initialize()
        {
            // SDK is already initialized in constructor, just reload consent if needed
            if (ConsentInformation.CanRequestAds())
            {
                _isConsentFormLoaded = true;
                _consentState = AdConsentState.Accepted;
            }
        }

        public void SetConsentState(AdConsentState state, bool personalizedAds)
        {
            _consentState = state;
            _isPersonalizedAdsEnabled = personalizedAds;
            MoldLogger.LogInfo($"Consent state set to: {state}, Personalized: {personalizedAds}");
        }

        public bool IsRewardedAdReady(RewardedAdType type)
        {
            // Placeholder - not implemented in current version
            return false;
        }

        public void ShowRewardedAd(RewardedAdType type, Action<bool> onComplete)
        {
            // Placeholder - not implemented in current version
            MoldLogger.LogWarning("Rewarded ads not implemented yet.");
            onComplete?.Invoke(false);
        }

        public void PreloadAds()
        {
            // Placeholder - preload logic would go here
            MoldLogger.LogInfo("Preloading ads...");
        }
    }
}