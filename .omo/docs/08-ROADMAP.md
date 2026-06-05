# Ore Sorter — 30-Day Development Roadmap

**Versiyon:** 1.0
**Tarih:** 2026-06-05
**Hedef:** v1.0 Soft Launch (Google Play Internal Testing)

---

## 🎯 Genel Bakış

```
HAFTA 1: "Oynanabilir Demo"        — Engine tamam, içerik var
HAFTA 2: "Production-Ready"        — AdMob + MoldController refactor
HAFTA 3: "Compliance + Polish"     — GDPR + Firebase + L26-L50
HAFTA 4: "Soft Launch"             — Test, bug bash, store listing
```

**Kritik yol (her gün):** UI prefab → AdMob → GDPR → Crash test → Store listing

---

## 📅 HAFTA 1: "Oynanabilir Demo" (5 iş günü)

### Gün 1 (Pzt) — Placeholder Art + Setup

**Sabah (4h):**
- [ ] `.omo/docs/` klasör yapısı kurulumu
- [ ] Tüm 8 döküman zaten yazıldı ✓
- [ ] `Assets/Art/` klasör yapısı oluştur
- [ ] AI image generation prompt template'leri hazırla
  - 7 ore prompt'u (her renk)
  - 5 mold prompt'u (her varyant)
  - 2 cork prompt'u
  - 6 UI component prompt'u
- [ ] Midjourney/DALL-E'de ilk 10 görseli üret, `Assets/Art/Ore/` altına kaydet

**Öğleden sonra (4h):**
- [ ] İlk 10 görseli Photoshop/Photopea'da post-process
  - Transparan arka plan
  - 1024×1024 standardizasyon
  - 2px drop shadow ekleme
- [ ] DomainColor hex'leri ile renk doğrulama
- [ ] Sprite atlas (opsiyonel) veya direkt sprite'lar

**Çıktı:** 10 placeholder sprite (7 ore + 2 mold + 1 cork), 1024×1024 PNG

### Gün 2 (Salı) — UI Prefab Seti

**Sabah (4h):**
- [ ] `Assets/UI/Prefabs/` klasörü oluştur
- [ ] **HUDCanvas prefab:** TMP text + Image + Button kompozisyonu
  - TopLeft: Back button
  - TopCenter: Level title (L01 / 50)
  - TopRight: Coin display + Pause button
  - BottomLeft: Move counter (X / Y)
  - BottomRight: Undo + Hint butonları
- [ ] **HUDPresenter.cs** refactor — UI element binding

**Öğleden sonra (4h):**
- [ ] **MainMenuCanvas prefab:** Logo + Continue + New Game + Settings
- [ ] **SettingsCanvas prefab:** Language + Sound + Music + Privacy links
- [ ] **WinPanel prefab:** 3 yıldız + Moves + Replay/Next + "Watch ad" CTA
- [ ] **LosePanel prefab:** Try Again + Hint + Undo butonları

**Çıktı:** 5 UI prefab, tüm binding'ler HudPresenter + yeni sınıflar

### Gün 3 (Çar) — World Map + Level Select

**Sabah (4h):**
- [ ] **WorldMapCanvas prefab:** 2 biome kartı (Crystal Mines, Volcanic Forge)
- [ ] **BiomeCard prefab:** Background + 5×5 grid + node states
- [ ] **LevelNode prefab:** Empty/Current/Completed/Locked state
- [ ] **WorldMapView.cs** — biome navigation, level selection
- [ ] Background gradient image (Crystal Mines mavi-mor, Forge turuncu-siyah)

**Öğleden sonra (4h):**
- [ ] Camera setup (portrait, SafeArea)
- [ ] ScreenTransitionService entegrasyonu (Main Menu → World Map → Game)
- [ ] Music: Main Menu track (Uppbeat free, 1 loop)
- [ ] SFX: Button click, modal açılış

**Çıktı:** World Map UI + scene navigation çalışıyor

### Gün 4 (Per) — L02-L10 Elle Tasarım (Crystal Mines)

**Sabah (4h):**
- [ ] L01.sol dosyasını template olarak kopyala
- [ ] L02-L10 (9 level) için elle tasarım:
  - 5 mold, 2 empty, 3-4 renk, 4 max layer
  - `Molds:` populate, par + good değerleri
  - OreSortSolver ile solvability doğrulama
