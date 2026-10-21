using System;

namespace ChestDefense.Fps
{
    // No Unity lifecycle: authoritative ammo and reload transactions are testable in isolation.
    public sealed class FpsMagazine
    {
        public int Capacity { get; }
        public int MaximumReserve { get; }
        public int Loaded { get; private set; }
        public int Reserve { get; private set; }
        public bool Reloading { get; private set; }
        public double ReloadStarted { get; private set; }
        public double ReloadEnds { get; private set; }
        double nextShot;
        public FpsMagazine(int capacity, int maximumReserve, int loaded, int reserve)
        {
            if (capacity <= 0 || maximumReserve < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            Capacity = capacity; MaximumReserve = maximumReserve;
            // Migration safety when the designer reduces capacities in an existing save.
            Loaded = Math.Min(capacity, Math.Max(0, loaded));
            Reserve = Math.Min(maximumReserve, Math.Max(0, reserve));
        }
        public bool TryFire(double now, double interval)
        {
            if (Reloading || Loaded <= 0 || now < nextShot || interval <= 0 || double.IsNaN(interval) || double.IsInfinity(interval)) return false;
            Loaded--; nextShot = now + interval; return true;
        }
        public bool BeginReload(double now, double seconds)
        {
            if (Reloading || Loaded >= Capacity || Reserve <= 0 || seconds <= 0 || double.IsNaN(seconds) || double.IsInfinity(seconds)) return false;
            Reloading = true; ReloadStarted = now; ReloadEnds = now + seconds; return true;
        }
        public bool CompleteReload(double now)
        {
            if (!Reloading || now < ReloadEnds) return false;
            int moved = Math.Min(Capacity - Loaded, Reserve);
            Loaded += moved; Reserve -= moved; Reloading = false; return true;
        }
        public void CancelReload() => Reloading = false;
        public int AddReserve(int amount)
        {
            if (amount <= 0) return 0;
            int accepted = Math.Min(amount, MaximumReserve - Reserve); Reserve += accepted; return accepted;
        }
    }
}
