using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Plays the configured shared UI sound when this button is clicked.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class MiningButtonSfx : MonoBehaviour
    {
        [SerializeField] private MiningAudioManager audioManager;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            button ??= GetComponent<Button>();
            button.onClick.RemoveListener(Play);
            button.onClick.AddListener(Play);
        }

        private void OnDisable()
        {
            button?.onClick.RemoveListener(Play);
        }

        private void Play()
        {
            audioManager?.PlayButtonSfx();
        }
    }
}
