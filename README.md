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

- 🎯 **Core Gameplay:** Sıvı sıralama mekaniği, seviye tabanlı puzzle sistemi
- 🔄 **Undo System:** Her hamle için geri alma desteği
- 🎮 **Level Generation:** Seed-based ve zorluk eğrili level üretimi (v2)
- 🌍 **Localization:** Çoklu dil desteği (TR, EN, DE, ES, FR)
- 💰 **Economy System:** Jeton, ipucu, enerji ve reklam entegrasyonu altyapısı
- 📱 **Cross-Platform:** Android ve PC için optimize build pipeline'ları
- 🎨 **Visual Feedback:** Wobble animasyonları, particle efektleri, cork drop
- ♻️ **Object Pooling:** Memory optimizasyonu için particle pool sistemi
- 🎬 **Zero-Allocation Animations:** PrimeTween ile performans optimizasyonu

---

## 🏗️ Mimari Yapı

Proje **Clean Architecture** prensiplerine göre yapılandırılmıştır:

```
PuzzleGame/
├── Domain/                    # Saf iş mantığı (Unity bağımsız)
│   ├── Interfaces/            # Domain sözleşmeleri
│   ├── Models/                # Entity'ler (BottleState, LiquidLayer, DomainColor)
│   └── Services/              # Domain servisleri (LevelGenerator, BottleValidation)
│
├── Application/               # Uygulama katmanı
│   ├── Interfaces/            # Application sözleşmeleri
│   ├── Configuration/         # ScriptableObject yapılandırma dosyaları
│   ├── Events/                # EventAggregator tabanlı mesajlaşma
│   ├── Logging/               # BottleLogger
│   ├── Services/              # İş akışları (AnimationService, AudioService, InputHandlerService)
│   └── UI/                    # UI bileşenleri
│
├── Infrastructure/            # Altyapı katmanı
│   ├── Interfaces/            # Interface sözleşmeleri
│   └── Implementations/       # Interface implementasyonları
│
├── Installers/                # VContainer DI setup (GameInstaller)
├── Editor/                    # Unity Editor araçları (PuzzleGameEditorWindow)
└── Tests/                     # NUnit testleri + Fakes
```

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
