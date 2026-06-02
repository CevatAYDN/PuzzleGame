using UnityEngine;

namespace PuzzleGame.Configuration
{
    /// <summary>
    /// Oyun içi ekonomi dengesi için yapılandırma.
    /// Jeton, ipucu, enerji gibi kaynakların yönetimi.
    /// </summary>
    [CreateAssetMenu(fileName = "EconomyConfig", menuName = "PuzzleGame/EconomyConfig")]
    public class EconomyConfig : ScriptableObject
    {
        [Header("Başlangıç Değerleri")]
        public int initialCoins = 100;
        public int initialHints = 3;
        public int initialEnergy = 5;

        [Header("Kazanç Oranları")]
        [Tooltip("Seviye başı kazanılan minimum jeton")]
        public int minCoinsPerLevel = 10;
        [Tooltip("Seviye başı kazanılan maksimum jeton")]
        public int maxCoinsPerLevel = 50;
        [Tooltip("3 yıldız bonusu")]
        public int threeStarBonus = 25;

        [Header("Giderler")]
        [Tooltip("İpucu maliyeti")]
        public int hintCost = 20;
        [Tooltip("Geri alma maliyeti (eğer sınırlıysa)")]
        public int undoCost = 5;
        [Tooltip("Enerji yenileme maliyeti")]
        public int energyRefillCost = 100;

        [Header("Reklam Bonusları")]
        [Tooltip("Rewarded ad sonrası kazanılan jeton")]
        public int adRewardCoins = 30;
        [Tooltip("Günlük ad bonus çarpanı")]
        public float dailyAdBonusMultiplier = 1.5f;

        /// <summary>
        /// Hamle sayısına göre jeton ödülü hesaplar.
        /// </summary>
        public int CalculateCoinsForLevel(int moveCount, int starsEarned)
        {
            // Daha az hamle = daha fazla jeton
            int baseCoins = Mathf.Clamp(maxCoinsPerLevel - (moveCount * 2), minCoinsPerLevel, maxCoinsPerLevel);
            int starBonus = starsEarned >= 3 ? threeStarBonus : 0;
            return baseCoins + starBonus;
        }
    }
}