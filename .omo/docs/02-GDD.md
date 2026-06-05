# Ore Sorter — Game Design Document (GDD)

**Versiyon:** 1.0
**Tarih:** 2026-06-05

---

## 1. Core Gameplay

### 1.1 Saniye Başına Döngü

```
Oyuncu görür:
  - N kalıp (mold), her biri 0-4 katman ore ile dolu
  - 1-2 boş kalıp (cork takılı, tıklanınca açılır)
  - Renkli ore'lar karışık sırada

Oyuncu yapar:
  1. Bir dolu kalıba tıkla → "cast" modu aktif
  2. Başka bir kalıba tıkla → ore akışı başlar (animasyonlu)
  3. Cast tamamlanır veya reddedilir (kural ihlali)

Oyun:
  - Win: Tüm kalıplar tek renk (her biri) veya boş
  - Lose: 0 hamle hakkı kalmadı (max hamle = par × 2)
  - Star: Hamle sayısına göre 0-3
```

### 1.2 Mekanik Kuralları

- **Cast Kuralı:** Hedef kalıp boş VEYA üstteki ore rengi kaynak kalıbın üst ore rengiyle AYNI olmalı
- **Multi-layer Cast:** `minConsecutiveForCast: 2` — ardışık aynı renkli katmanlar toplu akabilir
- **Cork (Tıpa):** Boş kalıplarda cork görsel, tıklayınca açılır (0.2s pop animasyonu)
- **Win Condition:** Tüm kalıpların tüm katmanları tek renk VE her kalıp ya dolu (4 layer) ya boş (0 layer)

## 2. Zorluk Sistemi

### 2.1 Difficulty Curve (Lineer Yükseliş)

| Level Aralığı | Difficulty | Mold Sayısı | Renk Sayısı | Boş Mold | Max Hamle |
|---|---|---|---|---|---|
| L01-L10 | 1 (Tutorial) | 4-5 | 2-3 | 1-2 | 4-8 |
| L11-L20 | 2 (Easy) | 5-6 | 3-4 | 1-2 | 8-12 |
| L21-L30 | 3 (Medium) | 6-7 | 4-5 | 2 | 12-16 |
| L31-L40 | 4 (Hard) | 7-8 | 5-6 | 2 | 16-20 |
| L41-L50 | 5 (Expert) | 8-9 | 6-7 | 2-3 | 20-28 |

### 2.2 Star Sistemi (Görünür Hedef)

Her level için:
- **3 yıldız:** ≤ `par` hamle
- **2 yıldız:** ≤ `good` hamle (par × 1.5)
- **1 yıldız:** level tamamlandı
- **0 yıldız:** max hamle aşıldı (lose)

**UI:** Level başında modal: "★★★ ≤4 / ★★ ≤6 / ★ ≤8" (par=4, good=6, max=8 örneği)

### 2.3 Hint Sistemi

- **Tetikleyici:** Hint butonu (max 3/level)
- **Davranış:** OreSortSolver optimal sonraki hamleyi hesaplar, hedef kalıbı 0.5s vurgular (parlak glow)
- **Maliyet:** 15 coin (veya 0 coin ilk hint, +1 rewarded video ile)
- **Cooldown:** Aynı hint tekrar kullanılamaz (farklı hint hesaplanır)

### 2.4 Undo Sistemi

- **Tetikleyici:** Undo butonu (max 5/level)
- **Davranış:** Son hamleyi geri alır (görsel rollback, 0.3s)
- **Maliyet:** 10 coin (veya rewarded video)
- **Kısıt:** Cast başladıysa undo yapılamaz (mid-animation)

## 3. Tutorial (6 Adım)

| Adım | Tetikleyici | UI Mesajı | Aksiyon Beklenen |
|---|---|---|---|
| 1. Welcome | L01 başlar | "Hoş geldin! Bir kalıba tıkla." | Herhangi bir kalıba tıkla |
| 2. Tap to Select | Step 1 tamam | "Şimdi başka bir kalıba tıkla, ore aktar." | Farklı bir kalıba tıkla |
| 3. Tap to Cast | Step 2 tamam | "Ore'lar aynı renkse üst üste binebilir." | Cast tamamla |
| 4. Hint | Step 3 tamam | "Takıldın mı? İpucu için buraya tıkla." | Hint butonuna tıkla |
| 5. Undo | Step 4 tamam | "Yanlış hamle? Geri al!" | Undo butonuna tıkla |
| 6. Complete | Step 5 tamam | "Hedef: tüm kalıpları tek renge sırala." | Level tamamla |

