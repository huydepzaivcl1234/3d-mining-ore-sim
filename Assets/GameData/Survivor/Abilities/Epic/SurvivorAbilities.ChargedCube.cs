using System.Collections.Generic;
using UnityEngine;
using MiningSimulator.Ores;

namespace GameData.Player.Script
{
    public sealed partial class SurvivorAbilities
    {
        private sealed class CubeShot
        {
            public GameObject visual;
            public CharacterHealth target;
            public Vector3 destination;
            public float remaining, damage, radius;
            public CombatDamageType type;
            public bool active;
        }
        private readonly List<CubeShot> cubeShots=new();
        private readonly List<CharacterHealth> cubeVictims=new();
        private GameObject chargingCube;
        private Material cubeMaterial;
        private float cubeCharge;
        public int CubesLaunched {get;private set;}
        public float CubeChargeProgress=>Mathf.Clamp01(cubeCharge/Mathf.Max(.1f,Settings.epic.chargedCube.chargeSeconds));
        public int ActiveCubeProjectiles=>cubeShots.FindAll(s=>s.active).Count;
        private float CubeSizeMultiplier=>1+Mathf.Max(0,stats?stats.UpgradeBonus(SurvivorUpgradeId.ProjectileSize):0)*.01f;
        public float CubeBlastRadius=>Settings.epic.chargedCube.radius.At(Level(SurvivorUpgradeId.ChargedCube))*CubeSizeMultiplier;
        public float CubeFullSize=>Settings.epic.chargedCube.fullSize*(1+Settings.epic.chargedCube.sizePercent.At(Level(SurvivorUpgradeId.ChargedCube))*.01f)*CubeSizeMultiplier;

        private GameObject CreateCube(string name)
        {
            if(!cubeMaterial){cubeMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));cubeMaterial.name="Arcane Cube (runtime)";}
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;
            var collider=go.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
            go.GetComponent<Renderer>().sharedMaterial=cubeMaterial;
            return go;
        }
        private static Vector3 CubeTargetPoint(CharacterHealth target)
        {
            var collider=target.GetComponent<Collider>();
            return collider?collider.bounds.center:target.transform.position+Vector3.up*.6f;
        }
        private void TickCube(float dt)
        {
            dt=Mathf.Max(0,dt);
            int level=Level(SurvivorUpgradeId.ChargedCube);
            if(level<=0){ResetCube();return;}
            StepCubeShots(dt);
            var rule=Settings.epic.chargedCube;
            if(!chargingCube)chargingCube=CreateCube("Arcane Cube - charging");
            chargingCube.SetActive(true);
            cubeMaterial.color=rule.color;
            cubeCharge=Mathf.Min(Mathf.Max(.1f,rule.chargeSeconds),cubeCharge+dt);
            chargingCube.transform.position=transform.TransformPoint(rule.chargeOffset);
            chargingCube.transform.localScale=Vector3.one*(Mathf.Lerp(rule.startingSize,CubeFullSize,CubeChargeProgress)/Mathf.Max(1,rule.chargeSizeDivisor));
            chargingCube.transform.Rotate(Vector3.up,rule.rotationDegreesPerSecond*dt,Space.World);
            if(CubeChargeProgress<1)return;
            var target=Nearest(transform.position,combat.AttackRange,null);if(!target)return;
            CubeShot shot=cubeShots.Find(s=>!s.active);
            if(shot==null){shot=new CubeShot{visual=CreateCube("Arcane Cube - projectile")};cubeShots.Add(shot);}
            shot.target=target;shot.destination=CubeTargetPoint(target);shot.damage=rule.damage.At(level);shot.radius=CubeBlastRadius;shot.type=rule.damageType;
            shot.visual.transform.SetPositionAndRotation(chargingCube.transform.position,chargingCube.transform.rotation);
            shot.visual.transform.localScale=Vector3.one*CubeFullSize;shot.visual.SetActive(true);shot.active=true;
            shot.remaining=Mathf.Max(.01f,Vector3.Distance(shot.visual.transform.position,shot.destination)/Mathf.Max(.01f,combat.ProjectileSpeed));
            CubesLaunched++;cubeCharge=0;chargingCube.SetActive(false);
        }
        private void StepCubeShots(float dt)
        {
            foreach(var shot in cubeShots){if(!shot.active)continue;
                if(!shot.target||!shot.target.IsTargetable)shot.target=Nearest(shot.visual.transform.position,combat.AttackRange,null);
                if(shot.target)shot.destination=CubeTargetPoint(shot.target);
                shot.visual.transform.position=Vector3.Lerp(shot.visual.transform.position,shot.destination,Mathf.Clamp01(dt/Mathf.Max(.0001f,shot.remaining)));
                shot.remaining-=dt;if(shot.remaining>0)continue;
                shot.active=false;shot.visual.SetActive(false);cubeVictims.Clear();
                foreach(var target in CharacterHealth.ActiveMonsters)
                    if(target&&target.isActiveAndEnabled&&target.IsTargetable&&(CubeTargetPoint(target)-shot.destination).sqrMagnitude<=shot.radius*shot.radius)cubeVictims.Add(target);
                foreach(var target in cubeVictims)if(target&&target.IsTargetable)target.DealDamageWithTrueBonus(shot.damage,shot.type,gameObject,0,false,false,true);
            }
        }
        private void ResetCube()
        {
            cubeCharge=0;if(chargingCube)chargingCube.SetActive(false);
            foreach(var shot in cubeShots){shot.active=false;if(shot.visual)shot.visual.SetActive(false);}
        }
        private void DestroyCube()
        {
            if(chargingCube)Destroy(chargingCube);
            foreach(var shot in cubeShots)if(shot.visual)Destroy(shot.visual);
            if(cubeMaterial)Destroy(cubeMaterial);
        }
    }
}