- [ ] `Level_XX.asset` dosyaları `Assets/Resources/Levels/` altına kaydet

**Öğleden sonra (4h):**
- [ ] L01-L10 PlayMode test: tüm level solvable mı?
- [ ] Difficulty=1 level'lar için par kontrolü (OreSortSolver optimal çözüm)
- [ ] TutorialService ile L01'de 6-step akış doğrulama
- [ ] HintService + UndoService level'lar arası davranış

**Çıktı:** L01-L10 (10 level) solvable + par optimize

### Gün 5 (Cuma) — L11-L25 Elle Tasarım (Crystal Mines devam)

**Sabah (4h):**
- [ ] L11-L20 (10 level) elle tasarım:
  - 5-6 mold, 2 empty, 3-4 renk
  - Difficulty=2 (medium-easy)
  - Par + good + max values
- [ ] L21-L25 (5 level) elle tasarım:
  - 6 mold, 2 empty, 4-5 renk
  - Difficulty=2-3 transition

**Öğleden sonra (4h):**
- [ ] Tüm 25 level için solver doğrulama
- [ ] World Map preview: L01-L25 nodes
- [ ] Daily Challenge seed testi: 7 farklı seed
- [ ] Hafta 1 smoke test: Main Menu → L01 → L25

**Çıktı:** L01-L25 (25 level) playable end-to-end

---

## 📅 HAFTA 2: "Production-Ready" (5 iş günü)

### Gün 6 (Pzt) — MoldController Refactor

**Sabah (4h):**
- [ ] `MoldController.cs` (386 LOC) → 3 class:
  - `MoldVisualController.cs` (görsel, animator) — 120 LOC
  - `MoldInputHandler.cs` (touch/click) — 80 LOC
  - `MoldViewConfig.cs` (pure data, SO) — 50 LOC
- [ ] Tüm dependency injection korunur
- [ ] Mevcut testler (varsa) yeşil kalır

**Öğleden sonra (4h):**
- [ ] MoldPoolInitializer'ı güncelle (yeni component'leri spawn et)
- [ ] IActiveMoldsProvider düzeltmeleri
- [ ] PlayMode test: 25 level sim, memory leak kontrolü
- [ ] Performance: 100 frame allocation profile

**Çıktı:** MoldController SRP uyumlu, test edilebilir

### Gün 7 (Salı) — AdMob SDK Entegrasyonu

**Sabah (4h):**
- [ ] `com.google.ads.mobile` 9.0.0 package ekle
- [ ] AndroidManifest.xml'e AdMob App ID (test ID)
- [ ] `IAdService.cs` interface (Application):
  - `Initialize()`, `ShowRewardedAd(rewardType)`, `ShowInterstitialAd()`, `SetPersonalized(bool)`, `IsReady()`
- [ ] `AdMobService.cs` (Infrastructure) — MobileAds wrapper

**Öğleden sonra (4h):**
- [ ] GameInstaller'a IAdService registration
- [ ] Preloading logic: rewarded + interstitial pre-load
- [ ] Failure handler: ad load fail → user'a toast
- [ ] Test ID ile PlayMode smoke test (reklam yükleniyor mu?)

**Çıktı:** AdMob SDK bağlı, test ID'ler çalışıyor

### Gün 8 (Çar) — 4 Rewarded Video Akışı

**Sabah (4h):**
- [ ] Win screen "Watch ad for 2x coins" CTA → AdMobService.ShowRewarded
- [ ] Hint limit dolu → "Watch ad for +1 hint" modal
- [ ] Undo limit dolu → "Watch ad for +1 undo" modal
- [ ] Daily login → "Watch ad for 50 coin" CTA

**Öğleden sonra (4h):**
- [ ] Reward grant logic: her akış kendi callback'inde
- [ ] Analytics event'leri: `ad_rewarded_offered`, `ad_rewarded_watched`, `ad_rewarded_granted`
- [ ] Toast/feedback: "Reward granted!" 2s
- [ ] Frequency cap: aynı rewarded tip 30s içinde tekrar olamaz

**Çıktı:** 4 rewarded video akışı çalışıyor

### Gün 9 (Per) — Interstitial + Package Audit

**Sabah (4h):**
- [ ] Interstitial logic: `_levelsPlayedSinceLastAd` counter
- [ ] Her 3 level'da 1 interstitial (Win screen'de 0.5s sonra)
- [ ] Skip 5s sonra otomatik
- [ ] Ad fail → silent (oyuncu deneyimi etkilenmez)

