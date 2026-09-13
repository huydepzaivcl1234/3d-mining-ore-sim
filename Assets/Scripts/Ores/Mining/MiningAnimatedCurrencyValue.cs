using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Shared smooth counter state used by Money and Gem presentations.</summary>
    public sealed class MiningAnimatedCurrencyValue
    {
        private float target;
        private float speed;

        public float Value { get; private set; }

        public void Initialize(float amount)
        {
            Value = Mathf.Max(0f, amount);
            target = Value;
            speed = 0f;
        }

        public void SetTarget(float amount, MiningGameData data)
        {
            target = Mathf.Max(0f, amount);
            if (data == null)
            {
                Value = target;
                speed = 0f;
                return;
            }

            float difference = Mathf.Abs(target - Value);
            float durationLimitedSpeed = difference / data.CurrencyCountMaximumDuration;
            speed = Mathf.Max(data.CurrencyCountUnitsPerSecond, durationLimitedSpeed);
        }

        public bool Tick(float unscaledDeltaTime)
        {
            if (Mathf.Approximately(Value, target))
            {
                Value = target;
                return false;
            }

            Value = Mathf.MoveTowards(Value, target,
                Mathf.Max(0f, unscaledDeltaTime) * speed);
            return true;
        }
    }
}
