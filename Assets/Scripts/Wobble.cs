using UnityEngine;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Infrastructure.Implementations;

namespace PuzzleGame
{
    [RequireComponent(typeof(Renderer))]
    public class Wobble : MonoBehaviour, IUpdateable
    {
        [Header("Configuration")]
        public Configuration.WobbleConfig config;

        private Renderer _renderer;
        private MaterialPropertyBlock _propBlock;
        private Vector3 _previousPosition;
        private Vector3 _previousRotation;
        private float _wobbleX;
        private float _wobbleZ;
        private float _velocityX;
        private float _velocityZ;
        private bool _hasLiquidMaterial;
        private bool _isWobbleActive = true;
        private float _timeSinceLastUpdate;

        private static readonly int WobbleXProperty = Shader.PropertyToID("_WobbleX");
        private static readonly int WobbleZProperty = Shader.PropertyToID("_WobbleZ");

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _propBlock = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            UpdateManager.Instance?.Register(this);
        }

        private void Start()
        {
            _previousPosition = transform.position;
            _previousRotation = transform.rotation.eulerAngles;
            _hasLiquidMaterial = _renderer != null && _renderer.sharedMaterials != null && _renderer.sharedMaterials.Length > 1;
        }

        public void OnUpdate(float deltaTime)
        {
            if (!gameObject.activeInHierarchy) return;
            if (_renderer == null) return;

            Vector3 currentPosition = transform.position;
            Vector3 currentRotation = transform.rotation.eulerAngles;

            float moveDistSqr = (currentPosition - _previousPosition).sqrMagnitude;
            float rotDistSqr = (currentRotation - _previousRotation).sqrMagnitude;

            bool hasMovement = moveDistSqr > 0.00001f || rotDistSqr > 0.00001f;
            bool hasWobble = Mathf.Abs(_wobbleX) > 0.0001f || Mathf.Abs(_wobbleZ) > 0.0001f || Mathf.Abs(_velocityX) > 0.0001f || Mathf.Abs(_velocityZ) > 0.0001f;

            // Config defaults (config null ise fallback)
            float maxWobble = config != null ? config.maxWobble : 0.05f;
            float wobbleSpeed = config != null ? config.wobbleSpeed : 6.0f;
            float recoveryRate = config != null ? config.recoveryRate : 1.5f;
            float movementMultiplier = config != null ? config.movementMultiplier : 1.0f;
            float rotationMultiplier = config != null ? config.rotationMultiplier : 0.15f;
            float updateInterval = config != null ? config.updateInterval : 0.05f;

            if (hasMovement || hasWobble)
            {
                _isWobbleActive = true;

                // Calculate movement and rotation velocities
                Vector3 moveVelocity = (deltaTime > 0f) ? (_previousPosition - currentPosition) / deltaTime : Vector3.zero;
                Vector3 rotationDelta = currentRotation - _previousRotation;

                // Normalize rotation delta (handle 0-360 wrap)
                rotationDelta.x = Mathf.DeltaAngle(0, rotationDelta.x);
                rotationDelta.y = Mathf.DeltaAngle(0, rotationDelta.y);
                rotationDelta.z = Mathf.DeltaAngle(0, rotationDelta.z);

                // Smoothly reduce wobble over time (damping)
                _wobbleX = Mathf.SmoothDamp(_wobbleX, 0, ref _velocityX, 1.0f / recoveryRate);
                _wobbleZ = Mathf.SmoothDamp(_wobbleZ, 0, ref _velocityZ, 1.0f / recoveryRate);

                // Add movement and rotation to wobble
                float wobbleXDelta = (moveVelocity.x + rotationDelta.z * rotationMultiplier) * maxWobble * movementMultiplier;
                float wobbleZDelta = (moveVelocity.z - rotationDelta.x * rotationMultiplier) * maxWobble * movementMultiplier;

                _wobbleX = Mathf.Clamp(_wobbleX + wobbleXDelta, -maxWobble, maxWobble);
                _wobbleZ = Mathf.Clamp(_wobbleZ + wobbleZDelta, -maxWobble, maxWobble);

                // Throttled shader update — 60fps gereksiz, 20fps yeterli
                _timeSinceLastUpdate += deltaTime;
                if (_timeSinceLastUpdate >= updateInterval)
                {
                    _timeSinceLastUpdate = 0f;
                    // Apply sine wave wobble animation
                    float time = Time.time * wobbleSpeed;
                    float wobbleAmountX = _wobbleX * Mathf.Sin(time);
                    float wobbleAmountZ = _wobbleZ * Mathf.Sin(time + Mathf.PI * 0.3f);

                    // Send to shader using MaterialPropertyBlock
                    if (_hasLiquidMaterial)
                    {
                        _renderer.GetPropertyBlock(_propBlock, 1);
                        _propBlock.SetFloat(WobbleXProperty, wobbleAmountX);
                        _propBlock.SetFloat(WobbleZProperty, wobbleAmountZ);
                        _renderer.SetPropertyBlock(_propBlock, 1);
                    }
                }
            }
            else if (_isWobbleActive)
            {
                _wobbleX = 0f;
                _wobbleZ = 0f;
                _velocityX = 0f;
                _velocityZ = 0f;
                _timeSinceLastUpdate = 0f;

                if (_hasLiquidMaterial)
                {
                    _renderer.GetPropertyBlock(_propBlock, 1);
                    _propBlock.SetFloat(WobbleXProperty, 0f);
                    _propBlock.SetFloat(WobbleZProperty, 0f);
                    _renderer.SetPropertyBlock(_propBlock, 1);
                }
                _isWobbleActive = false;
            }

            // Save for next frame
            _previousPosition = currentPosition;
            _previousRotation = currentRotation;
        }

        private void OnDisable()
        {
            // Reset wobble when disabled using MaterialPropertyBlock to prevent material copy instantiation
            if (_renderer != null && _hasLiquidMaterial)
            {
                _renderer.GetPropertyBlock(_propBlock, 1);
                _propBlock.SetFloat(WobbleXProperty, 0f);
                _propBlock.SetFloat(WobbleZProperty, 0f);
                _renderer.SetPropertyBlock(_propBlock, 1);
            }

            UpdateManager.Instance?.Unregister(this);
        }

        /// <summary>
        /// Adds an impulse to the wobble (for pouring effects)
        /// </summary>
        public void AddImpulse(Vector3 direction, float strength)
        {
            float maxWobble = config != null ? config.maxWobble : 0.05f;
            _wobbleX = Mathf.Clamp(_wobbleX + direction.x * strength * maxWobble, -maxWobble, maxWobble);
            _wobbleZ = Mathf.Clamp(_wobbleZ + direction.z * strength * maxWobble, -maxWobble, maxWobble);
        }
    }
}
