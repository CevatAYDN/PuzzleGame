# Ore Sorter — AI Art Prompt Templates

**Tarih:** 2026-06-05
**Hedef:** Midjourney v6 + DALL-E 3 için production-ready prompt template'leri
**Kullanıcı:** Midjourney/DALL-E hesabınızla bu prompt'ları çalıştırın, çıktıları `Assets/Art/` altına kaydedin

---

## Genel Stil Ayarları (Tüm Prompt'lara Ekle)

```
Style suffix (her prompt sonuna ekle):
"flat design, minimal, vector style, soft drop shadow 2px opacity 10%,
isolated on pure white background, single object, centered,
4K render, no text, no watermark"

Color anchor (her prompt'a ekle):
"primary color: #{HEX_CODE}" (örn. #E63946)

Reference style (Midjourney --sref parametresi):
İlk üretilen başarılı görseli referans al, --iw 0.5 ile
```

---

## 1. Ore Sınıfı (7 Renk)

### 1.1 Crimson Ore (Kırmızı — Crystal Mines ana)
```
A single round crimson ore crystal, flat design, minimal shading,
single object, isolated on white background, vector style, 1024x1024,
primary color: #E63946, no shadows, no text
```

### 1.2 Sapphire Ore (Lacivert)
```
A single round sapphire ore crystal, flat design, minimal shading,
single object, isolated on white background, vector style, 1024x1024,
primary color: #1D3557, no shadows, no text
```

### 1.3 Emerald Ore (Yeşil)
```
A single round emerald ore crystal, flat design, minimal shading,
single object, isolated on white background, vector style, 1024x1024,
primary color: #2A9D8F, no shadows, no text
```

### 1.4 Amber Ore (Turuncu — Volcanic Forge ana)
```
A single round amber ore crystal, flat design, minimal shading,
single object, isolated on white background, vector style, 1024x1024,
primary color: #F4A261, no shadows, no text
```

### 1.5 Magma Ore (Kırmızı-turuncu)
```
A single round magma ore crystal, flat design, minimal shading,
single object, isolated on white background, vector style, 1024x1024,
primary color: #E76F51, no shadows, no text
```

### 1.6 Gold Ore (Altın)
```
A single round gold ore crystal, flat design, minimal shading,
single object, isolated on white background, vector style, 1024x1024,
primary color: #E9C46A, no shadows, no text
```

### 1.7 Lavender Ore (Mor — Uzman)
```
A single round lavender ore crystal, flat design, minimal shading,
single object, isolated on white background, vector style, 1024x1024,
primary color: #B084CC, no shadows, no text
```

**Varyasyon istemi:** Her ore için 5-10 varyasyon üret, en iyisini seç. `--repeat 5` (Midjourney) veya DALL-E için farklı seed'ler.

