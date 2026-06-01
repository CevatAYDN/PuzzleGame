using UnityEngine;
using BottleShaders.Application.Interfaces;
using BottleShaders.Infrastructure.Implementations;

namespace BottleShaders
{
    [RequireComponent(typeof(Renderer))]
    public class Wobble : MonoBehaviour, IUpdateable
    {
        [Header("Wobble Settings")]
        [SerializeField] private float maxWobble = 0.05f;
        [SerializeField] private float wobbleSpeed = 6.0f;
        [SerializeField] private float recoveryRate = 1.5f;
        [SerializeField] private float movementMultiplier = 1.0f;
        [SerializeField] private float rotationMultiplier = 0.15f;

        private Renderer _renderer;
        private MaterialPropertyBlock _propBlock;
        private Vector3 _previousPosition;
        private Vector3 _previousRotation;
        private float _wobbleX;
        private float _wobbleZ;
        private float _velocityX;
        private float _velocityZ;
        private bool _hasLiquidMaterial;

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

            // Smoothly reduce wobble over time (damping)
            _wobbleX = Mathf.SmoothDamp(_wobbleX, 0, ref _velocityX, 1.0f / recoveryRate);
            _wobbleZ = Mathf.SmoothDamp(_wobbleZ, 0, ref _velocityZ, 1.0f / recoveryRate);

            // Calculate movement and rotation
            Vector3 currentPosition = transform.position;
            Vector3 currentRotation = transform.rotation.eulerAngles;

            Vector3 moveVelocity = (_previousPosition - currentPosition) / deltaTime;
            Vector3 rotationDelta = currentRotation - _previousRotation;
            
            // Normalize rotation delta (handle 0-360 wrap)
            rotationDelta.x = Mathf.DeltaAngle(0, rotationDelta.x);
            rotationDelta.y = Mathf.DeltaAngle(0, rotationDelta.y);
            rotationDelta.z = Mathf.DeltaAngle(0, rotationDelta.z);

            // Add movement and rotation to wobble
            float wobbleXDelta = (moveVelocity.x + rotationDelta.z * rotationMultiplier) * maxWobble * movementMultiplier;
            float wobbleZDelta = (moveVelocity.z - rotationDelta.x * rotationMultiplier) * maxWobble * movementMultiplier;

            _wobbleX = Mathf.Clamp(_wobbleX + wobbleXDelta, -maxWobble, maxWobble);
            _wobbleZ = Mathf.Clamp(_wobbleZ + wobbleZDelta, -maxWobble, maxWobble);

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
            _wobbleX = Mathf.Clamp(_wobbleX + direction.x * strength * maxWobble, -maxWobble, maxWobble);
            _wobbleZ = Mathf.Clamp(_wobbleZ + direction.z * strength * maxWobble, -maxWobble, maxWobble);
        }
    }
}
