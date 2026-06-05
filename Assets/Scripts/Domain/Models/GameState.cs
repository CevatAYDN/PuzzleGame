namespace PuzzleGame.Domain.Models
{
    /// <summary>
    /// Oyunun global state'leri. State machine tarafından yönetilir.
    /// UI, input, audio her state'e göre davranışını değiştirir.
    /// </summary>
    public enum GameState
    {
        /// <summary>Oyun başlatılıyor, servisler kuruluyor.</summary>
        Boot,

        /// <summary>Ana menü ekranı, kullanıcı level seçiyor.</summary>
        Menu,

        /// <summary>Level yükleniyor, sahne setup.</summary>
        LevelLoading,

        /// <summary>Oyun aktif, input enabled.</summary>
        Playing,

        /// <summary>Seviye bitti, isteğe bağlı döküm yapılıyor.</summary>
        OptionalCasting,

        /// <summary>Oyun duraklatıldı, input disabled, timeScale=0.</summary>
        Paused,

        /// <summary>Level başarıyla tamamlandı, complete screen.</summary>
        LevelComplete,

        /// <summary>Level başarısız, retry screen.</summary>
        LevelFailed,

        /// <summary>Tüm oyun tamamlandı, credits/fin.</summary>
        GameOver,
    }
}
