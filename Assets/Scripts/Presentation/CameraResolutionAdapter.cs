using UnityEngine;
using PuzzleGame.Application.Interfaces;
using System.Linq;

namespace PuzzleGame.Presentation
{
    /// <summary>
    /// Adjusts the Main Camera's distance and look-at target dynamically
    /// based on the active gameplay Molds bounding box and screen aspect ratio.
    /// Ensures molds never clip on narrow screens or tablets.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraResolutionAdapter : MonoBehaviour
    {
        private IActiveMoldsProvider _moldsProvider;
        private Camera _camera;
        private Vector3 _defaultPosition;
        private bool _isInitialized;

        public void Initialize(IActiveMoldsProvider moldsProvider)
        {
            _moldsProvider = moldsProvider;
            _camera = GetComponent<Camera>();
            if (!_isInitialized)
            {
                _defaultPosition = transform.position;
                _isInitialized = true;
            }
            AdaptCamera();
        }

        private void LateUpdate()
        {
            if (_isInitialized)
            {
                AdaptCamera();
            }
        }

        public void AdaptCamera()
        {
            if (_camera == null || _moldsProvider == null) return;

            var activeMolds = _moldsProvider.Molds;
            if (activeMolds == null || activeMolds.Length == 0) return;

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float sumX = 0f;
            int count = 0;

            for (int i = 0; i < activeMolds.Length; i++)
            {
                var mold = activeMolds[i];
                if (mold == null || mold.Transform == null || !mold.GameObject.activeInHierarchy) continue;

                float posX = mold.Transform.position.x;
                if (posX < minX) minX = posX;
                if (posX > maxX) maxX = posX;
                sumX += posX;
                count++;
            }

            if (count == 0) return;

            float centerX = sumX / count;
            
            // Bounding box size: width of all active molds + safe side-padding (e.g. 1.8f)
            float width = (maxX - minX) + 1.8f;
            
            // Height of a mold is ~2.6f; adding headroom for Lift animation
            float height = 3.6f;

            float aspect = _camera.aspect;
            float radFov = _camera.fieldOfView * Mathf.Deg2Rad;
            float tanHalfFov = Mathf.Tan(radFov * 0.5f);

            // Compute distance needed to fit both horizontally and vertically
            float distanceX = width / (2.0f * tanHalfFov * aspect);
            float distanceY = height / (2.0f * tanHalfFov);

            float targetDistance = Mathf.Max(distanceX, distanceY) + 1.5f;
            targetDistance = Mathf.Max(12.0f, targetDistance); // Safe minimum distance to prevent zooming too close

            // Set camera position keeping the vertical offset intact
            transform.position = new Vector3(centerX, _defaultPosition.y, -targetDistance);
            transform.LookAt(new Vector3(centerX, 1.2f, 0f));
        }
    }
}
