# Ore Sorter — ASO & Marketing Plan

**Versiyon:** 1.0
**Tarih:** 2026-06-05

---

## 1. App Store Identity

### 1.1 App Metadata

| Field | Value |
|---|---|
| **App Name** | Ore Sorter |
| **Tagline** | Sort the Ores. Master the Forge. |
| **Short Description** | (80 char max) Mindful liquid sort puzzle with daily challenges |
| **Full Description** | (4000 char max) [see Section 2] |
| **Category** | Games > Puzzle |
| **Content Rating** | E10+ (ESRB), 7 (PEGI) |
| **Target Audience** | 13+ (NOT designed for children) |
| **Price** | Free |
| **In-App Purchases** | None |
| **Contains Ads** | Yes (AdMob) |
| **Languages** | English (default), Turkish, German, French, Spanish |

### 1.2 Brand Voice

- **Tone:** Calm, focused, encouraging
- **Persona:** Friendly mentor, not aggressive salesperson
- **Keywords:** mindful, satisfying, relaxing, daily, mastery, puzzle
- **Avoid:** Stress, gambling, pay-to-win

## 2. Store Listing (Google Play)

### 2.1 App Name (50 char limit)

```
Ore Sorter - Liquid Sort Puzzle
```

**Rationale:** Brand name + category descriptor (SEO boost for "liquid sort" + "puzzle")

### 2.2 Short Description (80 char limit)

```
Mindful liquid sort puzzle with 50 levels, daily challenges & rewards
```

### 2.3 Full Description (4000 char limit)

```
🧩 SORT THE ORES. MASTER THE FORGE.

Ore Sorter is a calm, satisfying liquid sort puzzle game that challenges
your mind with 50 hand-crafted levels across two beautiful biomes:
Crystal Mines and Volcanic Forge.

Every pour is intentional. Every solved mold is a small victory.

✨ FEATURES

• 50 hand-designed levels — From gentle first steps to expert-tier puzzles
• Two unique biomes — Crystal Mines (calm, blue) and Volcanic Forge (warm, fiery)
• Daily challenges — New puzzle every day, deterministic seed
• Streak rewards — Come back daily, build your streak, earn bonus coins
• Free, forever — No IAP, no subscriptions, no pay-to-win
• Ad-supported — Watch short videos for extra hints and coins (your choice)
• Haptic feedback — Feel every pour, every cork pop, every star earned
• 5 languages — English, Turkish, German, French, Spanish
• Offline play — No internet required for campaign levels
• Beautiful minimalist art — Flat design inspired by Monument Valley

🎯 HOW TO PLAY

Tap a mold (bottle) to select it, then tap another mold to pour.
Same-color ores stack. Empty molds accept any color.
Solve the level by sorting all ores into single-color molds.

⭐ STARS & MASTERY

Each level shows you the target: how many moves for 3 stars.
Replay for mastery. Beat your previous best.

📅 DAILY CHALLENGE

A new puzzle every day, with the same seed for all players worldwide.
Beat it to extend your streak and earn bonus rewards.

🎨 ABOUT THE ADS

We use rewarded video ads — you choose when to watch.
Earn extra hints, undos, or coins. No forced video ads during gameplay.
Interstitial ads only between levels (every 3rd), and you can always skip.

🔒 PRIVACY FIRST

We respect your privacy:
- GDPR compliant (EU consent flow)
- COPPA compliant (age verification)
- No personal data collection without consent
- Local save only — your progress stays on your device

Made with care by a small indie team.
No publishers, no investors, just good puzzles.

Questions or feedback? hello@oresorter.app
```

## 3. Keywords & ASO

### 3.1 Primary Keywords (50 char, Google Play keyword field)

```
liquid sort, water sort, puzzle, sort puzzle, color sort
```

**Rationale:** "liquid sort" + "water sort" = highest volume in puzzle category

### 3.2 Long-tail Keywords (in description naturally)

