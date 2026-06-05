using System;
using System.Collections.Generic;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain.Interfaces;

namespace PuzzleGame.Domain.Services
{
    /// <summary>
    /// Basit localization servisi.
    /// Dictionary tabanlı, temiz ve test edilebilir.
    ///
    /// Fix #17: Accepts an optional ITranslationProvider for data-driven translations.
    /// When no provider is supplied, falls back to the built-in hardcoded strings
    /// (backward compatible). To add new languages, inject a custom ITranslationProvider.
    /// </summary>
    public class LocalizationService : ILocalizationService
    {
        private readonly Dictionary<string, Dictionary<SupportedLanguage, string>> _translations = new();
        private SupportedLanguage _currentLanguage;

        public SupportedLanguage CurrentLanguage
        {
            get => _currentLanguage;
            set => SetLanguage(value);
        }

        /// <param name="defaultLanguage">Initial language.</param>
        /// <param name="provider">
        /// Optional translation provider. If null, built-in hardcoded strings are used.
        /// Injecting a provider (e.g. JSON file loader) follows OCP — no code change needed
        /// to add new languages.
        /// </param>
        public LocalizationService(
            SupportedLanguage defaultLanguage = SupportedLanguage.Turkish,
            ITranslationProvider provider = null)
        {
            _currentLanguage = defaultLanguage;

            if (provider != null)
            {
                var data = provider.Load();
                foreach (var kvp in data)
                {
                    _translations[kvp.Key] = new Dictionary<SupportedLanguage, string>(kvp.Value);
                }
            }
            else
            {
                LoadDefaultTranslations();
            }
        }

        public string GetString(string key)
        {
            if (_translations.TryGetValue(key, out var languageMap) &&
                languageMap.TryGetValue(_currentLanguage, out var value))
            {
                return value;
            }

            // Fallback to English
            if (languageMap != null && languageMap.TryGetValue(SupportedLanguage.English, out var fallback))
            {
                return fallback;
            }

            return key; // Return key as fallback
        }

        public void SetLanguage(SupportedLanguage language)
        {
            if (_currentLanguage != language)
            {
                _currentLanguage = language;
            }
        }

        public void AddTranslation(string key, SupportedLanguage language, string value)
        {
            if (!_translations.ContainsKey(key))
            {
                _translations[key] = new Dictionary<SupportedLanguage, string>();
            }
            _translations[key][language] = value;
        }

