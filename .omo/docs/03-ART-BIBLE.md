# Ore Sorter — Art Bible

**Versiyon:** 1.0
**Tarih:** 2026-06-05

---

## 1. Genel Sanat Yönetimi

### 1.1 Stil: Minimalist Flat (Monument Valley esinli)

**Karakteristikler:**
- Düz renkler, gradient, ince çizgiler
- Tipografik UI (büyük, okunaklı fontlar)
- Beyaz/negatif alan bol kullanımı
- Hafif drop shadow (max 2px, %10 opacity)
- 8-bit/pixel art YOK, realistic 3D YOK

**İlham Kaynakları:**
- Monument Valley (ustagames)
- Lara Croft GO (Square Enix)
- Two Dots (Playdots)
- Threes! (Sirvo)

### 1.2 Renk Sistemi

**Domain Color Palette** (`DomainColor` struct):

| Renk Adı | Hex | Kullanım |
|---|---|---|
| Crimson | #E63946 | Ore 1 (Crystal Mines ana) |
| Sapphire | #1D3557 | Ore 2 |
| Emerald | #2A9D8F | Ore 3 |
| Amber | #F4A261 | Ore 4 (Volcanic Forge ana) |
| Magma | #E76F51 | Ore 5 |
| Gold | #E9C46A | Ore 6 |
| Lavender | #B084CC | Ore 7 (uzman) |
| Slate | #6C757D | UI accent (nötr) |

**Background Gradients:**
- **Crystal Mines:** `#1a2a6c → #b8c6db` (linear, top to bottom)
- **Volcanic Forge:** `#ff6b35 → #1a0e0a` (radial, center to edge)

**UI Colors:**
- Primary: `#2A9D8F` (Emerald — buton, link)
- Secondary: `#1D3557` (Sapphire — başlık)
- Accent: `#F4A261` (Amber — yıldız, highlight)
- Background: `#F1FAEE` (nötr, ana menü)
- Error: `#E63946` (Crimson — uyarı, hata)
- Text: `#1D1D1D` (ana), `#6C757D` (ikincil)

### 1.3 Tipografi

**Font Seçimi:** Google Fonts (ücretsiz, ticari kullanıma açık)

| Kullanım | Font | Ağırlık | Boyut |
|---|---|---|---|
| Başlık (Level title) | **Poppins** | Bold | 36pt |
| Alt başlık (Modal title) | Poppins | SemiBold | 24pt |
| Body (Button, text) | **Inter** | Regular | 18pt |
| Caption (Hint, debug) | Inter | Light | 14pt |
| Sayı (Hamle, coin) | **JetBrains Mono** | Medium | 20pt |

**Lokalizasyon:** Tüm fontlar Latin Extended desteklemeli (Türkçe, Almanca aksan işaretleri)

## 2. Asset Listesi (AI Üretilecek)

### 2.1 Ore Sınıfı (7 Renk)

**Format:** SVG → PNG (1024×1024), transparan arka plan
**Stil:** Yuvarlak, parlak, hafif gradient (3D illüzyon)
**Boyut:** UI'da ~64×64 px, 5 mold × 4 layer = 28 görünür

**Midjourney/DALL-E Prompt Template:**
```
A single round {color_name} ore crystal, flat design, minimal shading,
single object, isolated on white background, vector style, 1024x1024,
{color_hex} primary color, no shadows, no text
```

### 2.2 Mold (Kalıp) — 5 Varyant

**Format:** SVG → PNG, 256×512 px (dikey)
**Stil:** Basit geometrik kap, hafif 3D illüzyon (gradient + highlight)
**Varyantlar:**
1. **Classic Glass** (Crystal Mines ana)
2. **Crystal Cut** (Crystal Mines boss)
3. **Iron Crucible** (Volcanic Forge ana)
4. **Obsidian** (Volcanic Forge boss)
5. **Trophy** (win animation, altın kaplama)

**Prompt Template:**
```
A {material} crucible bottle, flat design, transparent glass effect,
vertical orientation, 4 layer capacity, single object, isolated on white,
{color} tint, soft highlight, no text, vector style, 256x512
```

### 2.3 Cork (Tıpa)

**Format:** PNG, 128×128 px
**Stil:** Yuvarlak, doğal mantar dokusu (simplified)
**Varyant:** 2 — Light Cork (Crystal), Dark Cork (Volcanic)

### 2.4 UI Components

