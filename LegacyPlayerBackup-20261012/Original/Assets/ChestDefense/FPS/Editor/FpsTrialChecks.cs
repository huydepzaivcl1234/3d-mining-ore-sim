using System;

namespace ChestDefense.Fps.Editor
{
    public static class FpsTrialChecks
    {
        public static string Run()
        {
            var m = new FpsMagazine(12, 100, 12, 20);
            Check(m.TryFire(1, .1) && !m.TryFire(1.05, .1) && m.Loaded == 11, "Fire rate");
            m = new FpsMagazine(12, 100, 3, 4);
            Check(m.BeginReload(1, 2) && !m.CompleteReload(2.99) && m.CompleteReload(3), "Reload timing");
            Check(m.Loaded == 7 && m.Reserve == 0 && !m.CompleteReload(4), "Single reload commit");
            m = new FpsMagazine(12, 100, 1, 20); m.BeginReload(0, 2); m.CancelReload();
            Check(!m.CompleteReload(3) && m.Loaded == 1 && m.Reserve == 20, "Cancel preserves ammo");
            m = new FpsMagazine(8, 30, 12, 60);
            Check(m.Loaded == 8 && m.Reserve == 30 && m.AddReserve(10) == 0, "Capacity migration");
            m = new FpsMagazine(5, 10, 0, 0);
            Check(!m.TryFire(0, .1) && !m.BeginReload(0, 1) && m.AddReserve(15) == 10 && m.AddReserve(-1) == 0, "Pickup bounds");
            foreach (var name in new[] { "Pistol", "Rifle" })
            {
                var data = UnityEditor.AssetDatabase.LoadAssetAtPath<FpsWeaponData>("Assets/ChestDefense/FPS/Generated/" + name + ".asset");
                Check(data != null && data.TryValidate(out _), name + " configuration");
                Check(data.viewPrefab.rig != null && data.viewPrefab.muzzle != null, name + " rig/muzzle");
                Check(data.ShotInterval(100) >= data.minimumShotInterval, name + " cooldown survives attack-speed bonuses");
                var fastClicks = new FpsMagazine(100, 0, 100, 0);
                int accepted = 0;
                for (int i = 0; i < 100; i++) if (fastClicks.TryFire(i * .01, data.ShotInterval(100))) accepted++;
                Check(accepted <= Math.Ceiling(1 / data.minimumShotInterval), name + " fast click rate cap");
                var view = UnityEngine.Object.Instantiate(data.viewPrefab);
                try
                {
                    view.transform.SetPositionAndRotation(data.hipPosition, UnityEngine.Quaternion.Euler(data.hipEuler));
                    UnityEngine.Transform barrel = null;
                    foreach (var t in view.rig.GetComponentsInChildren<UnityEngine.Transform>(true))
                        if (t.name == (name == "Pistol" ? "Barrel" : "Root")) { barrel = t; break; }
                    foreach (var clip in new[] { data.idle, data.shoot, data.reload })
                        for (int i = 0; i <= 20; i++)
                        {
                            clip.SampleAnimation(view.rig.gameObject, clip.length * i / 20f);
                            var forward = name == "Pistol" ? barrel.up : -barrel.up;
                            Check(UnityEngine.Vector3.Dot(forward, UnityEngine.Vector3.forward) > 0, name + " action reverses barrel");
                            var tip = view.muzzle.position;
                            Check(tip.z > .1f && tip.z < 2 && Math.Abs(tip.x) < 1 && Math.Abs(tip.y) < 1, name + " action leaves view mount");
                        }
                    Check(view.Initialize(data,null),name+" initialization");
                    for(int i=0;i<300;i++)
                    {
                        view.Tick(true,1f/60);
                        if(i<120)continue;
                        foreach(var sight in new[]{view.rearSight,view.frontSight})
                        {
                            Check(sight!=null,name+" missing sight");
                            var p=sight.position;
                            Check(p.z>.03f && Math.Abs(p.x/p.z)<.02f && Math.Abs(p.y/p.z)<.02f,name+" idle ADS drift");
                        }
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(view.gameObject); }
            }
            return "PASS: ammo/rate/cancel/pickup, cooldown floors at 100x attack speed, 21 pose samples per Idle/Shoot/Reload, and 300 animated ADS ticks for both guns.";
        }
        static void Check(bool ok, string reason) { if (!ok) throw new InvalidOperationException(reason); }
    }
}
