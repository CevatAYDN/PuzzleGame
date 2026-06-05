# Ore Sorter — Monetization Plan (AdMob)

**Versiyon:** 1.0
**Tarih:** 2026-06-05

---

## 1. Strateji Özeti

**Kazanan Oyuncu, Kazanan Biz (Player-First Win-Win):**

| Format | Kullanım | Sıklık | Gelir Tipi |
|---|---|---|---|
| **Rewarded Video** | Oyuncu başlatır (4 akış) | 2-3/gün | Yüksek CPM ($15-30) |
| **Interstitial** | Otomatik (her 3 level) | 0.33/level | Orta CPM ($5-10) |
| **Banner** | KULLANILMAYACAK | 0 | — |

**Neden Banner Yok:**
- Kullanıcı deneyimini bozar
- Düşük CPM ($0.50-1.50)
- Premium hissi zayıflatır

**Neden IAP Yok (Kullanıcı Kararı):**
- Oyuncu talebi: IAP olmasın
- Rewarded video yeterli gelir potansiyeli
- Premium hissi korunur

## 2. AdMob SDK Entegrasyonu

### 2.1 Paket Ekleme

**Google Mobile Ads SDK** (Unity Package)
- Versiyon: 9.x (Unity 6 uyumlu)
- Kaynak: `https://github.com/googleads/googleads-mobile-unity`
- Boyut: ~2MB (IL2CPP sonrası)

**`Packages/manifest.json` ekleme:**
```json
"com.google.ads.mobile": "9.0.0"
```

**veya UPM aracılığıyla:**
- Window > Package Manager > Add package from git URL
- `https://github.com/googleads/googleads-mobile-unity.git?path=packages/source`

### 2.2 Manifest Güncellemesi

**`Assets/Plugins/Android/AndroidManifest.xml`:**
```xml
<manifest>
  <application>
    <meta-data
      android:name="com.google.android.gms.ads.APPLICATION_ID"
      android:value="ca-app-pub-XXXXX~YYYYY" />  <!-- Production ID -->
  </application>
</manifest>
```

### 2.3 Initialization

**`AdMobService.cs` (Infrastructure katmanı):**
```csharp
public class AdMobService : IAdService
{
    public void Initialize()
    {
        MobileAds.Initialize(initStatus => {
            if (initStatus == null) return;
            // Load ads here
            LoadRewardedAd();
            LoadInterstitialAd();
        });
    }
    
    public void LoadRewardedAd() { ... }
    public void LoadInterstitialAd() { ... }
    
    public void ShowRewardedAd(RewardType rewardType, Action onComplete)
    {
        if (_rewardedAd != null && _rewardedAd.CanShowAd())
        {
            _rewardedAd.Show(reward => {
                GrantReward(rewardType);
                onComplete?.Invoke();
                LoadRewardedAd(); // Preload next
            });
        }
    }
    
    public void ShowInterstitialAd()
    {
        if (_interstitialAd != null && _interstitialAd.CanShowAd())
        {
            _interstitialAd.Show();
            LoadInterstitialAd();
        }
    }
}
```

### 2.4 Ad Unit ID'ler

**Test ID'ler (development):**
- App ID: `ca-app-pub-3940256099942544~3347511713`
- Rewarded: `ca-app-pub-3940256099942544/5224354917`
- Interstitial: `ca-app-pub-3940256099942544/1033173712`

**Production ID'ler (AdMob Console'dan alınacak):**
- App ID: `ca-app-pub-XXXXX~XXXXX` (değiştirilecek)
- Rewarded: `ca-app-pub-XXXXX/XXXXX`
- Interstitial: `ca-app-pub-XXXXX/XXXXX`

## 3. Rewarded Video Akışları (4 Tip)

### 3.1 +10 Coin (Level Sonrası)

**Tetikleyici:** Win screen, "Watch ad for 2x coins" CTA
**Kullanıcı Aksiyonu:** Tıkla → Ad → Coin grant
**Reward:** 5 → 10 coin (veya 15 → 20 eğer 3-yıldız bonus)
**Cooldown:** 1 win / 1 ad (spam engeli)

**UI:**
```
[Win Screen]
  ⭐⭐⭐  Excellent!
  Hamle: 5 / 8
  
  [Replay]  [Next →]  [▶ Watch Ad for 2x Coins]
```

### 3.2 +1 Hint (Level İçinde)

**Tetikleyici:** Hint butonu, max 3/level aşıldığında
**Kullanıcı Aksiyonu:** Tıkla → "Out of hints!" modal → "Watch ad for +1 hint"
**Reward:** `maxHintPerLevel` + 1 (sadece o level için)
**Cooldown:** Level başına 1 rewarded hint

