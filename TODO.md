# TODO - Unity Editor “Tam Kontrol” Geliştirmeleri

## Step 1 — Playback / PlayMode çakışmasını düzelt
- [ ] Playback sırasında Play’e geçişte playback’i otomatik durdur
- [ ] Play sırasında (EditorApplication.isPlaying) playback UI/engine aktif olmasın
- [ ] Playback UpdatePlaybackLoop içinde hızlı return + state reset güvenliği
- [ ] “Load” / “Play” / “InitPlayback” akışlarında tutarlı state yönetimi

## Step 2 — Solve/Verify/Reseed/Optimize işlerini geliştir
- [ ] Tek seferde sadece bir uzun işlem çalışsın (reentrancy guard)
- [ ] İptal butonu / iptal bayrağı ekle
- [ ] İş boyunca ForgeEditorWindow status bar’ı güncelle
- [ ] ProgressBar + ClearProgressBar her koşulda garantilensin

## Step 3 — (Sonraki iterasyonlar)
- [ ] Validate aksiyonlarını güçlendir (quick fix/ping)
- [ ] Level load/export “aktif scene yönetimi + Undo grubu standardı” iyileştirme
- [ ] Global hotkey / refresh / tab switching iyileştirmeleri
