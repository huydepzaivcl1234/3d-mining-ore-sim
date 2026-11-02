using MiningSimulator.Ores;
using UnityEngine;

namespace GameData.Player.Script
{
    /// <summary>Distance-driven footfalls; reuses the existing positional SFX pool and volume settings.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerController1))]
    public sealed class PlayerFootsteps : MonoBehaviour
    {
        [SerializeField] private MiningAudioManager audioManager;
        [SerializeField] private AudioClip[] grassClips = System.Array.Empty<AudioClip>();
        [SerializeField] private Transform leftFoot, rightFoot;
        [SerializeField, Min(.1f)] private float walkStepMeters = 1.2f;
        [SerializeField, Min(.1f)] private float runStepMeters = 1.6f;
        [SerializeField, Min(.05f)] private float minimumInterval = .18f;
        [SerializeField, Range(0f, 1f)] private float volume = .55f;
        private PlayerController1 controller;
        private GameData.Player.Manager.InputManager input;
        private AnimationRunMana stamina;
        private MiningCharacterHealth health;
        private Vector3 previousPosition;
        private float travelled, nextStep;
        private int previousClip = -1;
        private bool right;
        public int StepsPlayed { get; private set; }

        private void Awake()
        {
            controller = GetComponent<PlayerController1>();
            input = GetComponent<GameData.Player.Manager.InputManager>();
            stamina = GetComponent<AnimationRunMana>();
            health = GetComponent<MiningCharacterHealth>();
            if (!audioManager) audioManager = FindAnyObjectByType<MiningAudioManager>();
        }
        private void OnEnable() { previousPosition = transform.position; travelled = 0f; nextStep = 0f; }
        private void Update()
        {
            Vector3 delta = transform.position - previousPosition;
            previousPosition = transform.position;
            delta.y = 0f;
            float dt = Time.deltaTime, distance = delta.magnitude;
            if (dt <= 0f || !controller.enabled || !controller.IsGrounded ||
                health && health.Health <= 0f || input && input.Move.sqrMagnitude < .01f ||
                distance > Mathf.Max(2f, dt * 20f))
            { travelled = 0f; return; }
            // Standing still or pushing into a wall cannot generate footsteps.
            if (distance < .0001f) return;
            bool running = input && input.Run && (!stamina || stamina.CanRun);
            float stride = running ? runStepMeters : walkStepMeters;
            travelled += distance;
            if (travelled < Mathf.Max(.1f, stride) || Time.time < nextStep) return;
            travelled = Mathf.Repeat(travelled, Mathf.Max(.1f, stride));
            if (!audioManager || grassClips.Length == 0) return;
            int index = grassClips.Length == 1 ? 0 : Random.Range(0, grassClips.Length - 1);
            if (grassClips.Length > 1 && index >= previousClip) index++;
            var clip = grassClips[index];
            if (!clip) return;
            previousClip = index;
            var foot = right ? rightFoot : leftFoot;
            audioManager.PlayWorldSfx(clip, foot ? foot.position : transform.position, volume);
            right = !right;
            nextStep = Time.time + Mathf.Max(.05f, minimumInterval);
            StepsPlayed++;
        }
    }
}
