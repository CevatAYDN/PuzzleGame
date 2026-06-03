using System.Collections.Generic;
using UnityEngine;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain.Interfaces;
// IRendererService now in PuzzleGame.Application.Interfaces

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// BottleController (MonoBehaviour) için Application-katmanı soyutlaması.
    /// Domain katmanının Unity'ye bağımlı olmamasını sağlar.
    /// Unit testlerde mock'lanabilir.
    /// </summary>
    public interface IBottleView
    {
        BottleState State { get; }
        bool IsEmpty { get; }
        bool IsCapped { get; }
        Transform Transform { get; }
        GameObject GameObject { get; }
        float Height { get; }
        IReadOnlyList<LiquidLayer> VisualLayers { get; }
        float VisualTotalFill { get; }

        /// <summary>
        /// Pool-assigned index. Set by BottlePoolInitializer so PourService
        /// never needs to parse GameObject.name strings.
        /// </summary>
        int BottleIndex { get; set; }

        void Initialize(IRendererService rendererService,
                        IBottleValidator validator,
                        IAnimationService animationService,
                        List<LiquidLayer> initialLayers);
        bool TryPourTo(IBottleView target);
        void SetSelectionHighlight(bool active);
        void AnimateCompletion();
        void UpdateVisualsFromState();
        void SetVisualState(IReadOnlyList<LiquidLayer> layers, float totalFill);
        void SetVisualPourProgress(LayerSnapshot startLayers, float t, bool isSource, LiquidLayer pouredLayer);
        void PlaySettleBounce();
        void AddWobbleImpulse(Vector3 direction, float strength);
    }
}