**UI:**
```
[Hint Button: 0/3 remaining, disabled]
  ↓ tıklanır
[Modal: "Out of Hints!"]
  Watch a short video to earn 1 more hint for this level.
  
  [Cancel]  [▶ Watch Ad]
```

### 3.3 +1 Undo (Level İçinde)

**Tetikleyici:** Undo butonu, max 5/level aşıldığında
**Kullanıcı Aksiyonu:** Tıkla → "Out of undos!" modal → "Watch ad for +1 undo"
**Reward:** `maxUndoPerLevel` + 1 (sadece o level için)
**Cooldown:** Level başına 1 rewarded undo

**UI:**
```
[Undo Button: 0/5 remaining, disabled]
  ↓ tıklanır
[Modal: "Out of Undos!"]
  Watch a short video to earn 1 more undo for this level.
  
  [Cancel]  [▶ Watch Ad]
```

### 3.4 Daily Bonus 2x (Login Sonrası)

**Tetikleyici:** Daily login modal (her gün ilk açılış)
**Kullanıcı Aksiyonu:** Modal aç → "Watch ad for 50 coin" CTA
**Reward:** 25 → 50 coin
**Cooldown:** Günde 1 kez

**UI:**
```
[Daily Login Modal]
  🎁 Welcome back!
  Day 7 Streak
  Your daily bonus: 25 coins
  
  [Collect 25]  [▶ Watch Ad for 50]
```

## 4. Interstitial Stratejisi

### 4.1 Tetikleyici: Her 3 Level Arası 1

**Kural:**
- Level sayacı `_levelsPlayedSinceLastAd`
- `_levelsPlayedSinceLastAd++` her level tamamlandığında
- `_levelsPlayedSinceLastAd >= 3` → interstitial göster
- Counter sıfırla

**Akış:**
```
Level 1 tamamlandı → counter=1, no ad
Level 2 tamamlandı → counter=2, no ad
Level 3 tamamlandı → counter=3, AD GÖSTER
  → "Ad finished" callback → counter=0
Level 4 tamamlandı → counter=1, no ad
...
```

**Win Screen'de sıralama:**
1. Win modal aç (rewards, stars)
2. 0.5s bekle (UX sürekliliği)
3. Interstitial göster (eğer counter=3)
4. "Next" tıklanır → next level

### 4.2 Yükleme Stratejisi

```csharp
private void LoadInterstitialAd()
{
    var adRequest = new AdRequest();
    _interstitialAd?.Destroy();
    _interstitialAd = new InterstitialAd(_interstitialAdUnitId);
    _interstitialAd.OnAdLoaded += (sender, args) => Debug.Log("Interstitial loaded");
    _interstitialAd.OnAdFailedToLoad += (sender, args) => 
    {
        Debug.LogError("Interstitial failed to load: " + args.LoadAdError);
        // Retry in 30s
        Invoke(nameof(LoadInterstitialAd), 30f);
    };
    _interstitialAd.LoadAd(adRequest);
}
```

### 4.3 Skip Edilebilirlik

Interstitial ads are typically **5-15s skip edilebilir** (AdMob default). Oyuncu istediği zaman kapatabilir.

## 5. AdMob Policy Compliance

### 5.1 GDPR/UMP SDK

**User Messaging Platform (UMP)** SDK — Google'ın consent management çözümü
- Paket: `com.google.ads.ump` (Google Mobile Ads ile birlikte gelir)
- İlk açılışta consent formu gösterir
- EU + EEA ülkeleri için zorunlu
- ABD (CCPA), Brezilya (LGPD) için opsiyonel ama önerilir

**Akış:**
```csharp
var consentRequest = new ConsentRequestParameters
{
    TagForUnderAgeOfConsent = false,
    ConsentDebugSettings = new ConsentDebugSettings { DebugGeography = DebugGeography.EEA }
};
ConsentInformation.Update(consentRequest, ...);
```

Detaylar: `05-PRIVACY-COMPLIANCE.md`

### 5.2 Ad Placement Policy

**Yasak Yerler:**
- ❌ Oyun açılışında (cold start) — AdMob policy violation
- ❌ Çok kısa session'larda (interstitial tetiklenmeden kapatma)
- ❌ Oyun devam ederken (oyuncu distracted)
- ❌ Çocuklara yönelik içerik (COPPA)

**İzin Verilen Yerler:**
- ✅ Level arası (natural break)
- ✅ Win/lose screen sonrası
- ✅ User-initiated (rewarded video CTA)
- ✅ Settings/credits (banner YOK, sadece kozmetik)

### 5.3 Family Policy (COPPA)

**Eğer oyun "designed for children" değilse:**
- Target audience: 13+ (rating: ESRB E10+, PEGI 7)
- Age gate: 13 yaş altı = ads gösterme
- Analytics: NoOp mode aktif

