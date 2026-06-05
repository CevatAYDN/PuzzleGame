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

        private readonly StringBuilder _sb = new StringBuilder(2048);

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
            // ── Mold States ────────────────────────────────────────────────────
            _sb.Clear();
            if (_molds != null)
            {
                for (int i = 0; i < _molds.Length; i++)
                {
                    var mold = _molds[i];
                    if (mold == null) continue;

                    var state = mold.State;
                    _sb.Append($"[{i}] {mold.GameObject.name}\n");
                    _sb.Append($"  Layers: {state.LayerCount}/{state.MaxLayers} | Full: {state.IsFull} | Empty: {state.IsEmpty}\n");
                    if (!state.IsEmpty)
                    {
                        var top = state.TopLayer;
                        if (top.HasValue)
                        {
                            var c = top.Value.Color;
                            _sb.Append($"  Top: R:{c.R:F2} G:{c.G:F2} B:{c.B:F2} A:{c.A:F2} x{top.Value.Amount:F2}\n");
                        }
                    }
                    _sb.Append("\n");
                }
            }

            if (_moldStateText != null)
                _moldStateText.text = _sb.ToString();

            // ── Service Stats ──────────────────────────────────────────────────
            if (_serviceText != null && _animationService != null)
            {
                _serviceText.text =
                    $"Animation Active: {_animationService.IsAnimating}\n";
            }

            // ── VFX Config ────────────────────────────────────────────────────
            if (_vfxText != null && _vfxConfig != null)
            {
                _vfxText.text =
                    $"Flow Intensity: {_vfxConfig.flowIntensity:F2}\n" +
                    $"Width Multiplier: {_vfxConfig.streamWidthMultiplier:F3}\n" +
                    $"Color Boost: {_vfxConfig.colorIntensityBoost:F2}\n" +
                    $"Trail: {(_vfxConfig.enableTrail ? "ON" : "OFF")}\n" +
                    $"Particle Cap: {_vfxConfig.particleCapacity}";
            }
        }
    }
}
