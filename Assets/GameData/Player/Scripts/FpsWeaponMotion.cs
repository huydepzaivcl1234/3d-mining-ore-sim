using UnityEngine;

namespace GameData.Player.Script
{
    /// <summary>Independent visual target solver; no transforms, input polling or per-frame allocation.</summary>
    public sealed class FpsWeaponMotion
    {
        private Vector3 position, positionVelocity, euler, rotationVelocity;
        private float phase;
        public Vector3 Position => position;
        public Vector3 Euler => euler;

        public void Reset()
        {
            position = positionVelocity = euler = rotationVelocity = Vector3.zero;
            phase = 0f;
        }

        public void Step(FpsWeaponMotionProfile profile, Vector2 lookDegreesPerSecond,
            Vector2 moveVelocity, bool grounded, float aimWeight, float dt)
        {
            if (!profile || dt <= 0f) return;
            // Do not turn a teleport, editor pause or stalled frame into a large visual impulse.
            if (dt > .2f) { Reset(); return; }
            Vector2 look = Vector2.ClampMagnitude(lookDegreesPerSecond, Mathf.Max(0f, profile.maxLookSpeed));
            Vector2 movement = Vector2.ClampMagnitude(moveVelocity / Mathf.Max(.1f, profile.fullMoveSpeed), 1f);
            float speed = moveVelocity.magnitude;
            float walk = Mathf.Max(.1f, profile.walkMoveSpeed);
            float running = Mathf.Clamp01((speed - walk) / Mathf.Max(.1f, profile.fullMoveSpeed - walk));
            float intensity = grounded ? Mathf.Clamp01(speed / walk) * Mathf.Lerp(1f, profile.runMultiplier, running) : 0f;
            phase = Mathf.Repeat(phase + dt * Mathf.Lerp(profile.walkFrequency, profile.runFrequency, running) * Mathf.PI * 2f, Mathf.PI * 2f);
            Vector3 targetPosition = new Vector3(-look.x, look.y, 0f) * profile.lookPositionScale;
            targetPosition += new Vector3(-movement.x * profile.movementPosition.x, -Mathf.Abs(movement.y) * profile.movementPosition.y, 0f);
            targetPosition += new Vector3(Mathf.Sin(phase) * profile.bobAmplitude.x, Mathf.Sin(2f * phase) * profile.bobAmplitude.y, 0f) * intensity;
            Vector3 targetRotation = new Vector3(look.y, -look.x, -movement.x * profile.strafeRoll);
            targetRotation.x *= profile.lookRotationScale;
            targetRotation.y *= profile.lookRotationScale;
            targetRotation.z += Mathf.Sin(phase) * profile.bobRoll * intensity;
            targetPosition = Vector3.ClampMagnitude(targetPosition, Mathf.Max(0f, profile.maxPosition));
            targetRotation = Vector3.ClampMagnitude(targetRotation, Mathf.Max(0f, profile.maxRotation));
            float weight = Mathf.Lerp(1f, profile.adsMotionMultiplier, Mathf.Clamp01(aimWeight));
            float frequency = 4f / Mathf.Max(.02f, profile.responseSeconds);
            StepSpring(ref position, ref positionVelocity, targetPosition * weight, frequency, dt);
            StepSpring(ref euler, ref rotationVelocity, targetRotation * weight, frequency, dt);
            position = Vector3.ClampMagnitude(position, Mathf.Max(0f, profile.maxPosition));
            euler = Vector3.ClampMagnitude(euler, Mathf.Max(0f, profile.maxRotation));
        }

        public static Vector2 LookVelocity(Quaternion previous, Quaternion current, float dt)
        {
            if (dt <= 0f || dt > .2f) return Vector2.zero;
            Vector3 delta = (Quaternion.Inverse(previous) * current).eulerAngles;
            return new Vector2(Mathf.DeltaAngle(0f, delta.y), Mathf.DeltaAngle(0f, delta.x)) / dt;
        }

        /// <summary>Exact critically damped response for a target held constant during this timestep.</summary>
        public static void StepSpring(ref Vector3 value, ref Vector3 velocity, Vector3 target, float frequency, float dt)
        {
            if (dt <= 0f) return;
            frequency = Mathf.Max(.0001f, frequency);
            Vector3 error = value - target;
            Vector3 term = velocity + frequency * error;
            float decay = Mathf.Exp(-frequency * dt);
            value = target + (error + term * dt) * decay;
            velocity = (velocity - frequency * term * dt) * decay;
        }
    }
}