| Component | Boyut | Stil |
|---|---|---|
| Button (Primary) | 240×64 | Yuvarlak köşe (12px), gradient fill, drop shadow |
| Button (Secondary) | 240×64 | Beyaz fill, 2px border, no shadow |
| Button (Icon) | 64×64 | Yuvarlak, sadece icon |
| Card (Level) | 200×200 | Yuvarlak köşe, 2 border, gölge |
| Modal/Panel | 600×400 | Yuvarlak köşe (24px), blur backdrop |
| Star (Empty) | 64×64 | Outline only, gri |
| Star (Filled) | 64×64 | Solid altın, gradient |
| Coin Icon | 48×48 | Yuvarlak, altın, "$" veya "C" |
| Mold Icon (HUD) | 48×48 | Küçük, outline only |

### 2.5 Background

**Format:** PNG veya SVG, 1080×1920
**Varyant:** 2 (Crystal Mines, Volcanic Forge — yukarıdaki gradient)
**Animasyon:** Subtle parallax (0.5x scroll speed), 1-2 floating particles (dust, crystal, ember)

### 2.6 Particle Effects

| Effect | Kullanım | Süre |
|---|---|---|
| Cast splash | Cast tamam | 0.3s |
| Hint glow | Hint aktif | 1.0s loop (continuous) |
| Star burst | Star kazan | 0.5s |
| Win confetti | Win screen | 2.0s |
| Ember rise (Forge) | Background, idle | continuous |
| Snow/Mist (Mines) | Background, idle | continuous |

## 3. World Map Sanatı

### 3.1 Layout

```
┌─────────────────────┐
│   ╔═════════════╗   │
│   ║   CRYSTAL   ║   │   ← Biyom kartı 1
│   ║    MINES    ║   │
│   ║   [5x5]     ║   │
│   ╚═════════════╝   │
│                     │
│   ╔═════════════╗   │
│   ║  VOLCANIC   ║   │   ← Biyom kartı 2 (L25 sonrası açılır)
│   ║    FORGE    ║   │
│   ║   [5x5]     ║   │
│   ╚═════════════╝   │
└─────────────────────┘
```

### 3.2 Biyom Kartı Detayı

- **Background:** Biome-specific gradient
- **Decorative:** Floating crystal/ember particles (2-3 adet, subtle animation)
- **Title:** Poppins Bold, 32pt, beyaz, drop shadow
- **Level nodes:** Yuvarlak 40×40, ortada sayı
- **Node states:**
  - Empty: outline only, %50 opacity
  - Current: pulse animation (1.0s), glow
  - Completed: filled, ✓ checkmark
  - Locked: lock icon, %30 opacity

## 4. Animation Style

### 4.1 UI Animations
- **Modal açılış:** Slide up + fade in (0.3s, ease-out)
- **Button hover:** Scale 1.05x (0.2s)
- **Button press:** Scale 0.95x (0.1s)
- **Star kazan:** Pop + sparkle (0.5s)

### 4.2 Gameplay Animations
- **Cast başla:** Source mold tilt -5°, target mold tilt +5° (0.2s)
- **Ore flow:** Continuous pour animation (0.8s per layer)
- **Cork pop:** Cork scale 1.0 → 0.0 (0.2s, ease-in)
- **Win:** All molds scale 1.0 → 1.1 → 1.0 (0.5s, ease-out), confetti burst

### 4.3 Easing
- Default: `EaseType.OutQuad`
- Modal: `EaseType.OutBack`
- Cast: `EaseType.InOutSine`
- Star: `EaseType.OutElastic`

## 5. Ses Sanat Yönetimi (AI + Royalty-Free)

### 5.1 SFX

| Ses | Açıklama | Süre | Kaynak |
|---|---|---|---|
| Cast pour | Su/lava akışı | 0.8s loop | Freesound "water pour" CC0 |
| Cast land | Ore bounce | 0.15s | Freesound "drop on glass" |
| Cork pop | Hava çıkışı | 0.2s | Freesound "pop cork" |
| Hint chime | Kristal | 0.5s | Freesound "crystal chime" |
| Undo swoosh | Reverse whoosh | 0.4s | Freesound "swoosh reverse" |
| Win fanfare | Uplift major chord | 2.0s | Uppbeat "win fanfare" free |
| Lose tone | Minor 3rd descent | 1.5s | Uppbeat "lose" free |
| Coin collect | Classic coin | 0.3s | Freesound "coin pick up" |
| Star sparkle | High pitch | 0.5s | Freesound "magic sparkle" |
| Button click | Subtle tap | 0.1s | Freesound "ui click soft" |
| Ambient Mines | Cave wind + water drop | 30s loop | Freesound "cave ambient" |
| Ambient Forge | Lava bubble + crackle | 30s loop | Freesound "volcano ambient" |

### 5.2 Müzik

