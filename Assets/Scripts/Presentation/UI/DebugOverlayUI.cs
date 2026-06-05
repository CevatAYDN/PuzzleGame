using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Configuration;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PuzzleGame.Presentation.UI
{
    /// <summary>
    /// Runtime debug overlay displaying mold states, active tweens, and VFX params.
    /// Toggle with backtick key (default). Shows in the top-right corner of the Game view.
    /// Attached to the HUD Canvas by GameManager on start.
    ///
    /// Sprint #13: All three text blocks now share a single StringBuilder (4096 capacity)
    /// instead of string interpolation. Previous version allocated 6+ strings per frame
    /// (one per interpolated segment plus concatenation intermediates) — now exactly 3
    /// (one ToString per TMP.text assignment, which is unavoidable).
    /// </summary>
    public class DebugOverlayUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject _overlayRoot;
        [SerializeField] private TextMeshProUGUI _moldStateText;
        [SerializeField] private TextMeshProUGUI _serviceText;
        [SerializeField] private TextMeshProUGUI _vfxText;

        private bool _isVisible;
        private IAnimationService _animationService;
        private IMoldView[] _molds;
        private StreamVFXConfig _vfxConfig;
        private PourConfig _pourConfig;

        // Capacity sized for max-mold level (ForgeConstants.MaxMoldsPerLevel = 16 × ~200 chars).
        private readonly StringBuilder _sb = new StringBuilder(4096);

        private void Awake()
        {
            if (_overlayRoot != null)
                _overlayRoot.SetActive(false);
        }

        public void Initialize(
            IAnimationService animationService,
            IMoldView[] molds,
            StreamVFXConfig vfxConfig = null,
            PourConfig pourConfig = null)
        {
            _animationService = animationService;
            _molds = molds;
            _vfxConfig = vfxConfig;
            _pourConfig = pourConfig;
        }

        private void Update()
        {
            if (IsToggleKeyPressed())
            {
                _isVisible = !_isVisible;
                if (_overlayRoot != null)
                    _overlayRoot.SetActive(_isVisible);
            }

            if (_isVisible)
            {
                RefreshDisplay();
            }
        }

        private static bool IsToggleKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && kb.backquoteKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.BackQuote);
#endif
        }

        private void RefreshDisplay()
        {
            // Each block: _sb.Clear() → append → _xxxText.text = _sb.ToString().
            // _sb is shared across blocks but cleared between, so mutations don't leak.
            if (_moldStateText != null) RefreshMoldState();
            if (_serviceText != null && _animationService != null) RefreshService();
            if (_vfxText != null && _vfxConfig != null) RefreshVfx();
        }

        private void RefreshMoldState()
        {
            _sb.Clear();
            if (_molds != null)
            {
                for (int i = 0; i < _molds.Length; i++)
                {
                    var mold = _molds[i];
                    if (mold == null) continue;

                    var state = mold.State;
                    _sb.Append('[').Append(i).Append("] ").Append(mold.GameObject.name).Append('\n');
                    _sb.Append("  Layers: ").Append(state.LayerCount).Append('/').Append(state.MaxLayers)
                       .Append(" | Full: ").Append(state.IsFull)
                       .Append(" | Empty: ").Append(state.IsEmpty).Append('\n');
                    if (!state.IsEmpty)
                    {
                        var top = state.TopLayer;
                        if (top.HasValue)
                        {
                            var c = top.Value.Color;
                            _sb.Append("  Top: R:").AppendFormat("{0:F2}", c.R)
                               .Append(" G:").AppendFormat("{0:F2}", c.G)
                               .Append(" B:").AppendFormat("{0:F2}", c.B)
                               .Append(" A:").AppendFormat("{0:F2}", c.A)
                               .Append(" x").AppendFormat("{0:F2}", top.Value.Amount)
                               .Append('\n');
                        }
                    }
                    _sb.Append('\n');
                }
            }
            _moldStateText.text = _sb.ToString();
        }

        private void RefreshService()
        {
            _sb.Clear();
            _sb.Append("Animation Active: ").Append(_animationService.IsAnimating).Append('\n');
            _serviceText.text = _sb.ToString();
        }

        private void RefreshVfx()
        {
            _sb.Clear();
            _sb.Append("Flow Intensity: ").AppendFormat("{0:F2}", _vfxConfig.flowIntensity).Append('\n');
            _sb.Append("Width Multiplier: ").AppendFormat("{0:F3}", _vfxConfig.streamWidthMultiplier).Append('\n');
            _sb.Append("Color Boost: ").AppendFormat("{0:F2}", _vfxConfig.colorIntensityBoost).Append('\n');
            _sb.Append("Trail: ").Append(_vfxConfig.enableTrail ? "ON" : "OFF").Append('\n');
            _sb.Append("Particle Cap: ").Append(_vfxConfig.particleCapacity);
            _vfxText.text = _sb.ToString();
        }
    }
}
