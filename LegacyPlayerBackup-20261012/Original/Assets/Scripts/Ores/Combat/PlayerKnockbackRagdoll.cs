using System.Collections.Generic;
using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MiningSimulator.Ores
{
    /// <summary>Living knockback uses the capsule; only lethal damage hands the mesh to physics.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Animator), typeof(CharacterController), typeof(MiningCharacterHealth))]
    [DefaultExecutionOrder(-200)]
    public sealed class PlayerKnockbackRagdoll : MonoBehaviour
    {
        public enum KnockdownState { Normal, Knockback, DeadRagdoll, DeadHeld }
        [Min(.1f), SerializeField] private float knockbackSeconds = .5f;
        [Min(.1f), SerializeField] private float minimumFallSeconds = .4f;
        [Min(.1f), SerializeField] private float settledSeconds = .35f;
        [Min(.1f), SerializeField] private float settledSpeed = .8f;
        [SerializeField] private LayerMask groundLayers = ~0;
        public KnockdownState State { get; private set; }
        public bool IsIncapacitated => State != KnockdownState.Normal;
        public bool HasLanded { get; private set; }
        public Vector3 CameraFocusPosition => IsDeadPose && hips != null ? hips.position + Vector3.up * .4f : transform.position + Vector3.up;
        private bool IsDeadPose => State == KnockdownState.DeadRagdoll || State == KnockdownState.DeadHeld;
        private Animator animator;
        private CharacterController capsule;
        private MiningCharacterHealth health;
        private ThirdPersonController motor;
        private PlayerCombatInput combat;
        private MiningHitReaction reaction;
        private PlayerInput playerInput;
        private StarterAssetsInputs inputs;
        private Transform hips;
        private readonly List<Rigidbody> bodies = new();
        private readonly List<Collider> ragdollColliders = new();
        private readonly RaycastHit[] hits = new RaycastHit[64];
        private Transform[] poseBones;
        private Vector3[] animatedLocalPositions;
        private Quaternion[] animatedLocalRotations;
        private bool motorEnabled, combatEnabled, reactionEnabled, inputActive, ready;
        private float knockbackAge, fallAge, settleAge;
        private Vector3 knockbackVelocity;
        private void Awake()
        {
            animator = GetComponent<Animator>();
            capsule = GetComponent<CharacterController>();
            health = GetComponent<MiningCharacterHealth>();
            motor = GetComponent<ThirdPersonController>();
            combat = GetComponent<PlayerCombatInput>();
            reaction = GetComponent<MiningHitReaction>();
            playerInput = GetComponent<PlayerInput>();
            inputs = GetComponent<StarterAssetsInputs>();
            if (!animator.isHuman) { Debug.LogWarning("Death ragdoll needs the player's Humanoid rig.", this); return; }
            hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            if (hips == null) return;
            poseBones = hips.GetComponentsInChildren<Transform>();
            animatedLocalPositions = new Vector3[poseBones.Length];
            animatedLocalRotations = new Quaternion[poseBones.Length];
            for (int i = 0; i < poseBones.Length; i++)
            { animatedLocalPositions[i] = poseBones[i].localPosition; animatedLocalRotations[i] = poseBones[i].localRotation; }
            AddBody(HumanBodyBones.Hips, HumanBodyBones.Spine, .12f, 10f);
            AddBody(HumanBodyBones.Chest, HumanBodyBones.Neck, .13f, 12f);
            AddBody(HumanBodyBones.Head, HumanBodyBones.LastBone, .10f, 4f);
            AddBody(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, .065f, 3f);
            AddBody(HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, .055f, 2f);
            AddBody(HumanBodyBones.LeftHand, HumanBodyBones.LeftMiddleProximal, .045f, .7f);
            AddBody(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, .065f, 3f);
            AddBody(HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, .055f, 2f);
            AddBody(HumanBodyBones.RightHand, HumanBodyBones.RightMiddleProximal, .045f, .7f);
            AddBody(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, .085f, 6f);
            AddBody(HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, .06f, 4f);
            AddBody(HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes, .05f, 1f);
            AddBody(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, .085f, 6f);
            AddBody(HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, .06f, 4f);
            AddBody(HumanBodyBones.RightFoot, HumanBodyBones.RightToes, .05f, 1f);
            foreach (var body in bodies)
            {
                if (body.transform == hips) continue;
                var parent = body.transform.parent;
                Rigidbody connected = null;
                while (parent != null && parent != transform)
                {
                    connected = parent.GetComponent<Rigidbody>();
                    if (connected != null) break;
                    parent = parent.parent;
                }
                if (connected == null) continue;
                var joint = body.gameObject.AddComponent<CharacterJoint>();
                joint.connectedBody = connected;
                joint.anchor = Vector3.zero;
                joint.lowTwistLimit = new SoftJointLimit { limit = -45f };
                joint.highTwistLimit = new SoftJointLimit { limit = 45f };
                joint.swing1Limit = new SoftJointLimit { limit = 65f };
                joint.swing2Limit = new SoftJointLimit { limit = 40f };
                joint.enableProjection = true;
                joint.projectionDistance = .05f;
            }
            SetPhysics(false);
            ready = bodies.Count >= 11;
        }
        private void AddBody(HumanBodyBones id, HumanBodyBones endId, float radius, float mass)
        {
            var bone = animator.GetBoneTransform(id);
            if (bone == null) return;
            // Do not take ownership of somebody else's authored physics rig.
            if (bone.GetComponent<Rigidbody>() != null) return;
            var end = endId != HumanBodyBones.LastBone ? animator.GetBoneTransform(endId) : null;
            Vector3 delta = end != null ? end.position - bone.position : bone.up * .20f;
            if (delta.sqrMagnitude < .001f) delta = bone.up * .12f;
            var shape = new GameObject("Knockdown Collider").transform;
            shape.SetParent(bone, false);
            shape.position = bone.position + delta * .5f;
            shape.rotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
            var collider = shape.gameObject.AddComponent<CapsuleCollider>();
            float scale = Mathf.Max(.001f, Mathf.Abs(shape.lossyScale.y));
            collider.radius = Mathf.Min(radius, delta.magnitude * .3f) / scale;
            collider.height = Mathf.Max(collider.radius * 2f, delta.magnitude * .8f / scale);
            collider.enabled = false;
            shape.gameObject.layer = gameObject.layer;
            var body = bone.gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.mass = mass;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.solverIterations = 12;
            body.solverVelocityIterations = 8;
            body.maxDepenetrationVelocity = 3f;
            body.gameObject.AddComponent<PlayerRagdollGroundContact>().Initialize(this);
            bodies.Add(body);
            ragdollColliders.Add(collider);
        }

        public bool ApplyKnockback(Vector3 velocity)
        {
            if (!isActiveAndEnabled || !Application.isPlaying || Time.timeScale <= 0f ||
                health == null || health.Health <= 0f || IsDeadPose || !Finite(velocity) || !capsule.enabled) return false;
            if (State == KnockdownState.Normal)
            {
                motorEnabled = motor != null && motor.enabled;
                combatEnabled = combat != null && combat.enabled;
                reactionEnabled = reaction != null && reaction.enabled;
                inputActive = playerInput != null && playerInput.inputIsActive;
                if (motor != null) motor.enabled = false;
                if (combat != null) combat.enabled = false;
                if (reaction != null) reaction.enabled = false;
                if (playerInput != null) playerInput.DeactivateInput();
            }
            ClearInput();
            knockbackVelocity = velocity;
            knockbackAge = 0f;
            State = KnockdownState.Knockback;
            return true;
        }

        // PlayerDeathRespawn owns all gameplay locks and their restoration.
        public bool BeginDeathRagdoll(Vector3 velocity)
        {
            if (!ready || !Application.isPlaying || !isActiveAndEnabled || health.Health > 0f || !Finite(velocity)) return false;
            capsule.enabled = false;
            animator.enabled = false;
            SetPhysics(true);
            foreach (var body in bodies) { body.WakeUp(); body.linearVelocity = velocity; }
            Vector3 horizontal = Vector3.ProjectOnPlane(velocity, Vector3.up);
            bodies[0].AddTorque(Vector3.Cross(Vector3.up, horizontal.normalized) * 2f, ForceMode.VelocityChange);
            fallAge = settleAge = 0f;
            HasLanded = false;
            State = KnockdownState.DeadRagdoll;
            return true;
        }
        private static bool Finite(Vector3 v) => !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) &&
            !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);

        internal void RegisterGroundContact(Collision collision)
        {
            // Ignore the feet brushing the floor during the initial upward launch.
            if (State != KnockdownState.DeadRagdoll || HasLanded || fallAge < .1f ||
                bodies[0].linearVelocity.y > .5f || collision.transform.IsChildOf(transform) ||
                (groundLayers.value & (1 << collision.gameObject.layer)) == 0) return;
            for (int i = 0; i < collision.contactCount; i++)
                if (collision.GetContact(i).normal.y > .3f) { HasLanded = true; return; }
        }

        private void SetPhysics(bool active)
        {
            foreach (var body in bodies)
            {
                if (!active && !body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
                body.isKinematic = !active;
                body.detectCollisions = active;
                // Physics interpolation must not keep writing last ragdoll poses over Animator.
                body.interpolation = active ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
            }
            foreach (var c in ragdollColliders) c.enabled = active;
            if (!active) return;
            var own = GetComponentsInChildren<Collider>(true);
            // Reapply per-instance ignores on every activation; never alter global layer rules.
            foreach (var c in ragdollColliders)
                foreach (var other in own)
                    if (c != other) Physics.IgnoreCollision(c, other, true);
        }

        private void Update()
        {
            if (State != KnockdownState.Knockback || Time.timeScale <= 0f) return;
            ClearInput();
            knockbackAge += Time.deltaTime;
            knockbackVelocity.y += Physics.gravity.y * Time.deltaTime;
            if (capsule.enabled) capsule.Move(knockbackVelocity * Time.deltaTime);
            if (knockbackAge >= knockbackSeconds && capsule.isGrounded) CancelForDeath();
        }
        private void FixedUpdate()
        {
            if (State != KnockdownState.DeadRagdoll || Time.timeScale <= 0f) return;
            fallAge += Time.fixedDeltaTime;
            bool slow = true;
            foreach (var body in bodies)
                if (body.linearVelocity.sqrMagnitude > settledSpeed * settledSpeed) { slow = false; break; }
            int count = Physics.RaycastNonAlloc(hips.position + Vector3.up * .1f, Vector3.down, hits, .8f,
                groundLayers, QueryTriggerInteraction.Ignore);
            bool grounded = false;
            for (int i = 0; i < count; i++)
                if (!hits[i].transform.IsChildOf(transform) && hits[i].normal.y > .3f) { grounded = true; break; }
            settleAge = grounded && slow && fallAge >= minimumFallSeconds ? settleAge + Time.fixedDeltaTime : 0f;
            if (settleAge < settledSeconds) return;
            SetPhysics(false); // Freeze the actual landed pose, do not rebind or play an animation.
            State = KnockdownState.DeadHeld;
        }
        public void CancelForDeath()
        {
            if (State != KnockdownState.Knockback) return;
            State = KnockdownState.Normal;
            ClearInput();
            if (motor != null) { motor.ResetMotionAfterRespawn(); motor.enabled = motorEnabled; }
            if (combat != null) combat.enabled = combatEnabled;
            if (reaction != null) reaction.enabled = reactionEnabled;
            if (playerInput != null && inputActive && playerInput.isActiveAndEnabled)
                foreach (var player in PlayerInput.all)
                    if (player == playerInput) { playerInput.ActivateInput(); break; }
        }
        public void ResetForRespawn()
        {
            if (State == KnockdownState.Knockback) CancelForDeath();
            SetPhysics(false);
            if (poseBones != null) for (int i = 0; i < poseBones.Length; i++)
            { poseBones[i].localPosition = animatedLocalPositions[i]; poseBones[i].localRotation = animatedLocalRotations[i]; }
            if (animator != null) animator.enabled = true;
            HasLanded = false;
            State = KnockdownState.Normal;
        }
        private void ClearInput()
        {
            if (inputs == null) return;
            inputs.move = Vector2.zero; inputs.jump = inputs.sprint = false;
        }
        private void OnDisable() { if (IsIncapacitated) ResetForRespawn(); }
    }
}