| Track | Mood | BPM | Kaynak |
|---|---|---|---|
| Main Menu | Neutral, inviting | 80 | Uppbeat "puzzle menu" |
| Crystal Mines | Calm, ambient, synth | 60 | Uppbeat "calm ambient" |
| Volcanic Forge | Percussive, synth | 90 | Uppbeat "energetic percussive" |
| Win Jingle | Triumphant, 4-bar | 100 | Uppbeat "win jingle" |
| Lose Jingle | Gentle, encouraging | 80 | Uppbeat "retry jingle" |

**Lisans:** Tüm müzikler Uppbeat Free License (ticari kullanıma açık, attribution gerekli — Credits ekranında listele)

## 6. Branding

### 6.1 Logo

**Concept:** "OS" harfleri iç içe geçmiş, mold (kalıp) silueti içinde
**Style:** Minimal, monochrome (siyah veya beyaz), 2 versiyon (full + icon)
**Format:** SVG → PNG (512×512 ve 1024×1024)

**Midjourney Prompt:**
```
Minimalist logo for "Ore Sorter" mobile puzzle game, letter O and S
interlocked, with a bottle silhouette inside, flat design, monochrome
black on white, vector style, 1024x1024, no text except logo
```

### 6.2 App Icon

**Size:** 1024×1024 (Google Play)
**Background:** Crystal Mines gradient (mavi-mor)
**Foreground:** Logo, büyük, ortada, beyaz
**Padding:** %10 safe zone (kenar boşluğu)

### 6.3 Splash Screen

**Süre:** 0.5-1.0s
**Content:** Logo (büyük, ortada) + slogan ("Sort the Ores. Master the Forge.")
**Background:** Crystal Mines gradient

## 7. AI Üretim Rehberi (Midjourney/DALL-E)

### 7.1 Stil Tutarlılığı İçin

1. **Base prompt:** Her prompt'a şu ek: `flat design, minimal, vector style, no shadows except 2px drop, isolated on white background, single object`
2. **Color anchor:** Hex kodunu prompt'a ekle (örn. `primary color #E63946`)
3. **Reference iteration:** İlk üretilen 1-2 görseli sonraki prompt'lara `--iw 0.5` ile referans olarak ver
4. **Variation:** Aynı objenin 5-10 varyasyonunu üret, en iyisini seç
5. **Post-process:** Tüm çıktıları Photoshop'ta (veya free alternatif: Photopea) şu işlemler:
   - Beyaz arka planı transparan yap
   - Boyut standardizasyonu (1024×1024 veya belirtilen)
   - 2px drop shadow ekle (yoksa)
   - Renk doğrulama (HEX eşleşme)

### 7.2 Maliyet Tahmini

**Midjourney Standard ($30/ay):** ~2000 image/ay
**İhtiyaç:** ~50 unique asset × 5-10 varyasyon = 500 image
**Yeterlilik:** 1-2 ay yeter

**DALL-E API:** $0.04-0.08 per image
**İhtiyaç:** 500 image × $0.06 = ~$30 one-time

## 8. Dosya Organizasyonu (Unity Asset)

```
Assets/Art/
├── Ore/
│   ├── crimson.png (1024x1024)
│   ├── sapphire.png
│   ├── emerald.png
│   ├── amber.png
│   ├── magma.png
│   ├── gold.png
│   └── lavender.png
├── Mold/
│   ├── classic_glass.png (256x512)
│   ├── crystal_cut.png
│   ├── iron_crucible.png
│   ├── obsidian.png
│   └── trophy.png
├── Cork/
│   ├── light_cork.png (128x128)
│   └── dark_cork.png
├── UI/
│   ├── Button_Primary.png
│   ├── Button_Secondary.png
│   ├── Card_Level.png
│   ├── Star_Empty.png
│   ├── Star_Filled.png
│   └── Coin_Icon.png
├── Background/
│   ├── mines_background.png (1080x1920)
│   └── forge_background.png
├── WorldMap/
│   ├── crystal_mines_card.png
│   ├── volcanic_forge_card.png
│   └── node_empty.png
│   └── node_current.png
│   └── node_completed.png
│   └── node_locked.png
└── Logo/
    ├── logo_full_white.png (512x512)
    ├── logo_full_black.png
    ├── logo_icon.png
    └── app_icon.png (1024x1024)
```

## 9. QA Checklist (Sanat)

- [ ] Tüm ore renkleri HEX eşleşmesi
- [ ] Tüm mold varyantları aynı boy/oran
- [ ] UI elementleri retina uyumlu (4x export)
- [ ] Background gradient mobile'da banding yok
- [ ] Particle efektleri mobile'da 30+ FPS
- [ ] Logo küçük boyutta (24×24) okunabilir
- [ ] App icon tüm Android boyutlarında doğru (mdpi → xxxhdpi)
- [ ] SafeArea tüm UI'da uygulanmış
- [ ] Dark/Light mode (varsa) her iki palette test
