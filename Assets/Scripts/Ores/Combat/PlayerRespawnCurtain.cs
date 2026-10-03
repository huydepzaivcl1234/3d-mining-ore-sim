using System.Collections;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Screen-space shutters. Respawn remains owned by PlayerDeathRespawn.</summary>
    public sealed class PlayerRespawnCurtain : MonoBehaviour
    {
        [SerializeField] private RectTransform upperCurtain;
        [SerializeField] private RectTransform lowerCurtain;
        [Min(.05f), SerializeField] private float closeSeconds = .55f;
        [Min(.05f), SerializeField] private float openSeconds = .7f;
        private float coverage;
        private bool closing;
        private Coroutine motion;
        public float CloseSeconds => Mathf.Max(.05f, closeSeconds);
        public bool IsCovered => upperCurtain == null || lowerCurtain == null || coverage >= .999f;

        private void OnEnable() => ResetImmediate();
        private void OnDisable() => ResetImmediate();
        public void Close()
        {
            if (closing || coverage >= 1f || !isActiveAndEnabled) return;
            closing = true;
            AnimateTo(1f, CloseSeconds);
        }
        public void Open()
        {
            closing = false;
            if (!isActiveAndEnabled) { ResetImmediate(); return; }
            AnimateTo(0f, Mathf.Max(.05f, openSeconds));
        }
        public void ResetImmediate()
        {
            if (motion != null) StopCoroutine(motion);
            motion = null;
            closing = false;
            coverage = 0f;
            ApplyPosition();
        }
        private void AnimateTo(float destination, float seconds)
        {
            if (motion != null) StopCoroutine(motion);
            motion = StartCoroutine(Animate(destination, seconds));
        }
        private IEnumerator Animate(float destination, float seconds)
        {
            float from = coverage, elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / seconds);
                coverage = Mathf.Lerp(from, destination, t * t * (3f - 2f * t));
                ApplyPosition();
                yield return null;
            }
            coverage = destination;
            ApplyPosition();
            motion = null;
        }
        private void ApplyPosition()
        {
            if (upperCurtain == null || lowerCurtain == null) return;
            float travel = ((RectTransform)transform).rect.height * .5f + 2f;
            upperCurtain.anchoredPosition = new Vector2(0f, travel * (1f - coverage));
            lowerCurtain.anchoredPosition = new Vector2(0f, -travel * (1f - coverage));
            bool visible = coverage > 0f;
            upperCurtain.gameObject.SetActive(visible);
            lowerCurtain.gameObject.SetActive(visible);
        }
    }
}
