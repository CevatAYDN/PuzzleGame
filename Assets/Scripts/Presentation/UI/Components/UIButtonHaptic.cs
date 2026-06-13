using UnityEngine;
using UnityEngine.UI;
using PuzzleGame.Application.Interfaces;
using VContainer;

namespace PuzzleGame.Presentation.UI.Components
{
    [RequireComponent(typeof(Button))]
    public class UIButtonHaptic : MonoBehaviour
    {
        private Button _button;
        private IHapticFeedbackService _haptic;

        [Inject]
        public void Construct(IHapticFeedbackService haptic)
        {
            _haptic = haptic;
        }

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (_button != null)
                _button.onClick.AddListener(OnButtonClicked);
        }

        private void OnDisable()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnButtonClicked);
        }

        private void OnButtonClicked()
        {
            _haptic?.Trigger(HapticIntensity.Selection);
        }
    }
}