- liquid sort puzzle
- water sort puzzle
- color sort game
- pour puzzle
- ball sort
- bottle sort
- relaxing puzzle
- offline puzzle
- daily puzzle
- streak game

### 3.3 Competitor Analysis (Tier 1)

| App | Downloads | Rating | ASO Note |
|---|---|---|---|
| Water Sort Puzzle | 100M+ | 4.5 | En büyük, "color match" vurgusu |
| Ball Sort Puzzle | 50M+ | 4.6 | 3D görsel, "satisfying" vurgu |
| Sort Water | 10M+ | 4.3 | Minimalist, reklam-free versiyon ücretli |
| Liquid Sort | 5M+ | 4.4 | Indie, daily challenge vurgusu |

**Diferansiyasyon:** "Mindful, no IAP, 50 hand-crafted levels, 2 biomes" — rakiplerden ayrışma noktası

### 3.4 Localization (5 Dil)

| Dil | Başlık | Açıklama |
|---|---|---|
| EN (default) | Ore Sorter - Liquid Sort Puzzle | (yukarıdaki) |
| TR | Ore Sorter - Sıvı Sıralama Bulmaca | Cevher sıralama, sakin, günlük |
| DE | Ore Sorter - Farbsortier-Puzzle | Kristall-Minene, Vulkanschmiede |
| FR | Ore Sorter - Puzzle de tri de liquide | Minimaliste, quotidien, primé |
| ES | Ore Sorter - Rompecabezas de clasificación | Relajante, sin compras, diario |

**Not:** Tüm lokalizasyon `LocalizationService.cs` (56 key × 5 dil) zaten mevcut.

## 4. Visual Assets (Google Play Console)

### 4.1 App Icon (1024×1024)

**Spec:** 1024×1024 PNG, 32-bit, no transparency
**Design:** Logo + Crystal Mines gradient background (mavi-mor)
**Reference:** [03-ART-BIBLE.md section 6.2]

### 4.2 Feature Graphic (1024×500)

**Layout:**
```
┌──────────────────────────────────────────────┐
│  ORE                                          │
│  SORTER                                       │  ← Logo, büyük, sol
│  ───────                                      │
│  Sort the Ores. Master the Forge.            │  ← Tagline
│                                               │
│  [Game screenshot: 5 molds with ores]        │  ← Sağ, 3D mockup
│  [Mineral icons floating]                     │
└──────────────────────────────────────────────┘
```

### 4.3 Phone Screenshots (min 4, max 8)

**Spec:** 1080×1920 veya 1080×2400, PNG/JPEG
**İçerik sırası:**

1. **Hero shot:** Main menu + L01 in-game (kristal mines)
   - Caption: "50 hand-crafted levels"
2. **Gameplay:** Cast in progress (animasyonlu gösterim, mock)
   - Caption: "Pour with intention"
3. **Win screen:** 3 yıldız + "Excellent!"
   - Caption: "Master every level"
4. **Daily challenge:** Today's seed + streak
   - Caption: "Daily challenges & streaks"
5. **World map:** Crystal Mines + Volcanic Forge
   - Caption: "Two beautiful biomes"
6. **AdMob rewarded:** "Watch ad for 2x coins" CTA
   - Caption: "Watch ads for rewards (optional)"

**Hepsi:** Crystal Mines renk paleti (sakin, inviting) + Volcanic Forge (sıcak) vurgusu

### 4.4 Tablet Screenshots (opsiyonel, max 8)

Aynı phone screenshot'lar 16:10 aspect ratio'da (1920×1200).

### 4.5 Promo Video (30s, opsiyonel ama önerilir)

**Storyboard:**
- 0-3s: Logo reveal + slogan ("Sort the Ores. Master the Forge.")
- 3-8s: Gameplay (cast animation, ore flow)
- 8-12s: Win screen + stars
- 12-18s: World map (biome transition)
- 18-23s: Daily challenge card + streak
- 23-27s: "Free, forever. No IAP." text
- 27-30s: CTA "Download Ore Sorter" + store badge

