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

    /// <summary>
    /// UI/Inspector/Reflection enumeration sırası enum tanım sırasından bağımsız.
    /// Inspector dropdown'ları, debug overlay sıralaması ve editor tooling
    /// bu listeyi kullanmalı — enum'a yeni state eklenirse buraya da ekleyin.
    /// </summary>
    public static class GameStateOrder
    {
        public static readonly System.Collections.Generic.IReadOnlyList<GameState> DisplayOrder =
            new GameState[]
            {
                GameState.Boot,
                GameState.Menu,
                GameState.LevelLoading,
                GameState.Playing,
                GameState.OptionalCasting,
                GameState.Paused,
                GameState.LevelComplete,
                GameState.LevelFailed,
                GameState.GameOver,
            };
    }
}