**Öğleden sonra (4h):**
- [ ] Package audit:
  - ❌ `com.unity.timeline` kaldır
  - ❌ `com.unity.visualscripting` kaldır
  - ❌ `com.unity.visualeffectgraph` kaldır
  - ❌ `com.unity.ai.navigation` kaldır
  - ❌ `com.unity.multiplayer.center` kaldır
  - ❌ `com.unity.ide.rider` kaldır (kullanılmıyorsa)
- [ ] `Packages/manifest.json` cleanup
- [ ] Build size kontrol: ~80MB hedef (şu an ~150MB, %50 azalma)

**Çıktı:** Interstitial çalışıyor, build size optimize

### Gün 10 (Cuma) — SFX + Müzik Entegrasyonu

**Sabah (4h):**
- [ ] 12 SFX dosyası: Freesound CC0 + Uppbeat free
- [ ] `AudioService.cs` (mevcut) genişlet — her aksiyon için sfx
- [ ] Music crossfade (Main Menu ↔ Game)

**Öğleden sonra (4h):**
- [ ] Sound volume slider (Settings)
- [ ] Music volume slider (Settings)
- [ ] Haptic + SFX senkronizasyonu (cast pour = haptic + sfx)
- [ ] Ses test: 5 farklı cast senaryosu

**Çıktı:** Ses + haptic + UI tutarlı feedback

---

## 📅 HAFTA 3: "Compliance + Polish" (5 iş günü)

### Gün 11 (Pzt) — L26-L50 Elle Tasarım (Volcanic Forge)

**Sabah (4h):**
- [ ] L26-L35 (10 level) Volcanic Forge:
  - 6-7 mold, 2 empty, 4-5 renk, Difficulty=3
  - Sıcak renk paleti (turuncu, kırmızı, sarı)
- [ ] L36-L45 (10 level) Volcanic Forge:
  - 7-8 mold, 2 empty, 5-6 renk, Difficulty=4
- [ ] L46-L50 (5 level) Volcanic Forge Final:
  - 8-9 mold, 2-3 empty, 6-7 renk, Difficulty=5
  - Expert tier

**Öğleden sonra (4h):**
- [ ] Tüm 50 level solvability doğrulama
- [ ] L25 → L26 biome transition: World Map animasyonu
- [ ] Difficulty curve validation: L01=1, L25=3, L50=5
- [ ] Daily Challenge seed determinism: 7 gün × 3 cihaz aynı

**Çıktı:** 50 level tamamı, solvable + par optimize

### Gün 12 (Salı) — GDPR/UMP SDK Entegrasyonu

**Sabah (4h):**
- [ ] `com.google.ads.ump` package (AdMob ile birlikte)
- [ ] `ConsentManager.cs` (Infrastructure):
  - `Initialize()`, `ShowConsentIfNeeded()`, `IsConsentGiven()`, `ResetConsent()`
- [ ] `IConsentManager` interface (Application)
- [ ] `ConsentRequestParameters` setup (EU + TCF v2.2)

**Öğleden sonra (4h):**
- [ ] **Consent Modal UI:** Accept All / Reject All / Manage Choices
- [ ] Granular toggles: Analytics, Personalized Ads, Crash Reports
- [ ] PlayerPrefs persistence: `gdpr_consent_v1`
- [ ] Game flow: İlk açılış → Consent → MainMenu

**Çıktı:** GDPR consent flow çalışıyor

### Gün 13 (Çar) — Firebase Analytics + Age Gate

**Sabah (4h):**
- [ ] Firebase Unity SDK ekle (Analytics + Crashlytics)
- [ ] `FirebaseAnalyticsService.cs` (Infrastructure) — 18 event taxonomy
- [ ] `google-services.json` → `Assets/StreamingAssets/`
- [ ] Event mapping: mevcut NoOp'taki event'ler → Firebase

**Öğleden sonra (4h):**
- [ ] **Age Gate UI:** Date of Birth picker
- [ ] `IAgeVerificationService` + `AgeGateService.cs`
- [ ] <13 yaş → analytics NoOp + ads disable
- [ ] Settings > Privacy > "Verify Age" re-prompt

**Çıktı:** Analytics + age gate entegre

