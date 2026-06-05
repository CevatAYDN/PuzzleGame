# 🧩 PuzzleGame — Clean Architecture Liquid Sort Puzzle

Unity tabanlı, **Clean Architecture** ve **SOLID** prensiplerine uygun olarak geliştirilmiş sıvı sıralama bulmaca oyunu.

---

## 📋 İçindekiler

- [Özellikler](#-özellikler)
- [Mimari Yapı](#-mimari-yapı)
- [Kurulum](#-kurulum)
- [Katkıda Bulunma](#-katkıda-bulunma)
- [Lisans](#-lisans)

---

## ✨ Özellikler

- 🎯 **Core Gameplay:** Cevher sıralama mekaniği, seviye tabanlı puzzle sistemi
- 🔄 **Undo + Hint System:** Coin-gated undo ve hint (OreSortSolver ile)
- 🧠 **Solvability Guarantee:** `DifficultyBasedLevelGenerator.GenerateSolvable` retry'lar ile çözülebilir level üretir
- 🌍 **Localization:** 5 dilde 50+ anahtar (TR, EN, DE, ES, FR)
- 💰 **Economy System:** CoinWallet (PlayerPrefs persistent), HintService, UndoService, DailyChallengeService, StreakService
- 📱 **Cross-Platform:** Android (IL2CPP) + PC (x64) build pipeline'ları
- 🎨 **Visual Feedback:** Wobble animasyonları, particle efektleri, cork drop
- 📳 **Haptic Feedback:** Mobile-native (Android Vibrator / iOS UIImpactFeedback) haptics
- ♻️ **Object Pooling:** Memory optimizasyonu için particle pool sistemi
- 🎬 **Zero-Allocation Animations:** PrimeTween ile performans optimizasyonu
- 📊 **Analytics Hook:** NoOp default, Firebase/Amplitude/Unity Analytics adapter'ı drop-in
- 🎓 **Tutorial Service:** Otomatik ilerleyen step-by-step onboarding
- 🎨 **Centralized UI Style:** `UIStyleConfig` ScriptableObject (renkler, fontlar, boyutlar)

---

## 🏗️ Mimari Yapı

Proje **Clean Architecture** prensiplerine göre yapılandırılmıştır:

```
PuzzleGame/
├── Domain/                    # Saf iş mantığı (Unity bağımsız)
│   ├── Interfaces/            # Domain sözleşmeleri
│   ├── Models/                # Entity'ler (MoldState, OreLayer, DomainColor)
│   └── Services/              # Domain servisleri (LevelGenerator, MoldValidation, OreSortSolver)
│
├── Application/               # Uygulama katmanı
│   ├── Interfaces/            # Application sözleşmeleri
│   ├── Configuration/         # ScriptableObject yapılandırma dosyaları
│   ├── Events/                # EventAggregator tabanlı mesajlaşma
│   ├── Logging/               # MoldLogger
│   ├── Services/              # İş akışları (CastService, HintService, CoinWallet, TutorialService, ...)
│   └── UI/                    # UI bileşenleri (HudPresenter, DebugOverlayUI)
│
├── Infrastructure/            # Altyapı katmanı
│   ├── Interfaces/            # Interface sözleşmeleri
│   └── Implementations/       # Interface implementasyonları
│
├── Presentation/              # Unity MonoBehaviour'lar ve scene-level controllers
│   ├── GameManager.cs         # DI failure handler, FPS, audio boot, scene cleanup
│   ├── LevelFlowController.cs # Level load lifecycle (separation from GameManager)
│   ├── WinLoseEvaluator.cs    # Win/lose detection (separation from GameManager)
│   ├── MoldController.cs      # MonoBehaviour IMoldView impl
│   └── MoldPoolInitializer.cs # IActiveMoldsProvider — molds pool
│
├── Installers/                # VContainer DI setup (GameInstaller : LifetimeScope)
├── Editor/                    # Unity Editor araçları (PuzzleGameEditorWindow)
└── Tests/                     # NUnit testleri + Fakes
```

## 🎮 Terminoloji

| Eski (README v1) | Yeni (güncel kod) | Anlamı |
|---|---|---|
| Bottle | **Mold** | Kalıp (cam kap) |
| Pour | **Cast** | Döküm işlemi |
| Liquid | **Ore** | Cevher (renkli madde) |
| Cap | **Cork** | Tıpa |
| Container | **Crucible** | Ergitme kabı |

### 🧱 SOLID Prensipleri Uygulaması

- **S (Single Responsibility):** Her servis tek bir sorumluluğa sahip
- **O (Open/Closed):** Interface'ler ile genişletilebilir yapı
- **L (Liskov Substitution):** `ITweenService` (PrimeTween/Coroutine) değişebilir
- **I (Interface Segregation):** Küçük, özel interface'ler
- **D (Dependency Inversion):** VContainer DI container ile bağımlılık enjeksiyonu

---

## 🚀 Kurulum

### Gereksinimler

- **Unity 6.0+** (URP 17.4 ile)
- **.NET Framework 4.7.2+** veya **.NET 6+**
- **Git** (version control için)

### Adımlar

1. **Depoyu Klonla:**
   ```bash
   git clone https://github.com/your-org/PuzzleGame.git
   cd PuzzleGame
   ```

2. **Unity ile Aç:**
   - Unity Hub'da "Add" ile projeyi seç
   - Unity 6.0+ veya daha yeni bir sürüm kullan

3. **Paketleri Yükle:**
   - VContainer ve diğer bağımlılıklar `manifest.json`'da tanımlı
   - Unity Editor otomatik olarak restore edecektir

4. **Derle & Çalıştır:**
   - `File > Build Settings` -> Hedef platformu seç
   - `PuzzleGame > Builds` menüsünden hızlı build yapabilirsin

### Testleri Çalıştır

```bash
# Unity Test Runner ile
# Window > General > Test Runner
```

---

## 🧪 Test

- **Domain Tests:** `Assets/Tests/Domain/` altında
- **Application Tests:** `Assets/Tests/Application/` altında
- **Infrastructure Tests:** `Assets/Tests/Infrastructure/` altında

Test framework olarak **NUnit** kullanılmaktadır. Mock ihtiyacı için elle yazılmış `Fake*` sınıfları kullanılır (`Assets/Tests/Fakes/`).

---

## 🛠️ Geliştirme

### Yeni Servis Ekleme

1. Domain katmanında interface tanımla: `Domain/Interfaces/INewService.cs`
2. Application katmanında implement et: `Application/Services/NewService.cs`
3. VContainer installer'a kaydet: `Installers/GameInstaller.cs`

### Yeni Level Ekleme

1. `Assets/Resources/Data/` altında yeni `LevelData` ScriptableObject oluştur
2. Level properties'ini ayarla (color count, difficulty, seed vb.)
3. `levelCatalog` dizisine ekle

### Yeni Dil Ekleme

1. `Domain/Models/LocalizationEntry.cs` enum'ına ekle
2. `Domain/Services/LocalizationService.cs` içinde default translation'ları ekle
3. UI elementlerinde `ILocalizationService.GetString("key")` kullan

---

## 📱 Cross-Platform Build

### Android (IL2CPP + Vulkan)
```
PuzzleGame > Builds > Build Android (Release)
```

### PC (Windows x64)
```
PuzzleGame > Builds > Build PC (Windows x64)
```

Build output: `Builds/Android/` ve `Builds/PC/`

---

## 🤝 Katkıda Bulunma

1. Bu depoyu fork'la
2. Feature branch oluştur: `git checkout -b feature/amazing-feature`
3. Değişikliklerini commit et: `git commit -m 'feat: add amazing feature'`
4. Branch'i push et: `git push origin feature/amazing-feature`
5. Pull Request aç

---

## 📝 Lisans

Bu proje [MIT Lisansı](LICENSE) altında dağıtılmaktadır.

---

## 📞 İletişim

Sorularınız veya önerileriniz için: [proje-ekibi@email.com](mailto:proje-ekibi@email.com)

---

**Clean Code & SOLID prensiplerine uygun geliştirme yapmaya özen gösteriyoruz.** 🚀
