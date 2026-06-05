using UnityEngine;

namespace PuzzleGame.Application.Configuration
{
    [CreateAssetMenu(fileName = "EconomyConfig", menuName = "PuzzleGame/EconomyConfig")]
    public class EconomyConfig : ScriptableObject
    {
        [Header("Wallet")]
        [Min(0)] public int startingCoins = 50;
        [Min(0)] public int dailyLoginBonus = 25;
        [Min(0)] public int adRewardCoins = 10;

        [Header("Costs")]
        [Min(0)] public int hintCost = 15;
        [Min(0)] public int undoCost = 10;
        [Min(0)] public int extraMoldCost = 100;

        [Header("Rewards")]
        [Min(0)] public int perLevelCompletionBonus = 5;
        [Min(1)] public int threeStarBonusMultiplier = 3;

        [Header("Limits")]
        [Min(0)] public int maxHintPerLevel = 3;
        [Min(0)] public int maxUndoPerLevel = 5;
    }
}