### Gün 14 (Per) — Daily Challenge + Streak Polish

**Sabah (4h):**
- [ ] DailyChallengeService test: 7 farklı gün
- [ ] Streak grace period (1 gün atlanabilir)
- [ ] 7-gün bonus reward (100 coin)
- [ ] Daily Login modal (her açılışta)

**Öğleden sonra (4h):**
- [ ] 7 curated daily levels: `Assets/Resources/Daily/` (7 asset)
- [ ] Procedural seed test: 7 gün × 3 farklı difficulty
- [ ] UI: Daily Challenge card (World Map'te özel)
- [ ] Daily challenge win flow (50 coin + streak +1)

**Çıktı:** Daily challenge akışı tamam

### Gün 15 (Cuma) — Sentry + Crash Reporting

**Sabah (4h):**
- [ ] Sentry Unity SDK ekle
- [ ] `SentryService.cs` (Infrastructure): `ICrashReportingService`
- [ ] Sentry DSN configuration (env variable)
- [ ] Sample rate %10 (free tier 5K/ay)

**Öğleden sonra (4h):**
- [ ] Test crash (deliberately throw) → Sentry capture
- [ ] Try-catch'ler kritik path'lerde
- [ ] Network error handling (analytics + ads + crash)
- [ ] Settings > Privacy > "Crash Reports" toggle

**Çıktı:** Crash reporting çalışıyor

---

## 📅 HAFTA 4: "Soft Launch" (5 iş günü)

### Gün 16 (Pzt) — Final Bug Bash

**Sabah (4h):**
- [ ] Tüm 50 level smoke test (5 dk/level = 4 saat)
- [ ] Daily challenge 7 gün simülasyon
- [ ] 3 farklı cihazda test (Pixel 6, Samsung A52, Xiaomi Redmi 9)
- [ ] Portrait orientation + SafeArea
- [ ] Sound + haptic + music her level'da

**Öğleden sonra (4h):**
- [ ] GDPR flow her kombinasyon (Accept, Decline, Granular, Age <13, Age 13+)
- [ ] AdMob test ID ile her 4 rewarded + 1 interstitial
- [ ] Settings > Privacy > "Delete My Data" factory reset
- [ ] Bug listele (P0, P1, P2) → P0'lar düzelt

**Çıktı:** Critical bug list temizlendi

### Gün 17 (Salı) — Build Optimization

**Sabah (4h):**
- [ ] Texture compression: ASTC (mobile)
- [ ] Audio compression: Vorbis (quality 0.4)
- [ ] IL2CPP build: managed stripping "Low"
- [ ] Shader variant stripping
- [ ] Build size target: < 80MB

**Öğleden sonra (4h):**
- [ ] Memory Profiler integration
- [ ] 100-level sim, snapshot → leak check
- [ ] Frame profiler (60 FPS budget)
- [ ] String allocation grep (Update methods)
- [ ] Final performance report

**Çıktı:** Build < 80MB, 60 FPS stabil, no leaks

### Gün 18 (Çar) — Store Listing Hazırlığı

**Sabah (4h):**
- [ ] App icon final (1024×1024, tüm boyutlar)
- [ ] Feature graphic (1024×500)
- [ ] 6 phone screenshots (1080×1920)
- [ ] Promo video (30s, YouTube link)

**Öğleden sonra (4h):**
- [ ] Short description (80 char)
- [ ] Full description (4000 char, SEO optimized)
- [ ] Privacy Policy web sayfası (`https://oresorter.app/privacy`)
- [ ] Terms of Service web sayfası
- [ ] Press kit (`/press`)

**Çıktı:** Tüm store assets hazır

### Gün 19 (Per) — Google Play Console Setup

**Sabah (4h):**
- [ ] Google Play Console hesabı ($25 one-time)
- [ ] App creation: Ore Sorter
- [ ] Store listing: tüm alanlar doldurulmuş
- [ ] Data safety formu
- [ ] Content rating (IARC questionnaire)
- [ ] Target audience: NOT designed for children
- [ ] Pricing: Free
- [ ] Distribution: All countries (Çin hariç)

**Öğleden sonra (4h):**
- [ ] **Internal Testing track** oluştur
- [ ] Internal testers email listesi (50 kişi)
- [ ] AAB upload (signed)
- [ ] Release notes (alpha)
- [ ] Internal testing rollout: 5% → 25% → 100%
- [ ] Initial internal feedback toplama

**Çıktı:** Internal testing başladı

### Gün 20 (Cuma) — Soft Launch + Handover

**Sabah (4h):**
- [ ] Internal feedback review
- [ ] Crash report review (Sentry)
- [ ] Retention metrics (Firebase): D1, D7
- [ ] Rewarded video opt-in rate
- [ ] Interstitial skip rate

**Öğleden sonra (4h):**
- [ ] Final QA pass
- [ ] README güncelle (release notes)
- [ ] Roadmap v1.1 (90 gün sonrası) planla
- [ ] Handover dokümanı (bir sonraki geliştirici için)
- [ ] 🎉 v1.0 SOFT LAUNCH TAMAM

**Çıktı:** v1.0 hazır, internal testing aktif

---

## 📊 Haftalık KPI Tracking

### Hafta 1 Sonu
- 25 level playable
- 5 UI prefab
- World Map functional
- Ses + haptic çalışıyor

### Hafta 2 Sonu
- AdMob test ID'ler ile çalışıyor
- 4 rewarded + 1 interstitial
- MoldController refactor
- Build size < 80MB

### Hafta 3 Sonu
- 50 level tamamı solvable
- GDPR + COPPA consent
- Firebase + Sentry + Age Gate
- Daily Challenge 7-gün

### Hafta 4 Sonu
- Internal testing başladı
- Tüm compliance ✓
- Build optimize
- Store listing hazır

---

## 🚨 Risk Yönetimi

### Yüksek Risk (Pre-empt)

| Risk | Gün | Azaltma |
|---|---|---|
| 50 level solvability sorunu | 11 | 11. gün tüm level'lar solver doğrula |
| AdMob policy violation | 12-13 | GDPR + UMP tam setup, 13. gün policy review |
| Build size > 80MB | 17 | 17. gün texture/audio compression |
| Crash spike | 18 | 16-17. gün PlayMode test + Sentry monitoring |
| Tutorial skip rate yüksek | 18 | Analytics funnel, 18. gün UX review |

### Orta Risk (Monitor)

| Risk | Gün | Azaltma |
|---|---|---|
| D1 retention < %30 | 20 | A/B test tutorial (v1.1) |
| Rewarded opt-in < %15 | 20 | Placement review (v1.1) |
| Performance low-end | 17 | 4GB RAM device test |

### Düşük Risk (Accept)

| Risk | Kabul Stratejisi |
|---|---|
| İlk review negatif olabilir | Hızlı yanıt, güncelleme |
| ASO ranking düşük | Influencer + paid ads (v1.1) |
| Çin/Rusya kullanıcı kaybı | v1.0 hedefi dışı |

---

## 📈 Başarı Metrikleri (30 gün sonu)

| Metrik | Hedef | Aksiyon (başarısızsa) |
|---|---|---|
| Internal installs | 50+ | Influencer outreach |
| D1 retention | %35+ | Tutorial A/B test |
| D7 retention | %12+ | Daily challenge expansion |
| Crash-free rate | %99+ | Hotfix process |
| Rewarded opt-in | %20+ | Placement review |
| Rating | 4.0+ | UX polish |
| Build size | < 80MB | Texture compression |
| APK install time | < 30s | Bundle optimization |

---

## 🔄 Sonraki Adımlar (v1.1, 30-90 gün)

### Öncelik 1 (Hafta 5-6)
- Cloud save (Google Play Games)
- Push notification (daily challenge reminder)
- Sentry → Crashlytics (daha iyi Android integration)

### Öncelik 2 (Hafta 7-8)
- New biome: Forest Glade (L51-L75)
- Reaction system (color mixing)
- Dark mode

### Öncelik 3 (Hafta 9-12)
- iOS port (karar: Android metriklerine göre)
- A/B test infrastructure (Remote Config)
- Level editor (community content)

### Öncelik 4 (3+ ay)
- Multiplayer (async leaderboard)
- Seasonal events
- IAP (eğer retention %20+ ise — yeniden değerlendir)

---

**Bu roadmap uygulandığında, 30 gün içinde Google Play Internal Testing'e hazır, AAA kalite mobile puzzle game elde edersiniz.**

**Başlamak için:** Hafta 1, Gün 1 (Placeholder art + setup) → talimat verin, başlayalım.
