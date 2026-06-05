using UnityEngine;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Configuration;

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// Renders a fading trail behind the pour stream using a LineRenderer.
    /// Ring buffer of position samples, additive blending, configurable alpha.
    /// </summary>
    public class StreamTrailController : IStreamTrailController
    {
        private readonly StreamVFXConfig _config;
        private LineRenderer _lineRenderer;
        private Vector3[] _positions;
        private int _writeIndex;
        private int _sampleCount;
        private bool _isActive;
        private float _fadeTimer;
        private Color _baseColor;

        public StreamTrailController(StreamVFXConfig config)
        {
            _config = config ?? throw new System.ArgumentNullException(nameof(config));
        }

        public float TrailFadeDuration => _config.trailFadeDuration;
        public float TrailAlpha => _config.trailAlpha;

        /// <summary>
        /// Ensures the LineRenderer component exists on the given GameObject.
        /// Only called once during CastAnimationState setup.
        /// </summary>
        public LineRenderer EnsureLineRenderer(GameObject owner)
        {
            if (_lineRenderer != null) return _lineRenderer;

            _lineRenderer = owner.GetComponent<LineRenderer>();
            if (_lineRenderer == null)
            {
                _lineRenderer = owner.AddComponent<LineRenderer>();
            }

            int count = _config.trailSampleCount;
            _positions = new Vector3[count];
            _lineRenderer.positionCount = count;
            _lineRenderer.startWidth = 0.04f;
            _lineRenderer.endWidth = 0.01f;
            _lineRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            _lineRenderer.material.color = new Color(1, 1, 1, _config.trailAlpha);
            _lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _lineRenderer.receiveShadows = false;
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.enabled = false;

            return _lineRenderer;
        }

        public void BeginTrail(Vector3 startPosition, Color color)
        {
            if (!_config.enableTrail) return;
            if (_lineRenderer == null) return;

            _isActive = true;
            _fadeTimer = 0f;
            _baseColor = color;
            _writeIndex = 0;
            _sampleCount = 0;

            // Initialize all positions to the start
            for (int i = 0; i < _positions.Length; i++)
            {
                _positions[i] = startPosition;
            }

            Color trailColor = new Color(color.r, color.g, color.b, _config.trailAlpha);
            _lineRenderer.startColor = trailColor;
            _lineRenderer.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
            _lineRenderer.enabled = true;

            UpdatePositions();
        }

        public void UpdateTrail(Vector3 worldPosition)
        {
            if (!_isActive || _lineRenderer == null) return;

            _positions[_writeIndex] = worldPosition;
            _writeIndex = (_writeIndex + 1) % _positions.Length;
            if (_sampleCount < _positions.Length) _sampleCount++;

            UpdatePositions();
        }

        public void EndTrail()
        {
            _isActive = false;
            _fadeTimer = _config.trailFadeDuration;
        }

        /// <summary>
        /// Call from a MonoBehaviour Update loop to handle fade-out.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (_fadeTimer <= 0f || _lineRenderer == null) return;

            _fadeTimer -= deltaTime;
            float alpha = Mathf.Clamp01(_fadeTimer / _config.trailFadeDuration) * _config.trailAlpha;

            Color faded = _lineRenderer.startColor;
            faded.a = alpha;
            _lineRenderer.startColor = faded;

            if (_fadeTimer <= 0f)
            {
                _lineRenderer.enabled = false;
            }
        }

        private void UpdatePositions()
        {
            if (_lineRenderer == null) return;

            // Walk forward from writeIndex to render positions in time order
            for (int i = 0; i < _sampleCount; i++)
            {
                int srcIdx = (_writeIndex - _sampleCount + i + _positions.Length) % _positions.Length;
                _lineRenderer.SetPosition(i, _positions[srcIdx]);
            }

            // Fill remaining with last known position
            for (int i = _sampleCount; i < _positions.Length; i++)
            {
                int lastIdx = (_writeIndex - 1 + _positions.Length) % _positions.Length;
                _lineRenderer.SetPosition(i, _positions[lastIdx]);
            }
        }

        public void SetAlpha(float alpha)
        {
            if (_lineRenderer == null) return;
            Color faded = _lineRenderer.startColor;
            faded.a = alpha;
            _lineRenderer.startColor = faded;

            if (alpha <= 0f)
            {
                _lineRenderer.enabled = false;
            }
        }

        /// <summary>
        /// Destroys the owned LineRenderer material to prevent leaks.
        /// Call when the owning cast animation state is cleaned up.
        /// </summary>
        public void Cleanup()
        {
            if (_lineRenderer != null)
            {
                if (_lineRenderer.material != null)
                {
                    UnityEngine.Object.Destroy(_lineRenderer.material);
                }
                UnityEngine.Object.Destroy(_lineRenderer);
            }
            _lineRenderer = null;
            _isActive = false;
        }
    }
}
