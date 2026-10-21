using MiningSimulator.Ores;
using UnityEngine;

public partial class PlayerCombatInput
{
    private MiningItemSystem equipmentItems;
    private void DamageTarget(MiningCharacterHealth target, Vector3 point, float damageMultiplier = 1f)
    {
        if (!Application.isPlaying || Damage <= 0 || target == null || target.Health <= 0f ||
            (target.GetComponentInParent<TreasureChest>() != null || target.GetComponentInParent<TowerRuntime>() != null)) return;
        bool monsterHit = target.GetComponent<MushroomMonster>() != null;
        float trueBonus = monsterHit && PlayerStats != null ? target.MaxHealth * PlayerStats.ConsumableMonsterTrueDamageFraction : 0f;
        float dealt = target.DealDamageWithTrueBonus(Damage * damageMultiplier,
            Stats != null ? Stats.attackDamageType : CombatDamageType.Physical, gameObject, trueBonus);
        if (dealt <= 0f) return;
        if (target.GetComponent<MushroomMonster>() != null && Stats != null)
            MiningDamagePopup.Show(dealt, point, Stats.damagePopup);
        if (equipmentItems == null) equipmentItems = FindFirstObjectByType<MiningItemSystem>();
        var bonuses = equipmentItems != null ? equipmentItems.EquipmentBonuses : null;
        if (bonuses != null)
        {
            if (ownHealth != null && ownHealth.Health > 0f)
                ownHealth.Heal(dealt * bonuses.LifeStealFraction);
            target.ApplyBurn(bonuses.burnDamagePerTick, bonuses.burnTickSeconds, bonuses.burnDurationSeconds, gameObject);
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
