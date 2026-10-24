using UnityEngine;
using UnityEngine.InputSystem;

namespace MiningSimulator.Ores
{
    /// <summary>Designer-owned world interaction and hover-prompt configuration.</summary>
    [CreateAssetMenu(fileName = "MiningInteractionData",
        menuName = "Mining Simulator/Game Data/Interaction")]
    public sealed class MiningInteractionData : ScriptableObject
    {
        [Header("World Detection")]
        [SerializeField] private LayerMask interactionLayers = ~0;
        [Min(0.1f), SerializeField] private float maximumDistance = 500f;
        [SerializeField] private Key interactionKey = Key.F;

        [Header("Hover Prompt")]
        [SerializeField] private Vector2 promptSize = new(270f, 72f);
        [SerializeField] private Vector2 cursorOffset = new(22f, -18f);
        [Min(1f), SerializeField] private float fontSize = 20f;
        [SerializeField] private Color backgroundColor = new(0.035f, 0.045f, 0.06f, 0.94f);
        [SerializeField] private Color textColor = Color.white;

        [Header("Localization")]
        [SerializeField] private string englishPromptFormat = "[{0}] INTERACT\n{1}";
        [SerializeField] private string vietnamesePromptFormat = "[{0}] TƯƠNG TÁC\n{1}";

        public LayerMask InteractionLayers => interactionLayers;
        public float MaximumDistance => maximumDistance;
        public Key InteractionKey => interactionKey == Key.None ? Key.F : interactionKey;
        public Vector2 PromptSize => promptSize;
        public Vector2 CursorOffset => cursorOffset;
        public float FontSize => fontSize;
        public Color BackgroundColor => backgroundColor;
        public Color TextColor => textColor;

        public string GetPrompt(string targetLabel)
        {
            string keyName = InteractionKey.ToString().ToUpperInvariant();
            string format = MiningLocalization.Text(englishPromptFormat,
                vietnamesePromptFormat);
            return (format ?? "[{0}] INTERACT\n{1}")
                .Replace("{0}", keyName)
                .Replace("{1}", targetLabel ?? string.Empty);
        }

        private void OnValidate()
        {
            maximumDistance = Mathf.Max(0.1f, maximumDistance);
            promptSize.x = Mathf.Max(1f, promptSize.x);
            promptSize.y = Mathf.Max(1f, promptSize.y);
            fontSize = Mathf.Max(1f, fontSize);
            if (interactionKey == Key.None)
            {
                interactionKey = Key.F;
            }
        }
    }
}
