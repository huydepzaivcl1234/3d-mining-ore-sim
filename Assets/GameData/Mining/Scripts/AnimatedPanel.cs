using System.Collections;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Shared opening motion; each panel still owns its content and close action.</summary>
    public abstract class AnimatedPanel : MonoBehaviour
    {
        protected abstract RectTransform Body { get; }
        protected abstract float OpenDuration { get; }
        protected abstract float OpenStartScale { get; }

        private Vector3 restingScale;
        private Coroutine opening;

        protected virtual void Awake()
        {
            if (Body != null) restingScale = Body.localScale;
        }

        protected virtual void OnEnable()
        {
            if (Body != null) opening = StartCoroutine(AnimateOpen());
        }

        protected virtual void OnDisable()
        {
            if (opening != null) StopCoroutine(opening);
            opening = null;
            if (Body != null) Body.localScale = restingScale;
        }

        private IEnumerator AnimateOpen()
        {
            float duration = Mathf.Max(0.01f, OpenDuration);
            Vector3 start = restingScale * OpenStartScale;
            Body.localScale = start;
            for (float elapsed = 0f; elapsed < duration;)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration) - 1f;
                float eased = 1f + 2.70158f * t * t * t + 1.70158f * t * t;
                Body.localScale = Vector3.LerpUnclamped(start, restingScale, eased);
                yield return null;
            }
            Body.localScale = restingScale;
            opening = null;
        }
    }
}
