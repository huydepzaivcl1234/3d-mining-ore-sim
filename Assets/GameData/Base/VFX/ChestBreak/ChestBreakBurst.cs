using System.Collections;
using UnityEngine;

namespace MiningGame.Vfx
{
    [DisallowMultipleComponent]
    public sealed class ChestBreakBurst : MonoBehaviour
    {
        [SerializeField] private ParticleSystem flash;
        [SerializeField] private ParticleSystem smoke;
        [SerializeField] private ParticleSystem splinters;
        [SerializeField, Range(1, 48)] private int smokeCount = 28;
        [SerializeField, Range(0, 32)] private int splinterCount = 18;
        [SerializeField, Min(.1f)] private float size = 1f;

        private void OnEnable()
        {
            Prepare(flash); Prepare(smoke); Prepare(splinters);
            if (flash != null) flash.Emit(new ParticleSystem.EmitParams {
                position = transform.position, startLifetime = .22f,
                startSize = 1.35f * size, startColor = new Color(1, .8f, .35f, 1)
            }, 1);
            if (smoke != null)
                for (int i = 0; i < smokeCount; i++)
                {
                    Vector2 radial = Random.insideUnitCircle;
                    smoke.Emit(new ParticleSystem.EmitParams {
                        position = transform.position + new Vector3(radial.x, Random.Range(-.15f, .12f), radial.y) * .35f * size,
                        velocity = new Vector3(radial.x * .8f, Random.Range(.45f, 1.1f), radial.y * .8f) * size,
                        startLifetime = Random.Range(1.4f, 2.2f), startSize = Random.Range(.4f, .85f) * size,
                        rotation = Random.Range(0f, 360f), angularVelocity = Random.Range(-25f, 25f),
                        startColor = new Color(.4f, .34f, .28f, Random.Range(.45f, .7f))
                    }, 1);
                }
            if (splinters != null)
                for (int i = 0; i < splinterCount; i++)
                {
                    Vector2 radial = Random.insideUnitCircle.normalized;
                    splinters.Emit(new ParticleSystem.EmitParams {
                        position = transform.position + new Vector3(radial.x, 0, radial.y) * .15f * size,
                        velocity = new Vector3(radial.x * Random.Range(1f, 2.3f), Random.Range(2f, 3.7f), radial.y * Random.Range(1f, 2.3f)) * size,
                        startLifetime = Random.Range(.85f, 1.5f),
                        startSize3D = new Vector3(Random.Range(.08f, .17f), .025f, .035f) * size,
                        rotation3D = Random.insideUnitSphere * 180,
                        angularVelocity3D = Random.insideUnitSphere * 300,
                        startColor = new Color(.45f, .22f, .08f, 1)
                    }, 1);
                }
            StartCoroutine(Cleanup());
        }

        private static void Prepare(ParticleSystem system)
        {
            if (system == null) return;
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            system.Play(false);
        }

        private IEnumerator Cleanup() { yield return new WaitForSeconds(2.7f); Destroy(gameObject); }
        private void OnDisable() => StopAllCoroutines();
    }
}
