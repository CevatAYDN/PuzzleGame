using UnityEngine;
using UnityEngine.EventSystems;

namespace PuzzleGame.Presentation.UI
{
    /// <summary>
    /// Adds basic juice (scale bounce, glow toggle) to interactive buttons.
    /// Replaces standard flat behavior with organic liquid physics feel.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public class UIButtonJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float hoverScale = 1.05f;
        [SerializeField] private float pressScale = 0.95f;
        
        private Vector3 _originalScale;

        private void Awake()
        {
            _originalScale = transform.localScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            transform.localScale = _originalScale * hoverScale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = _originalScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            transform.localScale = _originalScale * pressScale;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            transform.localScale = _originalScale * hoverScale;
        }
    }
}