**Persistence:** PlayerPrefs (key: `tutorial_completed` bool). 1 level = true.

## 4. World Map (2 Biome)

### 4.1 Crystal Mines (L01-L25)
- **Atmosfer:** Sakin, berrak, doğal
- **Renk paleti:** Mavi, mor, yeşil (cam, kristal, mineral)
- **Background gradient:** Mavi-mor linear (top: #1a2a6c → bottom: #b8c6db)
- **Müzik:** Ambient, hafif synth pad, 60 BPM
- **Ses efektleri:** Cam, kristal (yüksek pitch chime), su damlası

### 4.2 Volcanic Forge (L26-L50)
- **Atmosfer:** Heyecanlı, sıcak, tehlikeli
- **Renk paleti:** Turuncu, kırmızı, sarı (lava, ateş, metal)
- **Background gradient:** Turuncu-siyah radial (center: #ff6b35 → edge: #1a0e0a)
- **Müzik:** Percussive, orta tempo synth, 90 BPM
- **Ses efektleri:** Metal, kıvılcım (crackle), lav kabarcığı (low rumble)

### 4.3 Map UI
- **Layout:** 2 ayrı biome kartı yan yana (portrait)
- **Her kart:** Biyom görseli + level noktaları (5x5 grid)
- **Node state:** Tamamlanmış = dolu, mevcut = parıltı, kilitli = kilit ikonu
- **Geçiş:** L25 tamamlanınca Volcanic Forge kartı "AÇIK" animasyonu

## 5. Daily Challenge

### 5.1 Üretim Stratejisi (Hibrit)

**7 Curated Master Levels** (elle tasarlanmış, her gün 1 tane):
- L01_baseline → Pazartesi (kolay, giriş)
- L07_balanced → Salı (dengeli)
- L15_stacking → Çarşamba (istif zorluğu)
- L22_color_mix → Perşembe (renk karışımı)
- L30_puzzle → Cuma (kritik düşünce)
- L40_expert → Cumartesi (uzman)
- L50_final → Pazar (final)

**7 Procedural** (seed-based, GenerateSolvable):
- `seed = (utcYear * 10000) + (utcMonth * 100) + utcDay` (gün bazlı deterministik)
- Difficulty = (gun % 5) + 1 (1-5 arası)
- Haftalık rotasyon: Pzt = 1 procedural, Sal = 1 curated, ...

### 5.2 Günlük Akış

```
Oyuncu açılır
  → Daily Login modal (25 coin, "2x için izle")
  → Daily Challenge CTA (kartta, "Bugün: ★★★ ≤7")
  → Challenge tıklanır
  → 1 level (3-5 dk)
  → Win: +50 coin + streak +1
  → Lose: streak -1 (grace period: 1 gün atlanabilir)
```

### 5.3 Streak Sistemi

- **Increment:** Daily challenge tamamlanınca +1
- **Decrement:** Gün atlanırsa (UTC date değişimi) -1
- **Grace:** İlk gün kaçırılırsa streak korunur (24h)
- **Reward:** Her 7 gün = bonus 100 coin

## 6. Economy

### 6.1 Coin Akışı

**Kazanç (Income):**
| Aksiyon | Miktar |
|---|---|
| Level tamamlama | 5 coin (10 rewarded ile 10) |
| 3-yıldız bonus | 5 coin ek (toplam 15) |
| Daily login | 25 coin (50 rewarded ile) |
| Daily challenge | 50 coin |
| Streak 7-gün | 100 coin |
| Rewarded video (interstitial yok, sadece rewarded) | +10 coin veya +1 hint/undo |

**Harcama (Expense):**
| Aksiyon | Miktar |
|---|---|
| Hint (3/level) | 15 coin |
| Undo (5/level) | 10 coin |
| Daily challenge kayıp | 0 (sadece streak kaybı) |

### 6.2 Economy Tuning (Önerilen)

Mevcut `EconomyConfig`:
- startingCoins: 50 → **100** (yeni oyuncu için 5 hint parası)
- hintCost: 15 → **8** (daha erişilebilir)
- undoCost: 10 → **5** (daha sık kullanım)

## 7. Kullanıcı Arayüzü Akışları

### 7.1 Main Menu
```
[Logo "Ore Sorter" + slogan]
  ↓
[Continue] (son level) | [New Game]
  ↓
[Daily Challenge] (badge: 1 challenge available)
  ↓
[Settings] | [Credits]
```

### 7.2 Level Select (World Map)
```
[< Crystal Mines]   [Volcanic Forge >]
  ↓
[● ● ● ● ●]  (5x5 grid, mevcut level parlar)
[● ● ● ● ●]
[...]
```

### 7.3 In-Game HUD
```
[← Back] [L01 / 50] [Hamle: 3 / 8]    [💰 100] [↶ Undo] [💡 Hint] [⏸ Pause]
                                                                  
  ┌──┐  ┌──┐  ┌──┐  ┌──┐  ┌──┐
  │🟦│  │🟥│  │🟩│  │  │  │  │
  │🟦│  │🟥│  │🟩│  │  │  │  │   (5 mold, 2 boş)
  └──┘  └──┘  └──┘  └──┘  └──┘
```

### 7.4 Win Screen
```
  ⭐⭐⭐  (veya ⭐⭐ veya ⭐)
  "Excellent!" (veya "Good" / "Complete")
  
  Hamle: 5 / 8
  Par: 6 hedef
  
  [Replay]  [Next →]  [Watch ad for 2x coins] (rewarded CTA)
```

### 7.5 Lose Screen
```
  💔 "Out of moves!"
  
  Hamle: 8 / 8 (max)
  
  [↶ Undo (×3)]  [💡 Hint (×2)]  [↻ Restart]
```

## 8. Audio Design

### 8.1 SFX Listesi
| Aksiyon | Ses | Süre | Kaynak |
|---|---|---|---|
| Cast başla | Su akışı | 0.8s | Freesound CC0 |
| Cast tamam | Cam çarpma | 0.2s | Freesound CC0 |
| Ore bounce | Yumuşak pop | 0.1s | Freesound CC0 |
| Hint aktif | Kristal chime | 0.5s | Freesound CC0 |
| Undo | Reverse swoosh | 0.4s | Freesound CC0 |
| Win | Fanfare (kısa) | 2s | Freesound CC0 |
| Lose | Minor descent | 1.5s | Freesound CC0 |
| Button click | Subtle tap | 0.1s | Freesound CC0 |
| Star earn | Chime + sparkle | 0.5s | Freesound CC0 |
| Daily reward | Coin collect | 0.3s | Freesound CC0 |

### 8.2 Müzik
- **Main Menu:** Neutral, inviting (Uppbeat free)
- **Crystal Mines:** Calm ambient, 60 BPM (Uppbeat free)
- **Volcanic Forge:** Energetic percussion, 90 BPM (Uppbeat free)
- **Win:** Triumphant, 1 loop (Uppbeat free)

## 9. Test Stratejisi

### 9.1 Unit Tests (NUnit, EditMode)
- Mevcut 28 test (Domain/Application)
- Yeni eklenecekler:
  - HintService 3-level farklı senaryo
  - DailyChallengeService seed determinism (7 gün × 3 cihaz = aynı level)
  - StreakService grace period

### 9.2 PlayMode Tests
- Tutorial 6-adım smoke test
- Level L01 → L02 win flow
- Daily challenge tam akış
- AdMob mock (test ID'ler) integration

### 9.3 Manual QA
- 5 cihaz smoke test (Samsung, Xiaomi, Pixel, OnePlus, Huawei)
- Portrait orientation test
- SafeArea test (notch, punch-hole)
- Low-end device (RAM 4GB) FPS test
