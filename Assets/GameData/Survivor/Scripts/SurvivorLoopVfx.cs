using UnityEngine;
using MiningSimulator.Ores;

namespace GameData.Player.Script
{
    // Owned by its actor; dynamic child effects obey the existing offscreen simulation pause.
    public sealed class SurvivorLoopVfx : MonoBehaviour
    {
        private WorldActorVisibility visibility;
        private ParticleSystem[] particles;
        private Renderer[] renderers;
        private Renderer[] bodyRenderers;
        private SurvivorAbilityData.LoopVisual visual;
        private Vector3 worldScale;
        private ParticleSystem.Particle[] innerParticles;
        private bool paused;
        private void Configure(Transform actor,SurvivorAbilityData.LoopVisual settings)
        {
            visual=settings;if(settings.worldSpace)innerParticles=new ParticleSystem.Particle[256];
            visibility=actor.GetComponent<WorldActorVisibility>();
            particles=GetComponentsInChildren<ParticleSystem>(true);renderers=GetComponentsInChildren<Renderer>(true);
            if(settings.followBodyBounds)bodyRenderers=actor.GetComponentsInChildren<Renderer>();
        }
        public static void Set(ref GameObject instance,SurvivorAbilityData.LoopVisual settings,Transform actor,bool show,float radiusScale=1)
        {
            if(!show||settings==null||!settings.prefab){if(instance)instance.SetActive(false);return;}
            if(!instance){instance=Instantiate(settings.prefab,actor,false);instance.name=settings.prefab.name+" (active effect)";instance.AddComponent<SurvivorLoopVfx>().Configure(actor,settings);}
            instance.transform.localPosition=settings.offset;instance.transform.localRotation=Quaternion.identity;
            instance.transform.localScale=Vector3.Scale(settings.scale,new Vector3(radiusScale,1,radiusScale));
            instance.GetComponent<SurvivorLoopVfx>().worldScale=instance.transform.localScale;
            if(!instance.activeSelf)instance.SetActive(true);
        }
        private void LateUpdate()
        {
            bool hide=visibility&&(!visibility.IsVisible||visibility.SimulationPaused);
            if(!hide&&bodyRenderers!=null){Bounds bounds=default;bool found=false;
                foreach(var r in bodyRenderers)if(r&&r.gameObject.activeInHierarchy&&!(r is ParticleSystemRenderer)&&!r.GetComponent<TMPro.TMP_Text>()&&r.bounds.size.sqrMagnitude>.0001f){if(!found){bounds=r.bounds;found=true;}else bounds.Encapsulate(r.bounds);}
                if(found)transform.position=bounds.center+transform.parent.TransformVector(visual.offset);
            }
            if(visual.worldSpace){
                transform.rotation=Quaternion.identity;
                var p=transform.parent.lossyScale;
                // Set() records the authored world scale before actor-scale compensation.
                transform.localScale=new Vector3(worldScale.x/Mathf.Max(.0001f,Mathf.Abs(p.x)),worldScale.y/Mathf.Max(.0001f,Mathf.Abs(p.y)),worldScale.z/Mathf.Max(.0001f,Mathf.Abs(p.z)));
            }
            if(!hide&&innerParticles!=null)foreach(var ps in particles)if(ps&&(ps.name=="Sparks"||ps.name=="DarkSparks")){
                int count=ps.GetParticles(innerParticles);float radius=visual.referenceRadius*worldScale.x;
                for(int i=0;i<count;i++){var particle=innerParticles[i];Vector3 world=ps.transform.TransformPoint(particle.position),flat=Vector3.ProjectOnPlane(world-transform.position,Vector3.up);float allowed=Mathf.Max(0,radius-particle.GetCurrentSize(ps)*Mathf.Abs(ps.transform.lossyScale.x)*.5f);
                    if(flat.sqrMagnitude>allowed*allowed){world=transform.position+flat.normalized*allowed+Vector3.up*(world.y-transform.position.y);particle.position=ps.transform.InverseTransformPoint(world);innerParticles[i]=particle;}}
                ps.SetParticles(innerParticles,count);
            }
            if(hide==paused)return;paused=hide;
            foreach(var r in renderers)if(r)r.forceRenderingOff=hide;
            foreach(var ps in particles)if(ps){if(hide)ps.Pause(false);else ps.Play(false);}
        }
        private void OnEnable()
        {
            paused=false;
            if(renderers!=null)foreach(var r in renderers)if(r)r.forceRenderingOff=false;
        }
    }
}
