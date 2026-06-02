using System;
using UnityEngine;

namespace PuzzleGame.Configuration
{
    /// <summary>
    /// Zorluk eğrisi yöneticisi.
    /// Level numarasına göre zorluk faktörü hesaplar.
    /// </summary>
    [CreateAssetMenu(fileName = "DifficultyCurve", menuName = "PuzzleGame/DifficultyCurve")]
    public class DifficultyCurve : ScriptableObject
    {
        [Header("Zorluk Ayarları")]
        [Tooltip("Başlangıç zorluk seviyesi (0.0 - 1.0)")]
        public float startDifficulty = 0.1f;
        
        [Tooltip("Maksimum zorluk seviyesi (0.0 - 1.0)")]
        public float maxDifficulty = 0.9f;
        
        [Tooltip("Zorluk artış hızı")]
        public float difficultyRate = 0.02f;
        
        [Tooltip("Zorluk eğrisi (Level numarasına göre)")]
        public AnimationCurve difficultyCurve = AnimationCurve.Linear(0, 0.1f, 100, 0.9f);

        /// <summary>
        /// Level numarasına göre zorluk faktörü döndürür.
        /// </summary>
        public float GetDifficultyForLevel(int levelNumber)
        {
            if (levelNumber <= 0) return startDifficulty;
            
            // AnimationCurve veya lineer hesaplama kullan
            float difficulty = difficultyCurve.Evaluate(levelNumber);
            
            return Math.Clamp(difficulty, startDifficulty, maxDifficulty);
        }

        /// <summary>
        /// Zorluk faktörüne göre renk sayısı önerir.
        /// </summary>
        public int GetRecommendedColorCount(int levelNumber)
        {
            float diff = GetDifficultyForLevel(levelNumber);
            
            // Kolay: 3-4 renk, Orta: 5-6 renk, Zor: 7+ renk
            if (diff < 0.3f) return 4;
            if (diff < 0.6f) return 6;
            return 8;
        }
    }
}