        private void LoadDefaultTranslations()
        {
            // Game flow
            AddTranslation("moves_text", SupportedLanguage.Turkish, "Hamle");
            AddTranslation("level_complete", SupportedLanguage.Turkish, "Seviye Tamamlandı!");
            AddTranslation("undo", SupportedLanguage.Turkish, "Geri Al");
            AddTranslation("restart", SupportedLanguage.Turkish, "Yeniden Başlat");
            AddTranslation("menu", SupportedLanguage.Turkish, "Ana Menü");
            AddTranslation("pause", SupportedLanguage.Turkish, "Duraklat");
            AddTranslation("resume", SupportedLanguage.Turkish, "Devam Et");
            AddTranslation("next_level", SupportedLanguage.Turkish, "Sonraki Seviye");
            AddTranslation("replay", SupportedLanguage.Turkish, "Tekrar Oyna");
            AddTranslation("play", SupportedLanguage.Turkish, "Oyna");
            AddTranslation("hint", SupportedLanguage.Turkish, "İpucu");
            AddTranslation("settings", SupportedLanguage.Turkish, "Ayarlar");
            AddTranslation("quit", SupportedLanguage.Turkish, "Çıkış");
            AddTranslation("level_select", SupportedLanguage.Turkish, "Seviye Seç");
            AddTranslation("loading", SupportedLanguage.Turkish, "Yükleniyor...");
            AddTranslation("ready", SupportedLanguage.Turkish, "Hazır!");

            // Economy
            AddTranslation("coins", SupportedLanguage.Turkish, "Jeton");
            AddTranslation("shop", SupportedLanguage.Turkish, "Mağaza");
            AddTranslation("buy_hint", SupportedLanguage.Turkish, "İpucu Al");
            AddTranslation("buy_undo", SupportedLanguage.Turkish, "Geri Al Hakkı Al");
            AddTranslation("insufficient_coins", SupportedLanguage.Turkish, "Yetersiz jeton. Bir tane daha kazanmak için bir reklam izleyin.");
            AddTranslation("watch_ad", SupportedLanguage.Turkish, "Reklam İzle");
            AddTranslation("reward_earned", SupportedLanguage.Turkish, "{0} jeton kazandınız!");

            // Stars and feedback
            AddTranslation("stars", SupportedLanguage.Turkish, "Yıldız");
            AddTranslation("best_score", SupportedLanguage.Turkish, "En İyi");
            AddTranslation("total_stars", SupportedLanguage.Turkish, "Toplam Yıldız");
            AddTranslation("level_number", SupportedLanguage.Turkish, "Seviye {0}");
            AddTranslation("level_failed", SupportedLanguage.Turkish, "Başarısız — Tekrar Dene");
            AddTranslation("try_again", SupportedLanguage.Turkish, "Tekrar Dene");
            AddTranslation("perfect", SupportedLanguage.Turkish, "Mükemmel!");
            AddTranslation("congratulations", SupportedLanguage.Turkish, "Tebrikler!");
            AddTranslation("new_best", SupportedLanguage.Turkish, "Yeni En İyi!");

            // Daily
            AddTranslation("daily_challenge", SupportedLanguage.Turkish, "Günlük Bulmaca");
            AddTranslation("streak", SupportedLanguage.Turkish, "Seri: {0} gün");
            AddTranslation("day", SupportedLanguage.Turkish, "Gün");

            // Tutorial
            AddTranslation("tutorial_welcome", SupportedLanguage.Turkish, "Hoş geldin! Hadi başlayalım.");
            AddTranslation("tutorial_tap_to_select", SupportedLanguage.Turkish, "Bir kalıba dokun");
            AddTranslation("tutorial_tap_to_cast", SupportedLanguage.Turkish, "Hedef kalıba dokunarak dökmeyi başlat");
            AddTranslation("tutorial_drag_to_pour", SupportedLanguage.Turkish, "Sürükle ve bırak — sıvı akar");
            AddTranslation("tutorial_well_done", SupportedLanguage.Turkish, "Harika iş! Bir tane daha dene.");
            AddTranslation("tutorial_skip", SupportedLanguage.Turkish, "Atla");

            // Settings
            AddTranslation("language", SupportedLanguage.Turkish, "Dil");
            AddTranslation("sound_effects", SupportedLanguage.Turkish, "Ses Efektleri");
            AddTranslation("music", SupportedLanguage.Turkish, "Müzik");
            AddTranslation("vibration", SupportedLanguage.Turkish, "Titreşim");
            AddTranslation("colorblind_mode", SupportedLanguage.Turkish, "Renk Körlüğü Modu");
            AddTranslation("font_size", SupportedLanguage.Turkish, "Yazı Boyutu");
            AddTranslation("reduced_motion", SupportedLanguage.Turkish, "Azaltılmış Hareket");
            AddTranslation("reset_progress", SupportedLanguage.Turkish, "İlerlemeyi Sıfırla");
            AddTranslation("reset_confirm", SupportedLanguage.Turkish, "Tüm ilerlemen silinecek. Emin misin?");

            // Errors
            AddTranslation("error_level_not_found", SupportedLanguage.Turkish, "Seviye bulunamadı.");
            AddTranslation("error_connection", SupportedLanguage.Turkish, "Bağlantı hatası. Tekrar deneyin.");
            AddTranslation("error_save_failed", SupportedLanguage.Turkish, "Kayıt başarısız. Tekrar deneyin.");

            // English
            AddTranslation("moves_text", SupportedLanguage.English, "Moves");
            AddTranslation("level_complete", SupportedLanguage.English, "Level Complete!");
            AddTranslation("undo", SupportedLanguage.English, "Undo");
            AddTranslation("restart", SupportedLanguage.English, "Restart");
            AddTranslation("menu", SupportedLanguage.English, "Main Menu");
            AddTranslation("pause", SupportedLanguage.English, "Pause");
            AddTranslation("resume", SupportedLanguage.English, "Resume");
            AddTranslation("next_level", SupportedLanguage.English, "Next Level");
            AddTranslation("replay", SupportedLanguage.English, "Replay");
            AddTranslation("play", SupportedLanguage.English, "Play");
            AddTranslation("hint", SupportedLanguage.English, "Hint");
            AddTranslation("settings", SupportedLanguage.English, "Settings");
            AddTranslation("quit", SupportedLanguage.English, "Quit");
            AddTranslation("level_select", SupportedLanguage.English, "Level Select");
            AddTranslation("loading", SupportedLanguage.English, "Loading...");
            AddTranslation("ready", SupportedLanguage.English, "Ready!");
            AddTranslation("coins", SupportedLanguage.English, "Coins");
            AddTranslation("shop", SupportedLanguage.English, "Shop");
            AddTranslation("buy_hint", SupportedLanguage.English, "Buy Hint");
            AddTranslation("buy_undo", SupportedLanguage.English, "Buy Undo");
            AddTranslation("insufficient_coins", SupportedLanguage.English, "Not enough coins. Watch an ad to earn one more.");
            AddTranslation("watch_ad", SupportedLanguage.English, "Watch Ad");
            AddTranslation("reward_earned", SupportedLanguage.English, "You earned {0} coins!");
            AddTranslation("stars", SupportedLanguage.English, "Stars");
            AddTranslation("best_score", SupportedLanguage.English, "Best");
            AddTranslation("total_stars", SupportedLanguage.English, "Total Stars");
            AddTranslation("level_number", SupportedLanguage.English, "Level {0}");
            AddTranslation("level_failed", SupportedLanguage.English, "Failed — Try Again");
            AddTranslation("try_again", SupportedLanguage.English, "Try Again");
            AddTranslation("perfect", SupportedLanguage.English, "Perfect!");
            AddTranslation("congratulations", SupportedLanguage.English, "Congratulations!");
            AddTranslation("new_best", SupportedLanguage.English, "New Best!");
            AddTranslation("daily_challenge", SupportedLanguage.English, "Daily Challenge");
            AddTranslation("streak", SupportedLanguage.English, "Streak: {0} days");
            AddTranslation("day", SupportedLanguage.English, "Day");
            AddTranslation("tutorial_welcome", SupportedLanguage.English, "Welcome! Let's get started.");
            AddTranslation("tutorial_tap_to_select", SupportedLanguage.English, "Tap a mold to select it");
            AddTranslation("tutorial_tap_to_cast", SupportedLanguage.English, "Tap a target mold to start casting");
            AddTranslation("tutorial_drag_to_pour", SupportedLanguage.English, "Drag and drop — the ore flows");
            AddTranslation("tutorial_well_done", SupportedLanguage.English, "Great work! Try one more.");
            AddTranslation("tutorial_skip", SupportedLanguage.English, "Skip");
            AddTranslation("language", SupportedLanguage.English, "Language");
            AddTranslation("sound_effects", SupportedLanguage.English, "Sound Effects");
            AddTranslation("music", SupportedLanguage.English, "Music");
            AddTranslation("vibration", SupportedLanguage.English, "Vibration");
            AddTranslation("colorblind_mode", SupportedLanguage.English, "Colorblind Mode");
            AddTranslation("font_size", SupportedLanguage.English, "Font Size");
            AddTranslation("reduced_motion", SupportedLanguage.English, "Reduced Motion");
            AddTranslation("reset_progress", SupportedLanguage.English, "Reset Progress");
            AddTranslation("reset_confirm", SupportedLanguage.English, "All progress will be erased. Are you sure?");
            AddTranslation("error_level_not_found", SupportedLanguage.English, "Level not found.");
            AddTranslation("error_connection", SupportedLanguage.English, "Connection error. Please try again.");
            AddTranslation("error_save_failed", SupportedLanguage.English, "Save failed. Please try again.");

            // German
            AddTranslation("moves_text", SupportedLanguage.German, "Züge");
            AddTranslation("level_complete", SupportedLanguage.German, "Level geschafft!");
            AddTranslation("undo", SupportedLanguage.German, "Rückgängig");
            AddTranslation("restart", SupportedLanguage.German, "Neustart");
            AddTranslation("menu", SupportedLanguage.German, "Hauptmenü");
            AddTranslation("pause", SupportedLanguage.German, "Pause");
            AddTranslation("resume", SupportedLanguage.German, "Fortsetzen");
            AddTranslation("next_level", SupportedLanguage.German, "Nächstes Level");
            AddTranslation("replay", SupportedLanguage.German, "Wiederholen");
            AddTranslation("play", SupportedLanguage.German, "Spielen");
            AddTranslation("hint", SupportedLanguage.German, "Tipp");
            AddTranslation("settings", SupportedLanguage.German, "Einstellungen");
            AddTranslation("quit", SupportedLanguage.German, "Beenden");
            AddTranslation("level_select", SupportedLanguage.German, "Levelauswahl");
            AddTranslation("loading", SupportedLanguage.German, "Lädt...");
            AddTranslation("ready", SupportedLanguage.German, "Bereit!");
            AddTranslation("coins", SupportedLanguage.German, "Münzen");
            AddTranslation("shop", SupportedLanguage.German, "Shop");
            AddTranslation("buy_hint", SupportedLanguage.German, "Tipp kaufen");
            AddTranslation("buy_undo", SupportedLanguage.German, "Rückgängig kaufen");
            AddTranslation("insufficient_coins", SupportedLanguage.German, "Nicht genug Münzen. Sieh eine Werbung, um eine zu verdienen.");
            AddTranslation("watch_ad", SupportedLanguage.German, "Werbung ansehen");
            AddTranslation("reward_earned", SupportedLanguage.German, "Du hast {0} Münzen verdient!");
            AddTranslation("stars", SupportedLanguage.German, "Sterne");
            AddTranslation("best_score", SupportedLanguage.German, "Beste");
            AddTranslation("total_stars", SupportedLanguage.German, "Sterne gesamt");
            AddTranslation("level_number", SupportedLanguage.German, "Level {0}");
            AddTranslation("level_failed", SupportedLanguage.German, "Fehlgeschlagen — Nochmal");
            AddTranslation("try_again", SupportedLanguage.German, "Nochmal versuchen");
            AddTranslation("perfect", SupportedLanguage.German, "Perfekt!");
            AddTranslation("congratulations", SupportedLanguage.German, "Glückwunsch!");
            AddTranslation("new_best", SupportedLanguage.German, "Neuer Rekord!");
            AddTranslation("daily_challenge", SupportedLanguage.German, "Tägliche Herausforderung");
            AddTranslation("streak", SupportedLanguage.German, "Serie: {0} Tage");
            AddTranslation("day", SupportedLanguage.German, "Tag");
            AddTranslation("tutorial_welcome", SupportedLanguage.German, "Willkommen! Lass uns anfangen.");
            AddTranslation("tutorial_tap_to_select", SupportedLanguage.German, "Tippe auf eine Form");
            AddTranslation("tutorial_tap_to_cast", SupportedLanguage.German, "Tippe auf die Zielform, um zu gießen");
            AddTranslation("tutorial_drag_to_pour", SupportedLanguage.German, "Ziehen und ablegen — das Erz fließt");
            AddTranslation("tutorial_well_done", SupportedLanguage.German, "Gut gemacht! Noch eins.");
            AddTranslation("tutorial_skip", SupportedLanguage.German, "Überspringen");
            AddTranslation("language", SupportedLanguage.German, "Sprache");
            AddTranslation("sound_effects", SupportedLanguage.German, "Soundeffekte");
            AddTranslation("music", SupportedLanguage.German, "Musik");
            AddTranslation("vibration", SupportedLanguage.German, "Vibration");
            AddTranslation("colorblind_mode", SupportedLanguage.German, "Farbenblindmodus");
            AddTranslation("font_size", SupportedLanguage.German, "Schriftgröße");
            AddTranslation("reduced_motion", SupportedLanguage.German, "Reduzierte Bewegung");
            AddTranslation("reset_progress", SupportedLanguage.German, "Fortschritt zurücksetzen");
            AddTranslation("reset_confirm", SupportedLanguage.German, "Der gesamte Fortschritt wird gelöscht. Sicher?");
            AddTranslation("error_level_not_found", SupportedLanguage.German, "Level nicht gefunden.");
            AddTranslation("error_connection", SupportedLanguage.German, "Verbindungsfehler. Bitte erneut versuchen.");
            AddTranslation("error_save_failed", SupportedLanguage.German, "Speichern fehlgeschlagen. Bitte erneut versuchen.");

            // Spanish
            AddTranslation("moves_text", SupportedLanguage.Spanish, "Movimientos");
            AddTranslation("level_complete", SupportedLanguage.Spanish, "¡Nivel completado!");
            AddTranslation("undo", SupportedLanguage.Spanish, "Deshacer");
            AddTranslation("restart", SupportedLanguage.Spanish, "Reiniciar");
            AddTranslation("menu", SupportedLanguage.Spanish, "Menú principal");
            AddTranslation("pause", SupportedLanguage.Spanish, "Pausar");
            AddTranslation("resume", SupportedLanguage.Spanish, "Reanudar");
            AddTranslation("next_level", SupportedLanguage.Spanish, "Siguiente nivel");
            AddTranslation("replay", SupportedLanguage.Spanish, "Repetir");
            AddTranslation("play", SupportedLanguage.Spanish, "Jugar");
            AddTranslation("hint", SupportedLanguage.Spanish, "Pista");
            AddTranslation("settings", SupportedLanguage.Spanish, "Ajustes");
            AddTranslation("quit", SupportedLanguage.Spanish, "Salir");
            AddTranslation("level_select", SupportedLanguage.Spanish, "Selección de nivel");
            AddTranslation("loading", SupportedLanguage.Spanish, "Cargando...");
            AddTranslation("ready", SupportedLanguage.Spanish, "¡Listo!");
            AddTranslation("coins", SupportedLanguage.Spanish, "Monedas");
            AddTranslation("shop", SupportedLanguage.Spanish, "Tienda");
            AddTranslation("buy_hint", SupportedLanguage.Spanish, "Comprar pista");
            AddTranslation("buy_undo", SupportedLanguage.Spanish, "Comprar deshacer");
            AddTranslation("insufficient_coins", SupportedLanguage.Spanish, "No tienes suficientes monedas. Mira un anuncio para ganar una.");
            AddTranslation("watch_ad", SupportedLanguage.Spanish, "Ver anuncio");
            AddTranslation("reward_earned", SupportedLanguage.Spanish, "¡Has ganado {0} monedas!");
            AddTranslation("stars", SupportedLanguage.Spanish, "Estrellas");
            AddTranslation("best_score", SupportedLanguage.Spanish, "Mejor");
            AddTranslation("total_stars", SupportedLanguage.Spanish, "Estrellas totales");
            AddTranslation("level_number", SupportedLanguage.Spanish, "Nivel {0}");
            AddTranslation("level_failed", SupportedLanguage.Spanish, "Fallido — Inténtalo de nuevo");
            AddTranslation("try_again", SupportedLanguage.Spanish, "Reintentar");
            AddTranslation("perfect", SupportedLanguage.Spanish, "¡Perfecto!");
            AddTranslation("congratulations", SupportedLanguage.Spanish, "¡Felicidades!");
            AddTranslation("new_best", SupportedLanguage.Spanish, "¡Nuevo récord!");
            AddTranslation("daily_challenge", SupportedLanguage.Spanish, "Reto diario");
            AddTranslation("streak", SupportedLanguage.Spanish, "Racha: {0} días");
            AddTranslation("day", SupportedLanguage.Spanish, "Día");
            AddTranslation("tutorial_welcome", SupportedLanguage.Spanish, "¡Bienvenido! Empecemos.");
            AddTranslation("tutorial_tap_to_select", SupportedLanguage.Spanish, "Toca un molde para seleccionarlo");
            AddTranslation("tutorial_tap_to_cast", SupportedLanguage.Spanish, "Toca el molde destino para empezar");
            AddTranslation("tutorial_drag_to_pour", SupportedLanguage.Spanish, "Arrastra y suelta — el mineral fluye");
            AddTranslation("tutorial_well_done", SupportedLanguage.Spanish, "¡Buen trabajo! Prueba otro.");
            AddTranslation("tutorial_skip", SupportedLanguage.Spanish, "Saltar");
            AddTranslation("language", SupportedLanguage.Spanish, "Idioma");
            AddTranslation("sound_effects", SupportedLanguage.Spanish, "Efectos de sonido");
            AddTranslation("music", SupportedLanguage.Spanish, "Música");
            AddTranslation("vibration", SupportedLanguage.Spanish, "Vibración");
            AddTranslation("colorblind_mode", SupportedLanguage.Spanish, "Modo daltónico");
            AddTranslation("font_size", SupportedLanguage.Spanish, "Tamaño de fuente");
            AddTranslation("reduced_motion", SupportedLanguage.Spanish, "Movimiento reducido");
            AddTranslation("reset_progress", SupportedLanguage.Spanish, "Reiniciar progreso");
            AddTranslation("reset_confirm", SupportedLanguage.Spanish, "Se borrará todo el progreso. ¿Estás seguro?");
            AddTranslation("error_level_not_found", SupportedLanguage.Spanish, "Nivel no encontrado.");
            AddTranslation("error_connection", SupportedLanguage.Spanish, "Error de conexión. Inténtalo de nuevo.");
            AddTranslation("error_save_failed", SupportedLanguage.Spanish, "Error al guardar. Inténtalo de nuevo.");

            // French
            AddTranslation("moves_text", SupportedLanguage.French, "Coups");
            AddTranslation("level_complete", SupportedLanguage.French, "Niveau terminé !");
            AddTranslation("undo", SupportedLanguage.French, "Annuler");
            AddTranslation("restart", SupportedLanguage.French, "Redémarrer");
            AddTranslation("menu", SupportedLanguage.French, "Menu principal");
            AddTranslation("pause", SupportedLanguage.French, "Pause");
            AddTranslation("resume", SupportedLanguage.French, "Reprendre");
            AddTranslation("next_level", SupportedLanguage.French, "Niveau suivant");
            AddTranslation("replay", SupportedLanguage.French, "Rejouer");
            AddTranslation("play", SupportedLanguage.French, "Jouer");
            AddTranslation("hint", SupportedLanguage.French, "Indice");
            AddTranslation("settings", SupportedLanguage.French, "Paramètres");
            AddTranslation("quit", SupportedLanguage.French, "Quitter");
            AddTranslation("level_select", SupportedLanguage.French, "Sélection du niveau");
            AddTranslation("loading", SupportedLanguage.French, "Chargement...");
            AddTranslation("ready", SupportedLanguage.French, "Prêt !");
            AddTranslation("coins", SupportedLanguage.French, "Pièces");
            AddTranslation("shop", SupportedLanguage.French, "Boutique");
            AddTranslation("buy_hint", SupportedLanguage.French, "Acheter un indice");
            AddTranslation("buy_undo", SupportedLanguage.French, "Acheter un annuler");
            AddTranslation("insufficient_coins", SupportedLanguage.French, "Pas assez de pièces. Regardez une pub pour en gagner.");
            AddTranslation("watch_ad", SupportedLanguage.French, "Regarder la pub");
            AddTranslation("reward_earned", SupportedLanguage.French, "Vous avez gagné {0} pièces !");
            AddTranslation("stars", SupportedLanguage.French, "Étoiles");
            AddTranslation("best_score", SupportedLanguage.French, "Meilleur");
            AddTranslation("total_stars", SupportedLanguage.French, "Étoiles totales");
            AddTranslation("level_number", SupportedLanguage.French, "Niveau {0}");
            AddTranslation("level_failed", SupportedLanguage.French, "Échec — Réessayez");
            AddTranslation("try_again", SupportedLanguage.French, "Réessayer");
            AddTranslation("perfect", SupportedLanguage.French, "Parfait !");
            AddTranslation("congratulations", SupportedLanguage.French, "Félicitations !");
            AddTranslation("new_best", SupportedLanguage.French, "Nouveau record !");
            AddTranslation("daily_challenge", SupportedLanguage.French, "Défi du jour");
            AddTranslation("streak", SupportedLanguage.French, "Série : {0} jours");
            AddTranslation("day", SupportedLanguage.French, "Jour");
            AddTranslation("tutorial_welcome", SupportedLanguage.French, "Bienvenue ! Commençons.");
            AddTranslation("tutorial_tap_to_select", SupportedLanguage.French, "Tapez un moule pour le sélectionner");
            AddTranslation("tutorial_tap_to_cast", SupportedLanguage.French, "Tapez le moule cible pour couler");
            AddTranslation("tutorial_drag_to_pour", SupportedLanguage.French, "Glissez et déposez — le minerai coule");
            AddTranslation("tutorial_well_done", SupportedLanguage.French, "Bon travail ! Essayez encore.");
            AddTranslation("tutorial_skip", SupportedLanguage.French, "Passer");
            AddTranslation("language", SupportedLanguage.French, "Langue");
            AddTranslation("sound_effects", SupportedLanguage.French, "Effets sonores");
            AddTranslation("music", SupportedLanguage.French, "Musique");
            AddTranslation("vibration", SupportedLanguage.French, "Vibration");
            AddTranslation("colorblind_mode", SupportedLanguage.French, "Mode daltonien");
            AddTranslation("font_size", SupportedLanguage.French, "Taille de police");
            AddTranslation("reduced_motion", SupportedLanguage.French, "Mouvement réduit");
            AddTranslation("reset_progress", SupportedLanguage.French, "Réinitialiser la progression");
            AddTranslation("reset_confirm", SupportedLanguage.French, "Toute la progression sera effacée. Êtes-vous sûr ?");
            AddTranslation("error_level_not_found", SupportedLanguage.French, "Niveau introuvable.");
            AddTranslation("error_connection", SupportedLanguage.French, "Erreur de connexion. Réessayez.");
            AddTranslation("error_save_failed", SupportedLanguage.French, "Échec de sauvegarde. Réessayez.");
        }
    }
}