**Format:** 1080p, 30fps, H.264, 30 saniye max

## 5. Press Kit

### 5.1 URL
`https://oresorter.app/press`

### 5.2 İçerik
- Logo (PNG, SVG, full color, mono)
- Screenshots (tüm boyutlar)
- Feature graphic
- Game factsheet (1 sayfa PDF)
- Trailer (YouTube embed)
- Press release (TR + EN)

### 5.3 Game Factsheet (1 sayfa PDF)

```
ORE SORTER — PRESS KIT

Game: Ore Sorter
Genre: Liquid Sort Puzzle
Platform: Android (iOS planned 2027)
Price: Free (ad-supported, no IAP)
Players: Single-player
Languages: EN, TR, DE, FR, ES
Release: Q3 2026 (soft launch)

Features:
- 50 hand-crafted levels
- 2 unique biomes
- Daily challenges with streak tracking
- Rewarded video ads (player-initiated)
- 6-step interactive tutorial
- GDPR + COPPA compliant

Developer: [Indie Team / Studio Name]
Contact: press@oresorter.app
Website: https://oresorter.app
```

## 6. Marketing Channels

### 6.1 Soft Launch (Hafta 4)

**Kanallar:**
- Google Play Internal Testing (50 tester)
- Reddit: r/AndroidGaming, r/IndieGaming, r/puzzlegames
- Discord: Indie Game Dev, Casual Games Connect
- ProductHunt (upcoming launch)

**Hedef:**
- 1000 indirme (hafta 1)
- %40 D1 retention
- 4.0+ rating

### 6.2 Global Launch (v1.0 → v1.1, ~6 hafta sonra)

**Kanallar:**
- Google Play Store (production)
- App of the Day submission (Google)
- Featured game outreach (indie game media)
- YouTube influencer outreach ($500-2000 budget)
- TikTok organic (short gameplay clips, 15s)

**Hedef:**
- 10K indirme (hafta 1)
- %35 D1 retention
- 4.5+ rating

### 6.3 Ongoing (v1.1+)

- Seasonal events (no implementation in v1.0, planned)
- Level packs (DLC, optional)
- New biome (Crystal Caves, Forest Glade)
- New mechanics (reaction system)
- Community Discord (player feedback, suggestions)

## 7. Pre-Launch Checklist (ASO)

### 7.1 Google Play Console

- [ ] App name (50 char)
- [ ] Short description (80 char)
- [ ] Full description (4000 char)
- [ ] App icon (1024×1024)
- [ ] Feature graphic (1024×500)
- [ ] Phone screenshots (min 4, 1920×1080 veya 1080×1920)
- [ ] Tablet screenshots (opsiyonel, min 4)
- [ ] Promo video (opsiyonel, YouTube link)
- [ ] Category: Games > Puzzle
- [ ] Tags: puzzle, casual, offline, free
- [ ] Contact: email, website
- [ ] Privacy policy URL
- [ ] Content rating (IARC)
- [ ] Target audience: 13+
- [ ] Contains ads: Yes
- [ ] In-app purchases: No
- [ ] Pricing: Free
- [ ] Distribution: All countries (veya seçili)

### 7.2 Data Safety Form (Google Play)

- [ ] Data collected: Device ID (analytics), Ad ID (AdMob)
- [ ] Data shared: Google (AdMob, Firebase), Sentry
- [ ] User consent: Yes (GDPR flow)
- [ ] Data encryption: TLS in transit
- [ ] User can request deletion: Yes (in-app "Delete my data")
- [ ] Children: Not designed for children, no data from <13

### 7.3 Store Listing Experiments (A/B Test)

Google Play Store Listing Experiments (closed beta):
- Test 1: App icon variant (mavi vs turuncu theme)
- Test 2: Screenshot order (gameplay vs win screen first)
- Test 3: Short description (minimalist vs feature-focused)

