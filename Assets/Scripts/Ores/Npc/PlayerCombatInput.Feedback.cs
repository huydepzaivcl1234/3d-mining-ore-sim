using System.Collections.Generic;
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
        SpawnEffect(impactPrefab, point, Quaternion.LookRotation(towardPlayer, Vector3.up));
    }

    private GameObject SpawnEffect(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        var effect = Instantiate(prefab, position, rotation);
        PlayAndDestroyEffect(effect);
        return effect;
    }

    private void PlayAndDestroyEffect(GameObject effect)
    {
        // The imported Free Slash VFX prefab disables Play On Awake on its
        // particle systems; explicitly start it at the animation contact event.
        var particles = effect.GetComponent<ParticleSystem>();
        if (particles != null) particles.Play(true);
        float lifetime = Stats != null ? Stats.impactVfxLifetime : 2f;
        Destroy(effect, Mathf.Max(0.1f, lifetime));
    }

    public void OnSlashDownStart()
    {
        PlaySlashEffects(slashDownVfx, slashDownEffects);
    }

    public void OnSlashUpStart()
    {
        PlaySlashEffects(slashUpVfx, slashUpEffects);
    }

    public void OnSpecialSlashStart(AnimationEvent animationEvent) => OnSlashThirdStart(animationEvent);

    public void OnSlashThirdStart(AnimationEvent animationEvent)
    {
        if (!Application.isPlaying || !combatMode || !CanUseGameplay() || animator == null || Stats == null) return;
        int layer = animator.GetLayerIndex(CombatLayerName);
        // Ignore an outgoing clip's event during a cancelled strike.
        if (layer < 0 || (animator.IsInTransition(layer)
            ? animator.GetNextAnimatorStateInfo(layer).shortNameHash
            : animator.GetCurrentAnimatorStateInfo(layer).shortNameHash) != ThirdAttackState) return;
        var prefab = Stats.thirdAttackSlashVfxPrefab;
        if (prefab == null) return;
        var clip = animationEvent.animatorClipInfo.clip;
        float contactTime = clip.length;
        foreach (var cue in clip.events)
            if ((cue.functionName == nameof(OnSwordStrikeThird) || cue.functionName == nameof(OnSpecialAttackHit)) && cue.time > animationEvent.time)
                contactTime = Mathf.Min(contactTime, cue.time);
        float window = Mathf.Max(.01f, contactTime - animationEvent.time);
        var state = animationEvent.animatorStateInfo;
        float playbackSpeed = Mathf.Max(.01f, state.speed * state.speedMultiplier * animator.speed);
        EndThirdSlash();
        // Snapshot the authored emitter pose. Neither the slash root nor its
        // particles follow the moving player/sword after emission.
        var effect = Instantiate(prefab, transform.TransformPoint(prefab.transform.localPosition),
            transform.rotation * prefab.transform.localRotation);
        thirdSlashInstance = effect;
        foreach (var system in effect.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = system.main;
            // Stretch each burst's lifetime/animation over the exact remaining
            // clip time, rather than using a fixed real-time delay.
            main.simulationSpeed = Mathf.Max(.01f, main.startLifetime.constantMax) / window * playbackSpeed;
        }
        var particles = effect.GetComponent<ParticleSystem>();
        if (particles != null) PlaySlashEffect(particles);
        Destroy(effect, Mathf.Max(Stats.thirdAttackSlashLifetime, window / playbackSpeed + .1f));
    }

    private void EndThirdSlash()
    {
        if (thirdSlashInstance == null) return;
        var particles = thirdSlashInstance.GetComponent<ParticleSystem>();
        if (particles != null) particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        Destroy(thirdSlashInstance);
        thirdSlashInstance = null;
    }

    private void PlaySlashEffects(ParticleSystem first, List<SlashVfxCue> additional)
    {
        if (!Application.isPlaying || !combatMode || !CanUseGameplay()) return;
        if (first != null) PlaySlashEffect(first);
        if (additional == null) return;
        foreach (var cue in additional)
        {
            if (cue.effect == null) continue;
            if (cue.delay <= 0f) PlaySlashEffect(cue.effect);
            else StartCoroutine(PlaySlashEffectAfterDelay(cue.effect, cue.delay / AttackSpeed));
        }
    }

    private System.Collections.IEnumerator PlaySlashEffectAfterDelay(ParticleSystem effect, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (combatMode && CanUseGameplay() && effect != null) PlaySlashEffect(effect);
    }

    private static void PlaySlashEffect(ParticleSystem effect)
    {
        // World-space particles keep the emitted slash in place even when the
        // scene emitter is parented to the moving hand or player.
        foreach (var system in effect.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = system.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
        }
        if (!effect.gameObject.activeSelf) effect.gameObject.SetActive(true);
        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        effect.Play(true);
    }




}
