using UnityEngine;
using MiningSimulator.Ores;

public class EquipmentSystem : MonoBehaviour
{
    // Keep these field names and types: they are assigned in SampleScene.
    [SerializeField] private GameObject weaponHolder;
    [SerializeField] private GameObject weapon;
    [SerializeField] private GameObject weaponSheath;

    private GameObject currentWeapon;
    private MiningCharacterHealth health;
    private MiningAudioManager audioManager;
    private Vector3 bladeBaseLocal;
    private Vector3 bladeTipLocal;
    private Vector3 previousTip;
    private Vector3 swingDirection;
    private bool hasPreviousTip;

    private void Awake()
    {
        health = GetComponent<MiningCharacterHealth>();
        audioManager = FindFirstObjectByType<MiningAudioManager>();
        ResetToSheath();
    }

    public void DrawWeapon()
    {
        if (health != null && health.Health <= 0f) return;
        bool wasDrawn = IsDrawn;
        MoveWeapon(weaponHolder);
        if (!wasDrawn && IsDrawn) PlayEquipSfx(true);
    }
    public void SheathWeapon()
    {
        bool wasDrawn = IsDrawn;
        MoveWeapon(weaponSheath);
        if (wasDrawn && !IsDrawn) PlayEquipSfx(false);
    }
    // Preserve event spellings used by older imported animation clips.
    public void ShealthWeapon() => SheathWeapon();
    public void OnDrawWeapon() => DrawWeapon();
    public void OnSheathWeapon() => SheathWeapon();

    public void ResetToSheath() => MoveWeapon(weaponSheath);

    private void PlayEquipSfx(bool drawing)
    {
        var data = MiningPlayerStats.For(this);
        if (data == null) return;
        var clip = drawing ? data.drawWeaponSfx : data.sheathWeaponSfx;
        var volume = drawing ? data.drawWeaponSfxVolume : data.sheathWeaponSfxVolume;
        if (audioManager == null) audioManager = FindFirstObjectByType<MiningAudioManager>();
        if (audioManager != null) audioManager.PlayWorldSfx(clip, transform.position, volume);
    }

    private void MoveWeapon(GameObject holder)
    {
        if (holder == null || weapon == null || weaponHolder == null || weaponSheath == null) return;
        if (currentWeapon == null)
        {
            // Adopt an existing scene/runtime copy before creating one. This also
            // repairs duplicate copies left by older animation events or reloads.
            currentWeapon = FindAndDeduplicateWeapons();
            if (currentWeapon == null)
                currentWeapon = Instantiate(weapon, holder.transform);
            FindBladeEndpoints();
        }
        if (currentWeapon.transform.parent != holder.transform)
            currentWeapon.transform.SetParent(holder.transform, false);
        currentWeapon.SetActive(true);
        hasPreviousTip = false;
        swingDirection = Vector3.zero;
    }

    private GameObject FindAndDeduplicateWeapons()
    {
        GameObject found = null;
        foreach (GameObject holder in new[] { weaponSheath, weaponHolder })
        {
            for (int i = holder.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = holder.transform.GetChild(i);
                if (child.name != weapon.name && child.name != weapon.name + "(Clone)") continue;
                if (found == null) found = child.gameObject;
                else
                {
                    child.gameObject.SetActive(false);
                    if (Application.isPlaying) Destroy(child.gameObject);
                    else DestroyImmediate(child.gameObject);
                }
            }
        }
        return found;
    }

    // The sword prefab has no authored tip socket. Determine its long mesh axis
    // once from renderer bounds, then track that tip through the hand animation.
    private void FindBladeEndpoints()
    {
        var renderers = currentWeapon.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds localBounds = default;
        foreach (Renderer renderer in renderers)
        {
            Bounds world = renderer.bounds;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 point = currentWeapon.transform.InverseTransformPoint(world.center +
                    Vector3.Scale(world.extents, new Vector3(x, y, z)));
                if (!hasBounds) { localBounds = new Bounds(point, Vector3.zero); hasBounds = true; }
                else localBounds.Encapsulate(point);
            }
        }
        if (!hasBounds)
        {
            bladeBaseLocal = Vector3.zero;
            bladeTipLocal = Vector3.up;
            return;
        }
        Vector3 size = localBounds.size;
        int axis = size.x > size.y && size.x > size.z ? 0 : size.y > size.z ? 1 : 2;
        Vector3 lower = localBounds.center;
        Vector3 upper = lower;
        lower[axis] = localBounds.min[axis];
        upper[axis] = localBounds.max[axis];
        Vector3 grip = currentWeapon.transform.InverseTransformPoint(currentWeapon.transform.parent.position);
        bladeTipLocal = (upper - grip).sqrMagnitude >= (lower - grip).sqrMagnitude ? upper : lower;
        bladeBaseLocal = bladeTipLocal == upper ? lower : upper;
    }

    private void LateUpdate()
    {
        if (!IsDrawn) { hasPreviousTip = false; return; }
        Vector3 tip = currentWeapon.transform.TransformPoint(bladeTipLocal);
        if (hasPreviousTip && Time.deltaTime > 0f)
        {
            Vector3 velocity = tip - previousTip;
            if (velocity.sqrMagnitude > 0.000001f) swingDirection = velocity.normalized;
        }
        previousTip = tip;
        hasPreviousTip = true;
    }

    public bool IsDrawn => currentWeapon != null && weaponHolder != null &&
        currentWeapon.transform.parent == weaponHolder.transform;

    public bool TryGetBladePose(out Vector3 tip, out Quaternion rotation)
    {
        tip = Vector3.zero;
        rotation = Quaternion.identity;
        if (!IsDrawn) return false;
        Transform sword = currentWeapon.transform;
        tip = sword.TransformPoint(bladeTipLocal);
        Vector3 blade = tip - sword.TransformPoint(bladeBaseLocal);
        if (blade.sqrMagnitude < 0.0001f) blade = sword.up;
        blade.Normalize();
        // Animation events arrive before LateUpdate: use this frame's tip
        // displacement so the flash faces the actual contact direction.
        Vector3 tipMotion = hasPreviousTip ? tip - previousTip : swingDirection;
        Vector3 sweep = Vector3.ProjectOnPlane(tipMotion, blade);
        if (sweep.sqrMagnitude < 0.0001f) sweep = Vector3.ProjectOnPlane(swingDirection, blade);
        if (sweep.sqrMagnitude < 0.0001f) sweep = Vector3.ProjectOnPlane(sword.forward, blade);
        if (sweep.sqrMagnitude < 0.0001f) sweep = Vector3.ProjectOnPlane(sword.right, blade);
        rotation = Quaternion.LookRotation(sweep.normalized, blade);
        return true;
    }
}
