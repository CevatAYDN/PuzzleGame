using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
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
        private InterstitialAd _interstitialAd;
        private Action _onAdClosedCallback;

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
            }

            _interstitialAd = new InterstitialAd(adUnitId);
            _interstitialAd.OnAdLoaded += HandleOnAdLoaded;
            _interstitialAd.OnAdFailedToLoad += HandleOnAdFailedToLoad;
            _interstitialAd.OnAdClosed += HandleOnAdClosed;

            var request = new AdRequest.Builder().Build();
            _interstitialAd.LoadAd(request);
        }

        private void HandleOnAdLoaded(object sender, EventArgs args)
        {
            _isInterstitialReady = true;
            MoldLogger.LogInfo("Interstitial ad loaded successfully.");
        }

        private void HandleOnAdFailedToLoad(object sender, AdFailedToLoadEventArgs args)
        {
            _isInterstitialReady = false;
            MoldLogger.LogError($"Interstitial ad failed to load: {args.LoadAdError.GetMessage()}");
        }

        private void HandleOnAdClosed(object sender, EventArgs args)
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
    }
}