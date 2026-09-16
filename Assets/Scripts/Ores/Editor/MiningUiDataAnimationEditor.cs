#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>
    /// Keeps authored scene layout as the source of truth. MiningUiData retains its legacy
    /// serialized fields for compatibility, but designers only edit runtime motion here.
    /// </summary>
    [CustomEditor(typeof(MiningUiData))]
    public sealed class MiningUiDataAnimationEditor : UnityEditor.Editor
    {
        private static readonly string[] ExperienceAnimation =
        {
            "npcExperienceBarAnimationSpeed"
        };

        private static readonly string[] ButtonAnimation =
        {
            "smoothButtonAnimationEnabled",
            "buttonHoverScale",
            "buttonHoverPunchScale",
            "buttonPressedScale",
            "buttonClickBounceScale",
            "buttonHoverPunchDuration",
            "buttonHoverSettleDuration",
            "buttonPressDuration",
            "buttonClickBounceDuration",
            "buttonClickSettleDuration"
        };

        private static readonly string[] PanelAnimation =
        {
            "panelTransitionDuration",
            "panelSlideExtraDistance",
            "shopSlideDirection",
            "rebirthHudSlideDirection",
            "audioMenuSlideDirection",
            "inventoryMenuSlideDirection",
            "npcProgressHudSlideDirection",
            "gemHudSlideDirection",
            "shopMenuButtonSlideDirection",
            "modalSlideDirection",
            "modalBackdropFadeDuration"
        };

        private static readonly string[] RewardPopupAnimation =
        {
            "rewardPopupDuration",
            "rewardPopupRiseDistance",
            "rewardPopupStartScale",
            "rewardPopupPopScale",
            "rewardPopupPopDuration",
            "rewardPopupSettleDuration",
            "rewardPopupFadeStart"
        };

        private static readonly string[] GiftPopupAnimation =
        {
            "giftRewardPopupStartScale",
            "giftRewardPopupPunchScale",
            "giftRewardPopupPopDuration",
            "giftRewardPopupSettleDuration",
            "giftRewardPopupHoldDuration",
            "giftRewardPopupFadeDuration"
        };

        private static readonly string[] ToastAnimation =
        {
            "unlockToastHoldDuration",
            "unlockToastFadeDuration"
        };

        private static readonly string[] RebirthAnimation =
        {
            "rebirthFlashFadeInDuration",
            "rebirthFlashHoldDuration",
            "rebirthFlashFadeOutDuration"
        };

        private static readonly string[] TimedInteraction =
        {
            "resetDataConfirmationDuration"
        };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "Layout, anchors, sizes, fonts, colors and sprites are authored directly in the Scene. " +
                "This asset now exposes animation settings only; changing Scene UI will not be reapplied from here.",
                MessageType.Info);

            DrawSection("Experience Animation", ExperienceAnimation);
            DrawSection("Button Animation", ButtonAnimation);
            DrawSection("Panel Animation", PanelAnimation);
            DrawSection("Reward Popup Animation", RewardPopupAnimation);
            DrawSection("Gift Popup Animation", GiftPopupAnimation);
            DrawSection("Unlock Toast Animation", ToastAnimation);
            DrawSection("Rebirth Flash Animation", RebirthAnimation);
            DrawSection("Timed Interaction", TimedInteraction);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSection(string title, string[] propertyNames)
        {
            EditorGUILayout.Space(7f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            foreach (string propertyName in propertyNames)
            {
                SerializedProperty property = serializedObject.FindProperty(propertyName);
                if (property != null)
                {
                    EditorGUILayout.PropertyField(property, includeChildren: true);
                }
            }
        }
    }
}
#endif
