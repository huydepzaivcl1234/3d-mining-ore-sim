using MiningSimulator.Ores;
using UnityEngine;

public partial class PlayerCombatInput
{
    private MiningItemSystem equipmentItems;
    private void DamageTarget(MiningCharacterHealth target, Vector3 point, float damageMultiplier = 1f)
    {
        if (!Application.isPlaying || Damage <= 0 || target == null || target.Health <= 0f) return;
        float dealt = target.DealDamage(Damage * damageMultiplier,
            Stats != null ? Stats.attackDamageType : CombatDamageType.Physical);
        if (dealt <= 0f) return;
        if (target.GetComponent<MushroomMonster>() != null && Stats != null)
            MiningDamagePopup.Show(dealt, point, Stats.damagePopup);
        if (equipmentItems == null) equipmentItems = FindFirstObjectByType<MiningItemSystem>();
        var bonuses = equipmentItems != null ? equipmentItems.EquipmentBonuses : null;
        if (bonuses != null)
        {
            if (ownHealth != null && ownHealth.Health > 0f)
                ownHealth.Heal(dealt * bonuses.LifeStealFraction);
            target.ApplyBurn(bonuses.burnDamagePerTick, bonuses.burnTickSeconds, bonuses.burnDurationSeconds);
        }
        var impactPrefab = Stats != null ? Stats.attackImpactVfxPrefab : null;
        if (impactPrefab == null) return;
        Vector3 towardPlayer = transform.position - point;
        towardPlayer.y = 0f;
        if (towardPlayer.sqrMagnitude < 0.0001f) towardPlayer = -StrikeForward;
        var effect = Instantiate(impactPrefab, point, Quaternion.LookRotation(towardPlayer, Vector3.up));
        foreach (var particles in effect.GetComponentsInChildren<ParticleSystem>(true)) particles.Play();
        Destroy(effect, Mathf.Max(.1f, Stats.impactVfxLifetime));
    }
}
