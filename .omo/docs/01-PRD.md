# Ore Sorter — Product Requirements Document (PRD)

**Versiyon:** 1.0
**Tarih:** 2026-06-05

---

## 1. Hedef Kitle

### 1.1 Persona
- **"Sabah Kahve Oyunçusu"** (35-50 yaş, %60 kadın)
  - Günlük 10-15 dk boş vakit
  - Sakin, stressiz oyunlar tercih eder
  - Reklam tolere eder, IAP'ye soğuk
  - Liquid sort / match-3 / mahjong alışkanlığı

- **"Kuyunun Dibindeki Madenci"** (20-35 yaş, %70 erkek)
  - 30+ dk odaklanma seansları
  - Mekanik mastery ve %100 completion hedefler
  - Hint/Undo'ya para vermez, rewarded video izler
  - Daily streak + leaderboard takıntısı

### 1.2 Coğrafya
- **Soft launch:** ABD, Kanada, İngiltere, Almanya, Türkiye
- **Global expansion (v1.1):** Fransa, İspanya, Brezilya, Japonya, Güney Kore

## 2. Platform & Build

| Özellik | Değer |
|---|---|
| Platform | Android (min SDK 24, target SDK 34) |
| Mimari | ARM64 (ARMv7 deprecated) |
| Rendering | URP 17.4 + Vulkan |
| Scripting | IL2CPP (managed stripping: Low) |
| Build target | Google Play (AAB) |
| Çözünürlük | 1080×1920+ destekli, SafeArea uyumlu |
| Orientation | Portrait (kilitli) |

## 3. Özellik Listesi (v1.0)

### 3.1 Core Features (MUST)
- [x] 50 campaign level (elle tasarlanmış)
- [x] Tutorial (6 adım, interaktif)
- [x] Hint Service (OreSortSolver-backed, max 3/level)
- [x] Undo Service (max 5/level, son hamle dahil)
- [x] Star sistemi (3 hedef, görünür par)
- [x] World Map (2 biome: Crystal Mines + Volcanic Forge)
- [x] Daily Challenge (UTC-deterministic seed, hibrit 7+7)
- [x] Streak tracking (consecutive days)
- [x] Daily Login Bonus (25 coin, 2x rewarded sonrası 50)
- [x] Haptic Feedback (mobile-native)
- [x] Sound Effects (cast, win, lose, hint, undo)
- [x] Background Music (her biome için 1 loop)
- [x] Localization (TR, EN, DE, FR, ES)

### 3.2 Monetization Features (MUST)
- [x] AdMob SDK integration
- [x] Rewarded Video: 4 akış (undo, hint, +10 coin, daily 2x)
- [x] Interstitial: her 3 level arası 1 (skip edilebilir, 5sn)
- [x] Consent flow (GDPR + COPPA)
- [x] "Watch ad" CTA her ekranda (yumuşak satış)

### 3.3 Nice-to-Have (v1.1 sonrası)
- [ ] Cloud Save (Google Play Games)
- [ ] Push notification (daily challenge reminder)
- [ ] Leaderboard (local only, küresel sonra)
- [ ] Themes (Dark/Light mode)
- [ ] Accessibility (color blind mode, büyük font)

## 4. Kullanıcı Akışları

### 4.1 İlk Açılış (D1)
```
App açılır
  → Splash (0.5sn, branded)
  → Consent Dialog (GDPR/COPPA)
    → Accept → Analytics + Ads aktif
    → Decline → NoOp + ads yok
  → Main Menu
    → "New Game" → L01
    → "Continue" → son level
  → L01 (tutorial başlar)
    → 6 adım interaktif tutorial
  → Win screen → "Continue to L02" veya "Replay"
  → World Map (2 biome görünür)
```

### 4.2 Günlük Session (D7+)
```
App açılır
  → Daily Login Bonus modal (25 coin, "Watch ad for 50")
  → Daily Challenge CTA ("Today's seed, play now")
  → Continue last level
  → 5-10 level oyna
  → Her 3 level'da 1 interstitial
  → Win/lose'da rewarded video teklifi
  → App kapat
```

## 5. Başarı Kriterleri (Acceptance)

### 5.1 Definition of Done (v1.0)
- [ ] 50 level solvable + par optimize
- [ ] 6 adım tutorial tüm cihazlarda geçer
- [ ] AdMob test ID'leri ile rewarded + interstitial doğrulanmış
- [ ] Crash-free rate %99.5+ (100 test session)
- [ ] APK < 80MB
- [ ] 5 dilde localization doğrulanmış
- [ ] GDPR/COPPA consent flow tüm kombinasyonlarda çalışıyor
- [ ] Tüm 28+ test pass
- [ ] 5 dakikalık smoke test (tüm akış) sorunsuz

### 5.2 Soft Launch Kriterleri
- [ ] Google Play Console'da "Internal Testing" başarılı
- [ ] Crash-free %99+ (100 kullanıcı, 7 gün)
- [ ] D1 retention %30+
- [ ] Rewarded video opt-in rate %20+

## 6. Out of Scope (v1.0)

- ❌ Multiplayer (online/offline)
- ❌ IAP (ücretli coin bundle, no-ads, premium)
- ❌ Social sharing (Twitter, Facebook, WhatsApp)
- ❌ Account system (email, Google Sign-In)
- ❌ In-game chat
- ❌ Modding / level editor
- ❌ VR / AR mode
- ❌ iOS / iPadOS / Apple Watch
- ❌ PC / Steam / Console
- ❌ Real-money tournaments

## 7. Riskler & Azaltma

| Risk | Olasılık | Etki | Azaltma |
|---|---|---|---|
| AdMob policy violation (EU consent) | Orta | Yüksek | Tam GDPR flow + UMP SDK |
| AI art tutarsız (style drift) | Yüksek | Orta | Tek art director prompt base + 5-10 reference iteration |
| Tutorial atlanır (skip) | Orta | Düşük | Skip 3-level ardından disabled, force 6 steps on first launch |
| Daily challenge solver bug | Düşük | Yüksek | 14-level buffer (7 proc + 7 curated), solver unit test 100% |
| Sentry quota aşımı (>5K events/ay) | Düşük | Orta | Sample %10, dev-only full debug |
| Memory leak (event subscriptions) | Orta | Orta | PlayMode test 100 level sim + IDisposable pattern |
| Soft launch rating < 4.0 | Orta | Yüksek | Pre-launch playtest 20 kişi, NPS score 30+ |

## 8. Ölçüm & İterasyon

- **Haftalık:** Firebase Analytics dashboard review (retention, session length, level completion)
- **Aylık:** Sentry crash report triage, ASO keyword performance
- **Çeyreklik:** Player survey (in-game), competitor analysis (Water Sort, Ball Sort)
- **v1.1 backlog:** Top 5 user feedback + top 3 crash fixes
