# Ore Sorter — Privacy & Compliance Plan

**Versiyon:** 1.0
**Tarih:** 2026-06-05

---

## 1. Yasal Çerçeve

### 1.1 Uygulanabilir Düzenlemeler

| Düzenleme | Bölge | Yaptırım | Geçerlilik |
|---|---|---|---|
| **GDPR** (General Data Protection Regulation) | EU + EEA | €20M veya ciro %4'ü | Tüm dünya (EU vatandaşı verisi işlenirse) |
| **CCPA** (California Consumer Privacy Act) | California, ABD | $7,500 / ihlal | ABD'de aktif kullanıcı varsa |
| **COPPA** (Children's Online Privacy Protection) | ABD (13 yaş altı) | $50,120 / ihlal | Çocuklara yönelik veya bilinen çocuk verisi |
| **LGPD** (Lei Geral de Proteção de Dados) | Brezilya | R$50M veya ciro %2'si | Brezilyalı kullanıcı varsa |
| **KVKK** (Türkiye) | Türkiye | ₺1,500,000 | Türk vatandaşı verisi işlenirse |
| **PIPL** (Personal Information Protection Law) | Çin | ¥50M veya ciro %5'i | Çin (v1.0 hedefi dışı) |

### 1.2 Hedef Bölgeler (v1.0)

- ✅ Avrupa Birliği (GDPR)
- ✅ Amerika Birleşik Devletleri (CCPA + COPPA)
- ✅ Türkiye (KVKK)
- ✅ Brezilya (LGPD)
- ❌ Çin, Rusya, İran (v1.0 hedefi dışı, Google Play kısıtlaması)

## 2. Veri Toplama (Data Inventory)

### 2.1 Toplanan Veriler

| Veri Tipi | Kaynak | Amaç | GDPR Kategorisi |
|---|---|---|---|
| **Analytics events** | Firebase Analytics | Gameplay metrikleri, retention | Pseudonymous (User ID) |
| **Crash reports** | Sentry | Bug fix, stability | Pseudonymous (Device ID) |
| **Ad impressions** | AdMob | Monetization, ad optimization | Pseudonymous (Ad ID) |
| **Player save data** | PlayerPrefs (local) | Progress, coins, settings | Local only (no server) |
| **Daily challenge seed** | Client computation | Determinism | None (offline) |
| **Device info** | OS auto | Crash context, ad targeting | Pseudonymous |

### 2.2 Toplanmayan Veriler (Explicit)

- ❌ Real name, email, phone
- ❌ Location (GPS, IP-based geolocation)
- ❌ Contacts, photos, microphone
- ❌ Biometric data
- ❌ Social media profiles
- ❌ Payment information (no IAP)

### 2.3 Veri Akışı

```
Oyuncu → [Cihaz (local)]
              ↓
         [Firebase Analytics] → Google Cloud (ABD/EU)
              ↓
         [Sentry] → Sentry Cloud (ABD)
              ↓
         [AdMob] → Google Cloud (ABD)
```

**Veri saklama:** Analytics 14 ay, Sentry 30 gün, AdMob 18 ay (Google policy)

## 3. GDPR Uyum (EU/EEA)

### 3.1 Hukuki Dayanak

| İşleme | Dayanak |
|---|---|
| Analytics (Firebase) | **Açık rıza (Consent)** |
| Ads (AdMob) | **Açık rıza (Consent)** |
| Crash reports (Sentry) | **Meşru menfaat (Legitimate interest)** — oyun kalitesi |
| Local save (PlayerPrefs) | **Sözleşme (Contract)** — oyun işlevselliği |

### 3.2 UMP SDK Entegrasyonu (User Messaging Platform)

**Google UMP SDK** — Google'ın IAB TCF v2.2 uyumlu consent yönetim çözümü.

**Akış:**

```csharp
// 1. İlk açılışta (MainMenu OnEnable)
ConsentRequestParameters request = new ConsentRequestParameters
{
    TagForUnderAgeOfConsent = _isUnder13, // COPPA kontrolü
    ConsentDebugSettings = new ConsentDebugSettings
    {
        DebugGeography = DebugGeography.EEA, // sadece development
        DebugDeviceId = "test-device-id"
    }
};

ConsentInformation.Update(request, (FormError updateError) =>
{
    if (updateError != null)
    {
        Debug.LogError("Consent update error: " + updateError);
        return;
    }
    
    // 2. Form gerekli mi kontrol et
    if (ConsentInformation.IsConsentFormAvailable)
    {
        LoadConsentForm();
    }
    else
    {
        // Consent zaten verilmiş veya EEA dışı
        InitializeAds();
    }
});

void LoadConsentForm()
{
    ConsentForm.Load((ConsentForm form, FormError loadError) =>
    {
        if (loadError != null) return;
        _consentForm = form;
        
        // 3. UI'da göster
        if (ConsentInformation.ConsentStatus == ConsentStatus.Required)
        {
            _consentForm.Show((FormError showError) =>
            {
                if (showError != null) return;
                
                // 4. Sonuç: ConsentStatus kontrol et
                if (ConsentInformation.ConsentStatus == ConsentStatus.Obtained)
                {
                    InitializeAds();
                    EnableAnalytics();
                }
                else
                {
                    // Decline
                    DisableAds();
                    DisableAnalytics();
                }
            });
        }
    });
}
```

### 3.3 Consent UI

**İlk açılışta modal (zorunlu):**

```
┌────────────────────────────────────┐
│  Your Privacy Matters              │
│                                    │
│  We use cookies and similar tech   │
│  to improve your experience and    │
│  show relevant ads.                │
│                                    │
│  [Privacy Policy] (link)           │
│  [Manage Choices] → granular       │
│  [Accept All]                      │
│  [Reject All]                      │
└────────────────────────────────────┘
```

**Granular choices (Manage):**
- [x] Analytics (Firebase)
- [x] Personalized ads
- [x] Non-personalized ads
- [ ] Crash reports (Sentry — opt-out, default off)

**Persistence:** Consent status PlayerPrefs'e kaydedilir (key: `gdpr_consent_v1`)
- Gerekmedikçe tekrar sorma
- Privacy Policy değişirse re-prompt

### 3.4 Veri Sahibi Hakları (GDPR Articles 15-22)

| Hak | Uygulama |
|---|---|
| **Erişim (Art. 15)** | "Data export" feature (v1.1) |
| **Düzeltme (Art. 16)** | Manuel (analytics'te userId düzeltme) |
| **Silme (Art. 17)** | "Delete my data" feature (PlayerPrefs reset) |
| **İşlemeyi kısıtlama (Art. 18)** | Settings → Privacy → Toggle analytics |
| **Taşınabilirlik (Art. 20)** | JSON export (v1.1) |
| **İtiraz (Art. 21)** | Otomatik (consent reject) |

**UI (Settings > Privacy):**
```
[Settings]
  → [Privacy]
      → [Privacy Policy] (link)
      → [Terms of Service] (link)
      → [Analytics] (toggle: on/off)
      → [Personalized Ads] (toggle: on/off)
      → [Delete My Data] (button → confirmation → factory reset)
      → [Data Subject Request] (email: privacy@oresorter.app)
```

## 4. CCPA Uyum (California)

### 4.1 "Do Not Sell My Personal Information" Linki

**Footer'da zorunlu link** (Web Privacy Policy + In-App Settings):

```
[Settings] → [Privacy] → [Do Not Sell My Personal Information]
```

**Aksiyon:** "Opt-out" toggle → AdMob non-personalized ads mode'a geçer

### 4.2 Veri Kategorileri (CCPA disclosure)

| Kategori | Toplanıyor mu? | Satılıyor mu? |
|---|---|---|
| Identifiers (Device ID, Ad ID) | Evet | Hayır (3rd party ile paylaşılmıyor) |
| Commercial info (purchases) | Hayır (IAP yok) | N/A |
| Biometric | Hayır | N/A |
| Internet activity (browsing) | Hayır (oyun içi) | N/A |
| Geolocation | Hayır (GPS yok) | N/A |

## 5. COPPA Uyum (ABD, 13 yaş altı)

### 5.1 Yaş Doğrulama (Age Gate)

**İlk açılışta (consent flow'dan önce):**

```
┌────────────────────────────────────┐
│  Welcome to Ore Sorter!            │
│                                    │
│  Before we start, please confirm   │
│  your age:                         │
│                                    │
│  [Date of Birth Picker]            │
│                                    │
│  [Continue]                        │
│                                    │
│  We use this to comply with        │
│  children's privacy laws (COPPA).  │
└────────────────────────────────────┘
```

**Eğer yaş < 13:**
- Tüm analytics → NoOp
- AdMob → ads disable (no personalized, no non-personalized)
- Sentry → NoOp
- Local save (PlayerPrefs) → OK (oyun işlevselliği)

**Persistence:** PlayerPrefs (key: `user_age_verified`, `user_is_under_13`)

### 5.2 "Designed for Children" Beyanı

Google Play Console > Store Listing:
- ✅ Target audience: **NOT designed primarily for children**
- Target age: 13+
- ESRB Rating: **E10+** (10 yaş üstü)
- PEGI Rating: **7**

**Açıklama:** "Designed for casual gamers aged 13+, not specifically for children"

### 5.3 Aile Politikaları (Family Policy)

**Google Play Family Policy uyumlu:**
- ✅ Tüm reklamlar "family-safe" (AdMob default)
- ❌ "Designed for children" kategorisinde DEĞİL
- ✅ Analytics toplama 13+ ile sınırlı
- ✅ Sentry crash reports 13+ ile sınırlı

## 6. KVKK Uyum (Türkiye)

GDPR ile büyük ölçüde aynı. Ek gereksinimler:

### 6.1 Veri Sorumlusu (Data Controller)

**Türkiye'de mukim olmayan şirket için:**
- VERBİS kaydı gerekli (veri sorumluları sicili)
- Veya Türkiye'de temsilci atanmalı (v1.1'de)

**Şimdilik:** Privacy Policy'de açıkça belirt:
> "Veri sorumlusu: [Şirket adı], iletişim: privacy@oresorter.app"

### 6.2 Açık Rıza

GDPR ile aynı — UMP SDK zaten Türkiye için de çalışıyor.

## 7. Privacy Policy (İçerik Taslağı)

**`/privacy-policy.html` (web) + In-app link:**

```html
# Ore Sorter — Privacy Policy
Last updated: 2026-06-XX

## 1. Introduction
This Privacy Policy describes how [Company Name] ("we", "us")
collects, uses, and protects your information when you play
Ore Sorter.

## 2. Data We Collect
- Analytics: Gameplay events, level progress (Firebase)
- Crash reports: Technical errors, device info (Sentry)
- Ad data: Impressions, clicks (AdMob/Google)
- Local save: Coins, levels, settings (device only)

## 3. How We Use Data
- Improve gameplay and user experience
- Show relevant (or non-relevant) advertisements
- Fix bugs and crashes
- Analyze player behavior (aggregate, anonymized)

## 4. Third-Party Services
- Google Firebase (Analytics)
- Google AdMob (Advertising)
- Sentry (Crash reporting)

## 5. Your Rights
- Access, correct, delete your data
- Opt-out of analytics and personalized ads
- Data portability (v1.1)

## 6. Children's Privacy
We do not knowingly collect data from children under 13.
Age verification on first launch.

## 7. Data Retention
- Analytics: 14 months
- Crash reports: 30 days
- Local save: Until app uninstall

## 8. International Transfers
Data may be transferred to USA (Google Cloud, Sentry).
EU-US Data Privacy Framework applies.

## 9. Contact
privacy@oresorter.app

## 10. Changes
We may update this policy. Continued use = acceptance.
```

## 8. Terms of Service (Kısa Taslak)

**`/terms-of-service.html`:**

```html
# Ore Sorter — Terms of Service

1. Acceptance: By using Ore Sorter, you agree to these terms.
2. License: Free to play, no IAP, no subscription.
3. User conduct: No cheating, no automated play.
4. Intellectual property: Game content © [Company Name].
5. Disclaimer: "As is" basis, no warranty.
6. Limitation of liability: Max liability = $0 (free game).
7. Termination: We may discontinue service with 30 days notice.
8. Governing law: [Jurisdiction].
9. Contact: legal@oresorter.app
```

## 9. In-App Implementation

### 9.1 Yapı

```
Assets/Resources/Legal/
├── PrivacyPolicy.md  (Markdown, in-app gösterim)
└── TermsOfService.md
```

### 9.2 Settings > Privacy Ekranı

```csharp
public class PrivacySettingsView : MonoBehaviour
{
    [Inject] private IPlayerDataService _playerData;
    [Inject] private IAnalyticsService _analytics;
    [Inject] private IAdService _ads;
    
    public void OnToggleAnalytics(bool enabled)
    {
        _playerData.SetAnalyticsEnabled(enabled);
        if (enabled) _analytics.Enable();
        else _analytics.Disable();
    }
    
    public void OnTogglePersonalizedAds(bool enabled)
    {
        _playerData.SetPersonalizedAdsEnabled(enabled);
        _ads.SetPersonalizedAds(enabled);
    }
    
    public void OnDeleteData()
    {
        // Confirmation modal
        // PlayerPrefs.DeleteAll()
        // Re-show consent
        SceneManager.LoadScene("MainMenu");
    }
    
    public void OnOpenPrivacyPolicy()
    {
        Application.OpenURL("https://oresorter.app/privacy");
    }
}
```

## 10. Compliance Checklist (Pre-Launch)

- [ ] Privacy Policy + ToS web'de yayında (https://oresorter.app/privacy)
- [ ] In-app Settings > Privacy > Links çalışıyor
- [ ] UMP SDK consent flow her senaryo için test (Accept, Decline, Granular)
- [ ] Age gate (13+) çalışıyor, <13 = ads + analytics disable
- [ ] "Do Not Sell" linki CCPA için mevcut
- [ ] Firebase Analytics sadece consent sonrası initialize
- [ ] Sentry crash reports sadece consent sonrası initialize
- [ ] AdMob non-personalized ads mode çalışıyor
- [ ] Google Play Console > Data safety formu doldurulmuş
- [ ] App Bundle'da target audience 13+ işaretli
- [ ] ESRB / PEGI rating sertifikası (IARC)

## 11. Post-Launch Monitoring

| Metrik | Threshold | Aksiyon |
|---|---|---|
| Consent reject rate | > %30 | UX review, değer önerisi güçlendir |
| Age gate <13 rate | > %10 | Marketing channel review (yanlış hedefleme) |
| Data deletion requests | > 10/ay | Süreç otomasyonu |
| Privacy Policy complaint | 0 hedef | Hemen yanıt, 72 saat içinde düzeltme |
| Regulatory inquiry | 0 hedef | Hemen legal counsel, 30 gün yanıt |
