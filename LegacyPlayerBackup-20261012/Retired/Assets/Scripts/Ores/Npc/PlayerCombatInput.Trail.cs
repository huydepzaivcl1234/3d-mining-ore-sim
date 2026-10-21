using MiningSimulator.Ores;
using UnityEngine;

public partial class PlayerCombatInput
{
    private EquipmentSystem swordEquipment;
    private SwordSwingTrail swordTrail;
    private int trailAttackState;
    private float trailFinishAt = float.PositiveInfinity;

    public void OnSwordTrailDownStart() => BeginSwordTrail(FirstAttackState, LungeAttackState);
    public void OnSwordTrailUpStart() => BeginSwordTrail(SecondAttackState, TurnAttackState);
    public void OnSpecialSwordTrailStart() => BeginSwordTrail(ThirdAttackState);

    private void BeginSwordTrail(int expectedState, int alternateState = 0)
    {
        if (!Application.isPlaying || !combatMode || !CanUseGameplay() || animator == null ||
            Stats == null || Stats.swordTrail == null || !Stats.swordTrail.enabled) return;
        int layer = animator.GetLayerIndex(CombatLayerName);
        if (layer < 0) return;
        var state = animator.IsInTransition(layer) ? animator.GetNextAnimatorStateInfo(layer) : animator.GetCurrentAnimatorStateInfo(layer);
        if (state.shortNameHash != expectedState && state.shortNameHash != alternateState) return;
        if (swordEquipment == null) swordEquipment = GetComponent<EquipmentSystem>();
        if (swordEquipment == null || !swordEquipment.IsDrawn) return;
        if (swordTrail == null) swordTrail = new SwordSwingTrail(transform);
        swordTrail.Clear();
        trailAttackState = state.shortNameHash;
        trailFinishAt = float.PositiveInfinity;
    }

    private void UpdateSwordTrail()
    {
        if (trailAttackState == 0) return;
        int layer = animator != null ? animator.GetLayerIndex(CombatLayerName) : -1;
        var state = layer >= 0 ? (animator.IsInTransition(layer) ? animator.GetNextAnimatorStateInfo(layer) : animator.GetCurrentAnimatorStateInfo(layer)) : default;
        if (!combatMode || !CanUseGameplay() || state.shortNameHash != trailAttackState || Time.time >= trailFinishAt ||
            swordEquipment == null || !swordEquipment.TryGetBladeSegment(out Vector3 bladeBase, out Vector3 tip))
        { EndSwordTrail(); return; }
        swordTrail.Sample(bladeBase, tip, Time.time, Stats.swordTrail);
    }

    private void FinishSwordTrail()
    {
        if (trailAttackState == 0) return;
        trailFinishAt = Time.time + Mathf.Max(0, Stats.swordTrail.followThroughSeconds) / AttackSpeed;
        if (trailFinishAt <= Time.time) EndSwordTrail();
    }

    private void EndSwordTrail()
    {
        trailAttackState = 0;
        trailFinishAt = float.PositiveInfinity;
        swordTrail?.Clear();
    }
}
