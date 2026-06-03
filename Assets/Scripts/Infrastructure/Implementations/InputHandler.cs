using PuzzleGame.Infrastructure.Interfaces;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// Abstracts Unity's input system so the rest of the codebase
    /// never touches Input/Mouse/Touchscreen directly.
    /// Supports both the legacy Input Manager and the new Input System.
    /// </summary>
    public class InputHandler : IInputHandler
    {
        private readonly Camera _camera;

        public InputHandler(Camera camera)
        {
            _camera = camera;

            if (_camera == null)
                Debug.LogError("InputHandler created with a null camera — Raycast will always fail.");
        }

        // ── IInputHandler ────────────────────────────────────────────────────

        /// <inheritdoc/>
        public bool GetPointerDown(out Vector2 position)
        {
            position = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                position = Mouse.current.position.ReadValue();
                return true;
            }
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                position = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }
#else
            if (Input.GetMouseButtonDown(0))
            {
                position = Input.mousePosition;
                return true;
            }
#endif
            return false;
        }

        /// <inheritdoc/>
        public bool Raycast(Vector2 screenPos, LayerMask mask, out RaycastHit hit)
        {
            hit = default;
            if (_camera == null) return false;

            // Screen boundary validation
            if (screenPos.x < 0f || screenPos.x > Screen.width || screenPos.y < 0f || screenPos.y > Screen.height)
                return false;

            Ray ray = _camera.ScreenPointToRay(screenPos);
            return Physics.Raycast(ray, out hit, 100f, mask);
        }
    }
}