Detaylar: `05-PRIVACY-COMPLIANCE.md`

## 6. Gelir Projeksiyonu

### 6.1 Hipotez (CPM Bazlı)

**Rewarded Video CPM:** $20-30 (gaming, tier 1 countries)
**Interstitial CPM:** $5-10

**Varsayımlar (günlük, 1000 DAU):**
- Rewarded: 2.5 video/gün/kullanıcı × 1000 = 2500 video/gün
- Interstitial: 0.33/level × 12 level/gün/kullanıcı × 1000 = 4000 impression/gün
- eCPM: Rewarded $25, Interstitial $7

**Günlük gelir:**
- Rewarded: (2500 / 1000) × $25 = **$62.50/gün**
- Interstitial: 4000 × $7 / 1000 = **$28/gün**
- **Toplam: ~$90/gün** (1000 DAU)

**Aylık (30K DAU ortalama):**
- $90 × 30 = **$2,700/ay** (yumuşak başlangıç)
- $90 × 30K / 1K = **$2,700/ay** (30K DAU)

**6 ay hedefi (100K DAU):**
- $90 × 100 = **$9,000/ay**

### 6.2 Optimizasyon Alanları

1. **A/B test ad placement:** Level arası vs Win screen
2. **A/B test reward miktarı:** +10 coin vs +5 coin (+15%, +20% video rate)
3. **Rewarded preload:** İlk level yüklenirken preload, latency düşürür
4. **Frequency capping:** Günde max 5 rewarded (oyuncu fatigue önleme)
5. **Mediation:** AdMob + Unity Ads + Meta Audience Network (v1.1, gelir +20-30%)

## 7. Negative Kullanıcı Deneyimi Önleme

### 7.1 "Ad Hell" Önleme

**Kural:** Oyuncu "kapatmadan" oynayabilmeli
- Interstitial: max 0.33/level (her 3 level'da 1)
- Rewarded: opt-in, cooldown 30s (spam engeli)
- Banner: YOK

### 7.2 Ad Başarısızlık Yönetimi

```csharp
private void OnRewardedAdFailedToShow(AdError error)
{
    Debug.LogError($"Rewarded ad failed: {error}");
    // Oyuncuya reward ver (AdMob politikası: başarısız reklam = oyuncu kaybı yok)
    GrantReward(_pendingRewardType);
    ShowToast("Reward granted (ad unavailable)");
}
```

**AdMob Policy:** Reklam başarısız olursa oyuncu yine de ödül almalı. Aksi halde policy violation.

### 7.3 Oyuncu Geri Bildirimi

Her reklam sonrası (interstitial): "Ad reported?" link → Google Feedback
Negative feedback rate > %5 → ad placement review

## 8. Test Stratejisi

### 8.1 Geliştirme Fazı

- [ ] AdMob Test ID'ler ile rewarded + interstitial çalışıyor
- [ ] Preload mekanizması (3G'de bile ad yüklü)
- [ ] Ad başarısız → reward grant (mock ile test)
- [ ] Consent flow her iki şık için (Accept / Decline)
- [ ] Age gate (<13) → ads disable

### 8.2 Internal Testing (Google Play Console)

- [ ] Closed testing 50 internal tester
- [ ] Rewarded video opt-in rate %15+
- [ ] Interstitial skip rate %50+ (beklenen)
- [ ] D1 retention %35+

### 8.3 Soft Launch (1 Hafta)

- Hedef ülke: ABD + Kanada
- Kullanıcı: 1000
- Metrik: D1 retention, rewarded video rate, crash-free

## 9. Riskler

| Risk | Olasılık | Etki | Azaltma |
|---|---|---|---|
| AdMob policy violation | Düşük | Yüksek (hesap kapatma) | Tam GDPR + family policy, ad placement review |
| Reward hack (fake video completion) | Orta | Yüksek | Server-side validation (v1.1, şimdilik client trust) |
| Player churn (ad saturation) | Orta | Yüksek | Frequency cap + soft placement |
| AdMob SDK crash | Çok düşük | Yüksek | Sentry monitoring, fallback no-op |
| Ülke kısıtlaması (Çin, Rusya) | Yüksek | Düşük | Soft launch hedefi dışında, v1.1+ |

## 10. Roadmap

### Hafta 2
- AdMob SDK ekleme + manifest
- `AdMobService.cs` (Infrastructure)
- `IAdService` interface (Application)
- Test ID'ler ile rewarded + interstitial entegrasyonu

### Hafta 3
- 4 rewarded akış implementasyonu
- Interstitial her-3-level logic
- Consent flow (UMP SDK)

### Hafta 4
- Production ID'ler (AdMob Console)
- Analytics event'leri (ad impression, click, reward)
- A/B test setup (Firebase Remote Config)
- Soft launch (Internal Testing)
