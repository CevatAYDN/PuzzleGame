# Ore Sorter — Product Brief

**Tarih:** 2026-06-05
**Versiyon:** v1.0
**Durum:** Onaylandı (Round 1 + Round 2 kararları)

---

## 🎯 Vizyon (1 Cümle)

> Minimalist flat görsel dille, 50 seviyede saf problem-çözme tatmini sunan, reklamsız invasive olmayan rewarded video monetization'lı premium liquid sort puzzle oyunu.

## 📦 Kapsam (Out)

- ❌ In-app purchase (IAP) — YOK
- ❌ Ücretli 3rd party SDK — YOK (Firebase, AdMob, Sentry free tier)
- ❌ iOS, PC, konsol — sadece Android
- ❌ Multiplayer, leaderboard global — sadece local best

## ✅ Kapsam (In)

- ✅ **Platform:** Android (IL2CPP, ARM64, Vulkan)
- ✅ **İçerik:** 50 level (2 biome: Crystal Mines + Volcanic Forge)
- ✅ **Sanat:** Minimalist flat (AI-generated, royalty-free)
- ✅ **Ses:** AI SFX (ElevenLabs) + royalty-free müzik
- ✅ **Reklam:** AdMob Rewarded Video + Interstitial
- ✅ **Retention:** Daily Challenge (hibrit) + Streak + Daily Login Bonus
- ✅ **Analitik:** Firebase Analytics (consent-gated)
- ✅ **Compliance:** GDPR + COPPA full consent flow

## 💰 Monetizasyon Özeti (Kazanan Oyuncu, Kazanan Biz)

| Aksiyon | Ödül | Tetikleyici |
|---|---|---|
| Level tamamla → 2x coin | +10 coin | Win screen |
| Hint bitti | +1 hint | Hint butonu (limit aşımı) |
| Undo bitti | +1 undo | Undo butonu (limit aşımı) |
| Günlük giriş | 25 → 50 coin | Daily login |
| Interstitial | (reklam) | Her 3 level arası 1 |

## 📊 Başarı Metrikleri (KPI)

| Metrik | Hedef (30 gün) |
|---|---|
| D1 Retention | %40+ |
| D7 Retention | %15+ |
| Avg session length | 8-12 dk |
| Levels/day | 8-12 |
| Rewarded video görüntüleme/gün | 2-3 (kullanıcı başına) |
| Crash-free rate | %99.5+ |
| LTV (30 gün) | $0.05-0.15 (reklam geliri) |

## 🛠️ Teknoloji Yığını

- **Engine:** Unity 6.0 LTS + URP 17.4
- **Dil:** C# 9.0
- **Mimari:** Clean Architecture (Domain/Application/Infrastructure/Composition/Presentation)
- **DI:** VContainer 1.18.0
- **Tween:** PrimeTween 1.4.0
- **Input:** Input System 1.19.0
- **Reklam:** AdMob SDK (Google, ücretsiz)
- **Analytics:** Firebase Analytics
- **Persistence:** PlayerPrefs (local, GDPR-uyumlu)

## 📅 Timeline

- **Hafta 1:** Placeholder art + UI prefabs + L02-L10 elle tasarım
- **Hafta 2:** L11-L25 (Crystal Mines) + MoldController refactor + AdMob SDK entegrasyonu
- **Hafta 3:** L26-L50 (Volcanic Forge) + GDPR consent + Firebase Analytics
- **Hafta 4:** World Map UI + Daily Challenge + Playtest + Bug bash + Store listing

## 🎮 Hedef Kitle

- 18-45 yaş casual mobile gamers
- Liquid sort / puzzle enthusiasts
- 5-15 dk günlük session tercih eden
- AdMob politikalarına uygun coğrafyalar (EU + ABD + TR + Asya)

---

**Detaylar için:** `01-PRD.md`, `02-GDD.md`, `03-ART-BIBLE.md`, `04-MONETIZATION.md`, `05-PRIVACY-COMPLIANCE.md`
