using System.Collections.Generic;
using UnityEngine;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain.Interfaces;
// IRendererService now in PuzzleGame.Application.Interfaces

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// MoldController (MonoBehaviour) için Application-katmanı soyutlaması.
    /// Domain katmanının Unity'ye bağımlı olmamasını sağlar.
    /// Unit testlerde mock'lanabilir.
    /// </summary>
    public interface IMoldView
    {
        MoldState State { get; }
        bool IsEmpty { get; }
        bool IsCapped { get; }
        Transform Transform { get; }
        GameObject GameObject { get; }
        float Height { get; }
        IReadOnlyList<OreLayer> VisualLayers { get; }
        float VisualTotalFill { get; }

        /// <summary>
        /// Pool-assigned index. Set by MoldPoolInitializer so CastService
        /// never needs to parse GameObject.name strings.
        /// </summary>
        int MoldIndex { get; set; }

        void Initialize(IRendererService rendererService,
                        IMoldValidator validator,
                        IAnimationService animationService,
                        List<OreLayer> initialLayers,
                        Configuration.MoldVisualConfig visualConfigOverride = null);
        void SetSelectionHighlight(bool active);
        void AnimateCompletion();
        void UpdateVisualsFromState();
        void SetVisualState(IReadOnlyList<OreLayer> layers, float totalFill);
        void SetVisualCastProgress(LayerSnapshot startLayers, float t, bool isSource, OreLayer CastedLayer);
        void PlaySettleBounce();
        void AddWobbleImpulse(Vector3 direction, float strength);
    }
}
