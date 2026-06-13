using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace PuzzleGame.Presentation.UI
{
    /// <summary>
    /// Manages Gamepad/Keyboard focus to ensure a Controller-first AAA experience.
    /// Restores focus to previous elements when panels close.
    /// </summary>
    public class UINavigationManager : MonoBehaviour
    {
        private static UINavigationManager _instance;
        public static UINavigationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[UINavigationManager]");
                    _instance = go.AddComponent<UINavigationManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private Stack<GameObject> _focusHistory = new Stack<GameObject>();
        private GameObject _currentFocus;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != _currentFocus)
            {
                if (EventSystem.current.currentSelectedGameObject != null)
                {
                    _currentFocus = EventSystem.current.currentSelectedGameObject;
                }
            }
        }

        /// <summary>
        /// Call this when opening a new modal/panel to set the initial focus.
        /// </summary>
        public void PushFocus(GameObject newFocusElement)
        {
            if (_currentFocus != null)
            {
                _focusHistory.Push(_currentFocus);
            }

            if (newFocusElement != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(newFocusElement);
                _currentFocus = newFocusElement;
            }
        }

        /// <summary>
        /// Call this when closing a modal/panel to restore the previous focus.
        /// </summary>
        public void PopFocus()
        {
            if (_focusHistory.Count > 0)
            {
                var previousFocus = _focusHistory.Pop();
                if (previousFocus != null && previousFocus.activeInHierarchy && EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(previousFocus);
                    _currentFocus = previousFocus;
                }
            }
            else if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                _currentFocus = null;
            }
        }
        
        public void ClearHistory()
        {
            _focusHistory.Clear();
            _currentFocus = null;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