## 8. User Acquisition (Soft Launch)

### 8.1 Organic (Ücretsiz)

**Reddit Posts (her hafta 1):**
- "I made a minimalist liquid sort puzzle with no IAP — feedback?"
- "Show HN-style" posts with screenshots
- r/IndieGaming "Feedback Friday"

**Discord Communities:**
- #showcase channels
- Game dev communities
- Puzzle game enthusiasts

**ProductHunt:**
- "Upcoming" listing 2 hafta önce
- Launch day post with demo video

### 8.2 Paid (Opsiyonel, $200-500 budget)

**YouTube Influencer (micro, 5K-50K subscribers):**
- 1-2 review video
- $100-300 per video
- Hedef: 5K-10K views

**TikTok Ads (mobile game discovery):**
- $50-100 budget, 7 gün
- 15s video (gameplay + CTA)
- Target: 18-35, puzzle game interest

**Google Ads UAC (Universal App Campaigns):**
- $100-200 budget, 14 gün
- Auto-optimize (CPI target: $0.50-1.00)
- 2-3 creative variations

## 9. Metrikler (KPIs)

### 9.1 Soft Launch (Hafta 1-4)

| Metrik | Hedef |
|---|---|
| Impressions | 50K |
| Page visits | 5K |
| Install (CPI) | 1K (< $1) |
| D1 retention | %35+ |
| D7 retention | %12+ |
| Avg session | 8-12 min |
| Levels/day (active user) | 8-12 |
| Rewarded video rate (DAU) | 2.5/gün |
| Crash-free rate | %99+ |
| Rating | 4.0+ |

### 9.2 Global Launch (Hafta 5-8)

| Metrik | Hedef |
|---|---|
| Impressions | 500K |
| Installs | 50K |
| D1 retention | %35+ |
| D7 retention | %12+ |
| D30 retention | %5+ |
| DAU/MAU | %20+ |
| LTV (30 gün) | $0.10+ |
| Rating | 4.3+ |
| Reviews | 500+ |

### 9.3 Long-term (6 ay)

| Metrik | Hedef |
|---|---|
| Total installs | 500K |
| DAU | 30K (avg) |
| MAU | 100K (avg) |
| Monthly revenue | $5-10K (AdMob) |
| LTV (6 ay) | $0.30+ |
| D90 retention | %8+ |
| Rating | 4.4+ stable |

## 10. Risks & Azaltma

| Risk | Olasılık | Etki | Azaltma |
|---|---|---|---|
| Düşük ASO ranking | Yüksek | Yüksek | Keyword research, A/B test, influencer |
| Negative reviews (ads complaint) | Orta | Orta | Soft placement, player-first design |
| Low retention (D1 < %30) | Orta | Yüksek | Tutorial refine, onboarding A/B test |
| Crash spike | Düşük | Çok yüksek | Sentry monitoring, hotfix process |
| Competitor copy (white-label) | Yüksek | Düşük | Brand identity, unique art style |
| AdMob policy violation | Düşük | Yüksek | Compliance review, legal check |

## 11. Post-Launch İterasyon

### 11.1 İlk 30 Gün (Stabilizasyon)
- Crash fix (P0, P1)
- Tutorial refinement (analytics funnel)
- Economy rebalance (if D1 retention < %35)
- ASO optimization (if install < target)

### 11.2 30-60 Gün (Optimization)
- A/B test rewarded video placement
- Daily challenge expansion (more curated levels)
- Localization QA (all 5 languages)
- Performance optimization (low-end devices)

### 11.3 60-90 Gün (Growth)
- Promo video + influencer outreach
- TikTok campaign
- Discord community setup
- v1.1 feature planning (player feedback)

### 11.4 90+ Gün (v1.1 Prep)
- Cloud Save (Google Play Games)
- Push notifications
- New biome (Forest Glade)
- New mechanics (reaction system)
- iOS port (decision based on Android metrics)
