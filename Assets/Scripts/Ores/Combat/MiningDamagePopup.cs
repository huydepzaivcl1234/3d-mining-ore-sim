using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MiningSimulator.Ores
{
    [Serializable]
    public sealed class MiningDamagePopupSettings
    {
        public bool enabled = true;
        [Min(0.1f)] public float duration = 0.85f;
        [Min(0f)] public float riseHeight = 0.65f;
        [Min(0f)] public float fallDistance = 0.3f;
        [Range(0.1f, 0.9f)] public float peakTime = 0.32f;
        [Min(0f)] public float sidewaysDistance = 0.25f;
        public Vector3 worldOffset = new(0f, 0.35f, 0f);
        [Min(0.001f)] public float worldScale = 0.18f;
        [Min(1f)] public float fontSize = 18f;
        [Header("Damage-based font size / Cỡ số theo damage")]
        public bool scaleWithDamage=true;
        [Min(0)] public float damageForMinimumSize=1,damageForMaximumSize=100;
        [Min(1)] public float minimumFontSize=12,maximumFontSize=28;
        public float FontSizeForDamage(float damage)
        {
            if(!scaleWithDamage)return Mathf.Max(1,fontSize);
            float min=Mathf.Max(1,minimumFontSize),max=Mathf.Max(min,maximumFontSize);
            float t=damageForMaximumSize>damageForMinimumSize?Mathf.InverseLerp(damageForMinimumSize,damageForMaximumSize,damage):damage>=damageForMaximumSize?1:0;
            return Mathf.Lerp(min,max,t);
        }
        public TMP_FontAsset font;
        public Material material;
        public Color color = new(1f, 0.85f, 0.24f, 1f);
        [Header("Damage type colors / Màu loại sát thương")]
        public Color physicalColor = new(1f, .5f, .08f, 1f);
        public Color magicColor = new(1f, .3f, .85f, 1f);
        public Color trueColor = Color.white;
        public Color ColorForDamage(CombatDamageType type) => type switch
        {
            CombatDamageType.Magic => magicColor,
            CombatDamageType.True => trueColor,
            _ => physicalColor
        };
        public Color outlineColor = Color.black;
        [Range(0f, 1f)] public float outlineWidth = 0.2f;
        [Range(0f, 0.95f)] public float fadeStart = 0.55f;
        [Min(0f)] public float scalePunch = 0.25f;
        [Range(0.01f, 1f)] public float punchFraction = 0.25f;
        [Header("Pop and random spread")]
        [Range(.1f,1f)] public float initialScale = .55f;
        [Range(.1f,1f)] public float endScale = .85f;
        public Vector2 spawnSpread = new(.22f,.12f);
        [Range(0f,.9f)] public float motionVariation = .2f;
        [Min(0f)] public float randomTiltDegrees = 12f;
        [Min(0f)] public float spinDegrees = 8f;
        [Min(1)] public int maximumVisible = 64;
        [Min(0)] public int maximumRetained = 16;
    }

    /// <summary>Detached world-space hit numbers. Never follows the sword or the damaged enemy.</summary>
    [RequireComponent(typeof(TextMeshPro))]
    public sealed class MiningDamagePopup : MonoBehaviour
    {
        private static readonly Stack<MiningDamagePopup> pool=new();
        private static readonly LinkedList<MiningDamagePopup> active=new();
        private static Transform poolRoot;
        private static bool clearing, destroyImmediately;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPool()=>ClearAll();
        /// <summary>Clears only transient hit numbers, including orphaned objects from older sessions.</summary>
        public static void ClearAll(bool immediate=false)
        {
            if(clearing)return;
            clearing=true;destroyImmediately=immediate;
            try
            {
                foreach(var popup in Resources.FindObjectsOfTypeAll<MiningDamagePopup>())
                {
                    if(!popup||!IsTransient(popup.gameObject))continue;
                    popup.inUse=false;popup.gameObject.SetActive(false);
                    if(popup.node?.List!=null)popup.node.List.Remove(popup.node);
                    ReleaseObject(popup.gameObject);
                }
                pool.Clear();active.Clear();poolRoot=null;
                foreach(var root in Resources.FindObjectsOfTypeAll<Transform>())
                {
                    if(!root||root.name!="Damage Popup VFX Pool"||!IsTransient(root.gameObject))continue;
                    bool owned=true;
                    foreach(Transform child in root)if(!child.GetComponent<MiningDamagePopup>()){owned=false;break;}
                    if(owned){root.gameObject.SetActive(false);ReleaseObject(root.gameObject);}
                }
            }
            finally{clearing=false;destroyImmediately=false;}
        }
        private static bool IsTransient(GameObject go)=>
            (go.hideFlags&(HideFlags.DontSaveInEditor|HideFlags.DontSaveInBuild))!=0;
        private static void ReleaseObject(UnityEngine.Object value)
        {
            if(!value)return;
#if UNITY_EDITOR
            if(destroyImmediately||!Application.isPlaying){DestroyImmediate(value);return;}
#endif
            Destroy(value);
        }
        public static int VisibleCount => active.Count;
        private MiningDamagePopupSettings settings;
        private TextMeshPro label;
        private Camera view;
        private Vector3 origin, sideways;
        private float elapsed;
        private bool inUse;
        private LinkedListNode<MiningDamagePopup> node;
        private Material ownedMaterial, sourceMaterial;
        private float motionScale, tilt, spin;
        private Color hitColor;

        public static void Show(float damage, Vector3 hitPoint, MiningDamagePopupSettings settings, CombatDamageType type = CombatDamageType.Physical)
        {
            if (!Application.isPlaying || clearing || damage <= 0f || float.IsNaN(damage) || float.IsInfinity(damage) || settings == null || !settings.enabled) return;
            // Keep every new hit visible under load, replacing the oldest number at the budget.
            while(active.Count>=Mathf.Max(1,settings.maximumVisible))active.First.Value.Recycle();
            MiningDamagePopup popup=null;
            while(pool.Count>0&&!popup)popup=pool.Pop();
            // DontSaveInEditor/DontSave detach objects from the scene; detached objects can survive Stop.
            if(!popup){var created=new GameObject("Damage Popup",typeof(TextMeshPro));created.hideFlags=HideFlags.DontSaveInBuild;popup=created.AddComponent<MiningDamagePopup>();}
            if(!poolRoot){var group=new GameObject("Damage Popup VFX Pool");group.hideFlags=HideFlags.DontSaveInBuild;poolRoot=group.transform;}
            popup.transform.SetParent(poolRoot,false);
            var go=popup.gameObject;popup.elapsed=0f;
            popup.settings = settings;
            popup.label = go.GetComponent<TextMeshPro>();
            popup.view = Camera.main;
            Vector3 right=popup.view?popup.view.transform.right:Vector3.right;
            Vector3 up=popup.view?popup.view.transform.up:Vector3.up;
            popup.origin = hitPoint + settings.worldOffset +
                right*UnityEngine.Random.Range(-settings.spawnSpread.x,settings.spawnSpread.x)+
                up*UnityEngine.Random.Range(-settings.spawnSpread.y,settings.spawnSpread.y);
            popup.sideways = right *
                UnityEngine.Random.Range(-settings.sidewaysDistance, settings.sidewaysDistance);
            popup.motionScale=UnityEngine.Random.Range(1-settings.motionVariation,1+settings.motionVariation);
            popup.tilt=UnityEngine.Random.Range(-settings.randomTiltDegrees,settings.randomTiltDegrees);
            popup.spin=UnityEngine.Random.Range(-settings.spinDegrees,settings.spinDegrees);
            popup.label.font = settings.font != null ? settings.font : TMP_Settings.defaultFontAsset;
            popup.ConfigureMaterial();
            popup.label.text = damage < .01f ? damage.ToString("0.#######") : damage.ToString("0.##");
            popup.label.fontSize = settings.FontSizeForDamage(damage);
            popup.label.fontStyle = FontStyles.Bold;
            popup.label.alignment = TextAlignmentOptions.Center;
            popup.label.textWrappingMode = TextWrappingModes.NoWrap;
            popup.hitColor = settings.ColorForDamage(type);
            popup.label.color = popup.hitColor;
            popup.label.enableAutoSizing = false;
            popup.label.raycastTarget = false;
            popup.transform.position = popup.origin;
            popup.inUse = true;
            popup.node??=new LinkedListNode<MiningDamagePopup>(popup);
            active.AddLast(popup.node);
            popup.ApplyProgress(0f);
            go.SetActive(true);
            popup.label.UpdateMeshPadding();
            popup.label.ForceMeshUpdate();
        }

        private void ConfigureMaterial()
        {
            var source=settings.material?settings.material:label.font.material;
            if(!ownedMaterial||sourceMaterial!=source)
            {
                if(ownedMaterial)Destroy(ownedMaterial);
                ownedMaterial=new Material(source){name="Damage Popup Font",hideFlags=HideFlags.DontSave,enableInstancing=false};
                sourceMaterial=source;
            }
            ownedMaterial.SetColor("_FaceColor",Color.white);
            ownedMaterial.SetColor("_OutlineColor",settings.outlineColor);
            ownedMaterial.SetFloat("_OutlineWidth",settings.outlineWidth);
            ownedMaterial.EnableKeyword("OUTLINE_ON");
            ShaderUtilities.UpdateShaderRatios(ownedMaterial);
            label.fontSharedMaterial=ownedMaterial;
            label.renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            label.renderer.receiveShadows=false;
            label.renderer.lightProbeUsage=UnityEngine.Rendering.LightProbeUsage.Off;
            label.renderer.reflectionProbeUsage=UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }

        // Smooth velocity reaches zero at the apex, then accelerates downwards.
        public static float EvaluateHeight(float progress, MiningDamagePopupSettings settings)
        {
            float peak = Mathf.Clamp(settings.peakTime, 0.1f, 0.9f);
            if (progress <= peak)
            {
                float t = Mathf.Clamp01(progress / peak);
                return settings.riseHeight * (1f - (1f - t) * (1f - t));
            }
            float fall = Mathf.Clamp01((progress - peak) / (1f - peak));
            return settings.riseHeight - (settings.riseHeight + settings.fallDistance) * fall * fall;
        }

        private void LateUpdate()
        {
            if (settings == null || !inUse) return;
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.1f, settings.duration));
            ApplyProgress(progress);
            if (view == null) view = Camera.main;
            if (progress >= 1f)Recycle();
        }

        private void ApplyProgress(float progress)
        {
            float lateral=1f-Mathf.Pow(1f-progress,2);
            transform.position = origin + sideways * lateral +
                Vector3.up * EvaluateHeight(progress, settings)*motionScale;
            float t=Mathf.Clamp01(progress/Mathf.Max(.01f,settings.punchFraction));
            float punch=t<.4f?Mathf.Lerp(settings.initialScale,1+settings.scalePunch,1-Mathf.Pow(1-t/.4f,3)):
                Mathf.Lerp(1+settings.scalePunch,1,Mathf.SmoothStep(0,1,(t-.4f)/.6f));
            float fade=Mathf.SmoothStep(0,1,Mathf.InverseLerp(settings.fadeStart,1,progress));
            punch*=Mathf.Lerp(1,settings.endScale,fade);
            transform.localScale = Vector3.one * Mathf.Max(0.001f, settings.worldScale) * punch;
            transform.rotation=(view?view.transform.rotation:Quaternion.identity)*Quaternion.Euler(0,0,tilt+spin*progress);
            Color color = hitColor;
            color.a *= 1f-fade;
            label.color = color;
        }
        private void Recycle()
        {
            if(!inUse)return;
            inUse=false;if(node?.List!=null)node.List.Remove(node);gameObject.SetActive(false);
            if(pool.Count<Mathf.Max(0,settings.maximumRetained))pool.Push(this);
            else Destroy(gameObject);
        }
        private void OnDisable(){if(inUse&&!clearing)Recycle();}
        private void OnDestroy()
        {
            if(node?.List!=null)node.List.Remove(node);
            inUse=false;
            if(ownedMaterial)ReleaseObject(ownedMaterial);
        }
    }
}