**Post-process:** Photopea.com (ücretsiz) → transparan arka plan → 1024×1024 → 2px drop shadow (%10 opacity, #000000)

---

## 2. Mold (Kalıp) — 5 Varyant

### 2.1 Classic Glass (Crystal Mines ana)
```
A classic glass crucible bottle, flat design, transparent glass effect,
vertical orientation, 4 layer capacity, single object, isolated on white,
soft highlight on top, no text, vector style, 256x512, primary color: #B8C6DB
```

### 2.2 Crystal Cut (Crystal Mines boss)
```
A faceted crystal-cut crucible, flat design, geometric facets,
vertical orientation, 4 layer capacity, single object, isolated on white,
soft highlight, no text, vector style, 256x512, primary color: #6C9BCF
```

### 2.3 Iron Crucible (Volcanic Forge ana)
```
An iron crucible bottle, flat design, dark metal texture,
vertical orientation, 4 layer capacity, single object, isolated on white,
metallic highlight, no text, vector style, 256x512, primary color: #3D3D3D
```

### 2.4 Obsidian (Volcanic Forge boss)
```
An obsidian crucible, flat design, glossy black volcanic glass,
vertical orientation, 4 layer capacity, single object, isolated on white,
glossy highlight, no text, vector style, 256x512, primary color: #1A0E0A
```

### 2.5 Trophy (Win animation)
```
A golden trophy crucible, flat design, polished gold,
vertical orientation, 4 layer capacity, single object, isolated on white,
bright highlight, sparkle accents, no text, vector style, 256x512,
primary color: #FFD700
```

---

## 3. Cork (Tıpa)

### 3.1 Light Cork (Crystal Mines)
```
A simple light wooden cork stopper, flat design, top-down view,
single object, isolated on white, no text, vector style, 128x128,
primary color: #D4A373
```

### 3.2 Dark Cork (Volcanic Forge)
```
A dark charred wooden cork stopper, flat design, top-down view,
single object, isolated on white, no text, vector style, 128x128,
primary color: #4A2C20
```

---

## 4. UI Components

### 4.1 Button Primary (240×64)
```
A modern UI button, rounded corners 12px, gradient fill, soft drop shadow,
primary color: #2A9D8F, no text, no icon, flat design, vector style,
240x64, isolated on white
```

### 4.2 Button Secondary (240×64)
```
A modern UI button, rounded corners 12px, white fill with 2px border,
primary color: #2A9D8F border, no text, no icon, flat design, vector style,
240x64, isolated on white
```

### 4.3 Card Level (200×200)
```
A level card UI element, rounded corners 16px, 2px border,
white background, no text, no icon, flat design, vector style,
200x200, isolated on white, primary color: #2A9D8F border
```

### 4.4 Star Filled (64×64)
```
A solid gold star icon, flat design, gradient gold fill,
single object, isolated on white, no text, vector style, 64x64,
primary color: #E9C46A
```

### 4.5 Star Empty (64×64)
```
An outline star icon, flat design, gray outline only,
single object, isolated on white, no text, vector style, 64x64,
primary color: #6C757D
```

### 4.6 Coin Icon (48×48)
```
A round gold coin with letter C in center, flat design, gradient gold,
single object, isolated on white, no text except the C, vector style,
48x48, primary color: #FFD700
```

### 4.7 Mold Icon HUD (48×48)
```
A small mold icon for HUD, flat design, outline only,
single object, isolated on white, no text, vector style, 48x48,
primary color: #2A9D8F
```

---

## 5. Background (2 Varyant)

### 5.1 Crystal Mines Background (1080×1920)
```
A vertical gradient background, top color #1A2A6C, bottom color #B8C6DB,
linear gradient top to bottom, minimal style, no objects, no text,
vector style, 1080x1920, soft fog overlay at bottom
```

### 5.2 Volcanic Forge Background (1080×1920)
```
A vertical radial gradient background, center color #FF6B35, edge color #1A0E0A,
radial gradient center to edge, minimal style, no objects, no text,
vector style, 1080x1920, soft ember glow at top
```

---

## 6. Logo

### 6.1 Logo Full (512×512)
```
A minimalist logo for Ore Sorter mobile puzzle game, letters O and S
interlocked, with a bottle silhouette inside the O, flat design,
monochrome black on white, vector style, 512x512, no text except the logo
```

### 6.2 App Icon (1024×1024)
```
A mobile app icon, minimalist, letter O and S interlocked with bottle silhouette,
flat design, gradient background #1A2A6C to #B8C6DB, white logo center,
vector style, 1024x1024, no text, 10% safe zone padding
```

---

## 7. World Map Nodes

### 7.1 Node Empty (64×64)
```
An empty level node icon, flat design, circular outline only, 50% opacity,
single object, isolated on white, no text, vector style, 64x64,
primary color: #6C757D
```

### 7.2 Node Current (64×64)
```
A glowing current level node icon, flat design, circular with glow effect,
single object, isolated on white, no text, vector style, 64x64,
primary color: #FFD700
```

### 7.3 Node Completed (64×64)
```
A completed level node icon, flat design, circular with checkmark,
single object, isolated on white, no text except checkmark, vector style,
64x64, primary color: #2A9D8F
```

### 7.4 Node Locked (64×64)
```
A locked level node icon, flat design, circular with lock symbol,
single object, isolated on white, no text, vector style, 64x64,
primary color: #6C757D, 30% opacity
```

---

## 8. Particle Effects (Frame Sequences)

### 8.1 Cast Splash (8 frames, 256×256)
```
Frame 1/8: A single water droplet, flat design, isolated on white, no text, vector style, 256x256, primary color: #4A90E2
Frame 2/8: Two water droplets merging, flat design, isolated on white, no text, vector style, 256x256, primary color: #4A90E2
...
(Final frame: 4 droplets fanning out)

Production: 8 PNG files, 256x256, transparent background
Usage: Cast completion animation (0.3s, 30fps)
```

### 8.2 Star Burst (6 frames, 512×512)
```
Frame 1/8: A single point of light, flat design, isolated on white, no text, vector style, 512x512, primary color: #FFD700
Frame 2/8: 4-point star burst, ...
...
(Final frame: 16-point star with sparkle particles)
```

### 8.3 Win Confetti (12 frames, 1080×1920)
```
Frame 1/12: Single confetti piece falling, flat design, isolated on transparent background, no text, vector style, 1080x1920, primary colors: #E63946 #1D3557 #2A9D8F #F4A261
...
```

---

## 9. Üretim Workflow (Kullanıcı İçin)

### Adım 1: Midjourney Setup
1. Midjourney Discord'da `/imagine` komutu
2. Prompt template'ini yapıştır
3. İlk 4 varyasyonu al
4. En iyisini upscale (`U1`, `U2`, `U3`, `U4`)
5. Varyasyon istemek için `V1-V4`

### Adım 2: DALL-E Setup
1. ChatGPT Plus veya DALL-E API
2. Aynı prompt'u kullan
3. 4 varyasyon üret
4. En iyisini indir (1024×1024 veya 1024×1792)

### Adım 3: Post-Process
1. **Photopea.com**'u aç (ücretsiz Photoshop alternatifi)
2. Dosyayı aç
3. Magic Wand → beyaz arka planı seç → Delete (transparan yap)
4. Image > Canvas Size: hedef boyutu ayarla
5. Filter > Noise > Add Noise: 0% (smooth)
6. Layer Style > Drop Shadow:
   - Opacity: 10%
   - Distance: 2px
   - Size: 4px
   - Color: #000000

### Adım 4: Unity Import
1. PNG dosyasını `Assets/Art/{Category}/` altına kaydet
2. Unity otomatik import eder
3. Inspector'da:
   - Texture Type: `Sprite (2D and UI)`
   - Sprite Mode: `Single`
   - Pixels Per Unit: 100
   - Compression: None (kalite için) veya ASTC 6x6 (mobile)
4. Apply

---

## 10. Maliyet Tahmini

| Kaynak | Maliyet | Çıktı |
|---|---|---|
| Midjourney Standard ($30/ay) | $30 | ~2000 görsel/ay |
| DALL-E API | ~$0.04/görsel | Pay-as-you-go |
| Photopea (post-process) | Ücretsiz | Web tabanlı |
| Unity 6 (zaten lisanslı) | $0 | — |

**Toplam ihtiyaç:** ~50 unique asset × 5-10 varyasyon = 500 görsel
**Tek seferlik:** ~$30-50 (DALL-E) veya $30/ay (Midjourney)

---

## 11. Varyasyon Öncelik Sırası (Hangi Önce Üretilmeli)

### P0 (Kritik, hemen gerekli)
1. 7 Ore (her renk)
2. 1 Classic Glass Mold
3. 1 Iron Crucible Mold
4. 2 Cork
5. 1 Background (Crystal Mines)
6. 3 Button (Primary, Secondary, Icon)
7. 2 Star (Filled, Empty)
8. 1 Coin Icon

**Toplam: 18 asset, ~1-2 gün üretim**

### P1 (Önemli, Week 1 sonu)
9. 2 Logo (full + app icon)
10. 4 World Map Node
11. 1 Card Level
12. 1 Background (Volcanic Forge)
13. 4 Mold varyant (Crystal Cut, Obsidian, Trophy + 1 backup)

**Toplam: 12 asset, ~2 gün üretim**

### P2 (Polish, Week 2-3)
14. Particle effects (cast splash, star burst, confetti)
15. UI polish (modal cards, sliders)
16. Biome-specific ambient elements (crystals, embers)

**Toplam: 10-15 asset, ~3 gün üretim**

---

## 12. Stil Tutarlılığı Kontrol Listesi

Üretim sonrası her asset için:

- [ ] Aynı yumuşaklık (sharpness < 50%)
- [ ] Tutarlı gölge yönü (üst sol, 45°)
- [ ] HEX renk eşleşmesi (eyedropper ile)
- [ ] Transparan arka plan (checker pattern testi)
- [ ] 2px drop shadow (%10 opacity)
- [ ] 4K master + 1024×1024 export
- [ ] Sprite atlas uyumluluğu (eğer kullanılacaksa)

---

## 13. Kullanım Kılavuzu

1. **Bu dosyayı aç** → İstediğin kategoriye git
2. **Prompt'u kopyala** → Midjourney/DALL-E'ye yapıştır
3. **Varyasyonları üret** → En iyisini seç
4. **Post-process** → Photopea ile transparan + drop shadow
5. **Unity'ye import** → `Assets/Art/{Category}/`
6. **Inspector ayarla** → Sprite (2D and UI), doğru boyut
7. **Test et** → Game view'da görsel kontrol

---

**Toplam üretim süresi (1 kişi): ~5-7 gün (yoğun)**
**Toplam üretim süresi (2 kişi paralel): ~3-4 gün**

Üretim tamamlandığında `Assets/Art/` altında organize edilmiş ~50 sprite dosyası olacak.
