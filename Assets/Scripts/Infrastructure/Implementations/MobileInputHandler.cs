using System;
using System.Collections.Generic;
using UnityEngine;
using PuzzleGame.Application.Interfaces;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// Mobile-optimized input handler with:
    /// - Dead zone filtering (ignores micro-movements below threshold)
    /// - Touch buffer (tracks recent touch positions for smoothing)
    /// - Swipe detection (directional gesture recognition)
    /// - Multi-touch support (primary touch only for gameplay)
    ///
    /// Drop-in replacement for InputHandler — same interface, richer behavior.
    /// </summary>
    public class MobileInputHandler : IInputHandler
    {
        private readonly Camera _camera;
        private readonly float _deadZoneRadius;
        private readonly int _touchBufferSize;
        private readonly float _swipeThreshold;

        // Touch buffer for position smoothing
        private readonly Queue<Vector2> _touchBuffer;
        private Vector2 _lastConfirmedDown;
        private bool _hasLastPosition;

        // Swipe state
        private Vector2 _swipeStart;
        private bool _trackingSwipe;
        private SwipeDirection? _pendingSwipe;

        public MobileInputHandler(Camera camera, float deadZoneRadius = 15f,
                                   int touchBufferSize = 5, float swipeThreshold = 60f)
        {
            _camera = camera;
            _deadZoneRadius = deadZoneRadius;
            _touchBufferSize = touchBufferSize;
            _swipeThreshold = swipeThreshold;
            _touchBuffer = new Queue<Vector2>(touchBufferSize);

            if (_camera == null)
                Debug.LogError("[MobileInputHandler] Created with null camera.");
        }

        /// <summary>
        /// Primary tap detection with dead zone filtering.
        /// Returns smoothed position if within dead zone of last confirmed tap.
        /// </summary>
        public bool GetPointerDown(out Vector2 position)
        {
            position = Vector2.zero;
            bool wasPressed = TryGetPrimaryPress(out Vector2 rawPos);

            if (!wasPressed)
            {
                _hasLastPosition = false;
                _touchBuffer.Clear();
                return false;
            }

            // Dead zone: if too close to last tap, filter it out
            if (_hasLastPosition)
            {
                float dist = Vector2.Distance(rawPos, _lastConfirmedDown);
                if (dist < _deadZoneRadius)
                    return false;
            }

            // Add to touch buffer for smoothing
            _touchBuffer.Enqueue(rawPos);
            if (_touchBuffer.Count > _touchBufferSize)
                _touchBuffer.Dequeue();

            // Smoothed position: average of buffer
            position = GetBufferedPosition();
            _lastConfirmedDown = position;
            _hasLastPosition = true;

            // Start tracking swipe
            _swipeStart = position;
            _trackingSwipe = true;
            _pendingSwipe = null;

            return true;
        }

        /// <summary>
        /// Get current swipe direction if a swipe gesture is in progress.
        /// Returns null if no swipe detected yet.
        /// </summary>
        public SwipeDirection? GetSwipeDirection()
        {
            if (!_trackingSwipe) return null;

            bool isHeld = TryGetPrimaryHold(out Vector2 currentPos);
            if (!isHeld)
            {
                _trackingSwipe = false;
                return _pendingSwipe;
            }

            Vector2 delta = currentPos - _swipeStart;
            float dist = delta.magnitude;

            if (dist >= _swipeThreshold)
            {
                _pendingSwipe = VectorToDirection(delta);
                _trackingSwipe = false;
                return _pendingSwipe;
            }

            return null;
        }

        /// <summary>
        /// Raycast with screen boundary validation.
        /// </summary>
        public bool Raycast(Vector2 screenPos, LayerMask mask, out RaycastHit hit)
        {
            hit = default;
            if (_camera == null) return false;

            if (screenPos.x < 0f || screenPos.x > Screen.width ||
                screenPos.y < 0f || screenPos.y > Screen.height)
                return false;

            Ray ray = _camera.ScreenPointToRay(screenPos);
            return Physics.Raycast(ray, out hit, 100f, mask);
        }

        /// <inheritdoc/>
        public bool Raycast(Vector2 screenPos, LayerMask mask, out RaycastHit hit, out Collider hitCollider)
        {
            hitCollider = null;
            bool result = Raycast(screenPos, mask, out hit);
            if (result) hitCollider = hit.collider;
            return result;
        }


        /// <summary>
        /// Reset all input state. Call on scene change.
        /// </summary>
        public void Reset()
        {
            _hasLastPosition = false;
            _touchBuffer.Clear();
            _trackingSwipe = false;
            _pendingSwipe = null;
        }

        // ── Private helpers ──

        private static bool TryGetPrimaryPress(out Vector2 position)
        {
            position = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                position = Mouse.current.position.ReadValue();
                return true;
            }
            if (Touchscreen.current != null &&
                Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
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

        private static bool TryGetPrimaryHold(out Vector2 position)
        {
            position = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                position = Mouse.current.position.ReadValue();
                return true;
            }
            if (Touchscreen.current != null &&
                Touchscreen.current.primaryTouch.press.isPressed)
            {
                position = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }
#else
            if (Input.GetMouseButton(0))
            {
                position = Input.mousePosition;
                return true;
            }
#endif
            return false;
        }

        private Vector2 GetBufferedPosition()
        {
            if (_touchBuffer.Count == 0) return Vector2.zero;

            float x = 0f, y = 0f;
            foreach (var pos in _touchBuffer)
            {
                x += pos.x;
                y += pos.y;
            }
            float invCount = 1f / _touchBuffer.Count;
            return new Vector2(x * invCount, y * invCount);
        }

        private static SwipeDirection VectorToDirection(Vector2 delta)
        {
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            if (angle >= 315f || angle < 45f)   return SwipeDirection.Right;
            if (angle >= 45f && angle < 135f)    return SwipeDirection.Up;
            if (angle >= 135f && angle < 225f)   return SwipeDirection.Left;
            return SwipeDirection.Down;
        }
    }

    /// <summary>
    /// Swipe direction enum for gesture recognition.
    /// </summary>
    public enum SwipeDirection
    {
        Up,
        Down,
        Left,
        Right
    }
}
