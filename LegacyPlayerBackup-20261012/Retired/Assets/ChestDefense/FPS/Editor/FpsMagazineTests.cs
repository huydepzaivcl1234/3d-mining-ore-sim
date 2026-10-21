#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;

namespace ChestDefense.Fps.Tests
{
    public sealed class FpsMagazineTests
    {
        [Test] public void AutomaticRateCannotConsumeExtraAmmoInSameFrame()
        {
            var m = new FpsMagazine(12, 100, 12, 20);
            Assert.IsTrue(m.TryFire(1, .1)); Assert.IsFalse(m.TryFire(1.05, .1));
            Assert.AreEqual(11, m.Loaded); Assert.IsTrue(m.TryFire(1.11, .1));
        }
        [Test] public void ReloadTransfersOnlyAvailableReserveAndCommitsOnce()
        {
            var m = new FpsMagazine(12, 100, 3, 4);
            Assert.IsTrue(m.BeginReload(1, 2)); Assert.IsFalse(m.TryFire(2, .1));
            Assert.IsFalse(m.CompleteReload(2.99)); Assert.AreEqual(3, m.Loaded);
            Assert.IsTrue(m.CompleteReload(3)); Assert.AreEqual(7, m.Loaded); Assert.AreEqual(0, m.Reserve);
            Assert.IsFalse(m.CompleteReload(4));
        }
        [Test] public void SwitchingOrDyingDuringReloadCannotGrantAmmo()
        {
            var m = new FpsMagazine(12, 100, 1, 20);
            m.BeginReload(0, 2); m.CancelReload();
            Assert.IsFalse(m.CompleteReload(3)); Assert.AreEqual(1, m.Loaded); Assert.AreEqual(20, m.Reserve);
        }
        [Test] public void NewCapacitiesConstrainExistingSaveWithoutNegativeAmmo()
        {
            var m = new FpsMagazine(8, 30, 12, 60);
            Assert.AreEqual(8, m.Loaded); Assert.AreEqual(30, m.Reserve);
            Assert.AreEqual(0, m.AddReserve(10)); Assert.IsFalse(m.BeginReload(0, 1));
        }
        [Test] public void EmptyReserveCannotReloadAndPickupReportsAcceptedAmount()
        {
            var m = new FpsMagazine(5, 10, 0, 0);
            Assert.IsFalse(m.TryFire(0, .1)); Assert.IsFalse(m.BeginReload(0, 1));
            Assert.AreEqual(10, m.AddReserve(15)); Assert.AreEqual(0, m.AddReserve(-1));
            Assert.IsTrue(m.BeginReload(0, 1)); Assert.IsTrue(m.CompleteReload(1));
            Assert.AreEqual(5, m.Loaded); Assert.AreEqual(5, m.Reserve);
        }
        [Test] public void CameraRayUsesNearestWallInsteadOfMonsterBehindIt()
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var owner = new GameObject("owner");
            try
            {
                Vector3 origin = Vector3.one * 10000;
                wall.transform.position = origin + Vector3.forward * 2; target.transform.position = origin + Vector3.forward * 4;
                Physics.SyncTransforms();
                Assert.IsTrue(FpsHitscan.Closest(new Ray(origin, Vector3.forward), 10, ~0, owner.transform,
                    null, new RaycastHit[16], out var hit, out var saturated));
                Assert.IsFalse(saturated); Assert.AreEqual(wall.GetComponent<Collider>(), hit.collider);
            }
            finally { Object.DestroyImmediate(wall); Object.DestroyImmediate(target); Object.DestroyImmediate(owner); }
        }
    }
}
#endif
