using System.Collections;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Unscaled presentation motion; never changes gameplay or saved data.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningPanelMotion : MonoBehaviour
    {
        [SerializeField, Min(.01f)] private float duration = .22f;
        [SerializeField, Range(.8f, 1f)] private float closedScale = .94f;
        [SerializeField] private bool animateAlpha;
        private Coroutine routine;
        private Vector3 homeScale;
        private CanvasGroup group;
        private bool cached;

        private void Cache()
        {
            if (cached) return;
            homeScale = transform.localScale;
            group = GetComponent<CanvasGroup>();
            if (animateAlpha && group == null) group = gameObject.AddComponent<CanvasGroup>();
            cached = true;
        }
        public void PlayOpen()
        {
            Cache();
            if (!isActiveAndEnabled) return;
            if (routine != null) StopCoroutine(routine);
            transform.localScale = homeScale * closedScale;
            if (animateAlpha) { group.alpha = 0f; group.blocksRaycasts = group.interactable = false; }
            routine = StartCoroutine(Animate(true, false));
        }
        public void PlayClose(bool deactivate = true)
        {
            Cache();
            if (!isActiveAndEnabled) { if (deactivate) gameObject.SetActive(false); return; }
            if (routine != null) StopCoroutine(routine);
            if (animateAlpha) group.blocksRaycasts = group.interactable = false;
            routine = StartCoroutine(Animate(false, deactivate));
        }
        private IEnumerator Animate(bool opening, bool deactivate)
        {
            Vector3 from = transform.localScale, target = homeScale * (opening ? 1f : closedScale);
            float fromAlpha = group != null ? group.alpha : 1f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                transform.localScale = Vector3.LerpUnclamped(from, target, eased);
                if (animateAlpha) group.alpha = Mathf.Lerp(fromAlpha, opening ? 1f : 0f, eased);
                yield return null;
            }
            transform.localScale = target;
            routine = null;
            if (animateAlpha) { group.alpha = opening ? 1f : 0f; group.interactable = group.blocksRaycasts = opening; }
            if (deactivate) gameObject.SetActive(false);
        }
        private void OnDisable()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            if (cached) transform.localScale = homeScale;
        }
    }
}
