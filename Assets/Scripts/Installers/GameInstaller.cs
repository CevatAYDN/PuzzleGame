using UnityEngine;
using Nexus.Core;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Configuration;

namespace PuzzleGame.Installers
{
    /// <summary>
    /// Composition Root: All configurations and catalogs assigned in the Inspector are stored here.
    /// Inherits from Nexus.Core.Root to serve as the context root for the scene.
    /// </summary>
    public class GameInstaller : Root
    {
        [Header("Configurations (auto-loaded from Resources if not assigned)")]
        [SerializeField] public GameConfig gameConfig;
        [SerializeField] public AnimationConfig animationConfig;
        [SerializeField] public LevelConfig levelConfig;
        [SerializeField] public AudioConfig audioConfig;
        [SerializeField] public StreamVFXConfig streamVFXConfig;
        [SerializeField] public EconomyConfig economyConfig;
        [SerializeField] public WobbleConfig wobbleConfig;
        [SerializeField] public LevelData[] levelCatalog;
    }
}
