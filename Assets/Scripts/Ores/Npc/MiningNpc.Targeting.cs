using UnityEngine;

namespace MiningSimulator.Ores
{
    public sealed partial class MiningNpc
    {
        private void TryAcquireTarget(Vector3 currentPosition)
        {
            Ore excludedOre = Time.time < ignoredOreUntil ? ignoredOre : null;
            LuckyBlock excludedBlock = Time.time < ignoredLuckyBlockUntil
                ? ignoredLuckyBlock
                : null;
            MiningChest excludedChest = Time.time < ignoredChestUntil ? ignoredChest : null;
            bool foundOre = oreSpawner.TryReserveClosestOre(this, currentPosition,
                CurrentMiningPower, excludedOre, out Ore ore, out int oreSlotIndex);
            LuckyBlock block = null;
            int blockSlotIndex = -1;
            bool foundBlock = luckyBlockSystem != null &&
                              luckyBlockSystem.TryReserveClosestBlock(this, currentPosition,
                                  CurrentMiningPower, excludedBlock, out block,
                                  out blockSlotIndex);

            bool foundChest = MiningChest.TryReserveClosest(this, currentPosition,
                CurrentMiningPower, excludedChest, out MiningChest chest);
            float oreDistance = foundOre ? ore.SqrDistanceToSurface(currentPosition) : float.PositiveInfinity;
            float blockDistance = foundBlock ? block.SqrDistanceToSurface(currentPosition) : float.PositiveInfinity;
            float chestDistance = foundChest ? chest.SqrDistanceToSurface(currentPosition) : float.PositiveInfinity;
            if (foundChest && chestDistance <= oreDistance && chestDistance <= blockDistance)
            {
                if (foundOre) ore.ReleaseMiner(this);
                if (foundBlock) block.ReleaseMiner(this);
                SetTarget(chest);
                return;
            }
            if (foundChest) chest.ReleaseMiner(this);

            if (foundOre && foundBlock)
            {
                if (block.SqrDistanceToSurface(currentPosition) <
                    ore.SqrDistanceToSurface(currentPosition))
                {
                    ore.ReleaseMiner(this);
                    SetTarget(block, blockSlotIndex);
                }
                else
                {
                    block.ReleaseMiner(this);
                    SetTarget(ore, oreSlotIndex);
                }
            }
            else if (foundBlock)
            {
                SetTarget(block, blockSlotIndex);
            }
            else if (foundOre)
            {
                SetTarget(ore, oreSlotIndex);
            }
        }

        private bool TrySwitchTarget(Ore ore)
        {
            if (ore == null || ore == targetOre)
            {
                return ore == targetOre;
            }

            if (!oreSpawner.TryReserveOre(this, ore, CurrentMiningPower, out int slotIndex))
            {
                return false;
            }

            SetTarget(ore, slotIndex);
            return true;
        }

        private void SetTarget(Ore ore, int slotIndex, bool commandedTarget = false)
        {
            Ore previousOre = targetOre;
            LuckyBlock previousBlock = targetLuckyBlock;
            MiningChest previousChest = targetChest;
            targetOre = ore;
            targetLuckyBlock = null;
            targetChest = null;
            reservedSlot = slotIndex;
            hasCommandedTarget = commandedTarget;
            previousBlock?.ReleaseMiner(this);
            previousChest?.ReleaseMiner(this);
            if (previousOre != null && previousOre != ore)
            {
                previousOre.ReleaseMiner(this);
                ignoredOre = previousOre;
                ignoredOreUntil = Time.time + npcData.IgnoredTargetDuration;
            }

            ResetTargetMovement();
        }

        private void SetTarget(LuckyBlock block, int slotIndex, bool commandedTarget = false)
        {
            Ore previousOre = targetOre;
            LuckyBlock previousBlock = targetLuckyBlock;
            MiningChest previousChest = targetChest;
            targetOre = null;
            targetLuckyBlock = block;
            targetChest = null;
            reservedSlot = slotIndex;
            hasCommandedTarget = commandedTarget;
            previousOre?.ReleaseMiner(this);
            previousChest?.ReleaseMiner(this);
            if (previousBlock != null && previousBlock != block)
            {
                previousBlock.ReleaseMiner(this);
                ignoredLuckyBlock = previousBlock;
                ignoredLuckyBlockUntil = Time.time + npcData.IgnoredTargetDuration;
            }

            ResetTargetMovement();
        }

        private void SetTarget(MiningChest chest, bool commandedTarget = false)
        {
            Ore previousOre = targetOre;
            LuckyBlock previousBlock = targetLuckyBlock;
            MiningChest previousChest = targetChest;
            targetOre = null;
            targetLuckyBlock = null;
            targetChest = chest;
            reservedSlot = -1;
            hasCommandedTarget = commandedTarget;
            previousOre?.ReleaseMiner(this);
            previousBlock?.ReleaseMiner(this);
            if (previousChest != null && previousChest != chest)
                previousChest.ReleaseMiner(this);
            ResetTargetMovement();
        }

        private void ResetTargetMovement()
        {
            SetMiningAnimationState(false);
            nextTargetSwitchTime = Time.time + npcData.TargetSwitchCooldown;
            stuckRepathAttempts = 0;
            ClearDetour();
            ResetGlobalPath();
            ResetProgressTracking();
        }

        private Vector3 GetPathTargetPosition(Vector3 currentPosition)
        {
            if (targetChest != null)
                return targetChest.GetClosestSurfacePoint(currentPosition);
            if (targetLuckyBlock != null)
            {
                return targetLuckyBlock.GetMiningStandPosition(
                    reservedSlot, npcData.ColliderRadius, npcData.StandSlotSpacingPadding);
            }

            if (targetOre == null) return transform.position;
            // Route arrival and mining reach are different thresholds. Close the last gap
            // instead of braking at a stand point just outside the damage range.
            float finalReach = npcData.MiningRange + npcData.StoppingDistance;
            if (targetOre.SqrDistanceToSurface(currentPosition) <= finalReach * finalReach)
                return targetOre.GetClosestSurfacePoint(currentPosition);
            // Retain the chosen side while moving around an ore. Re-selecting relative to
            // our new heading every repath can move the goal around the same rock forever.
            bool refreshApproach = approachOre != targetOre;
            if (!refreshApproach && Time.time >= nextApproachRefresh)
            {
                nextApproachRefresh = Time.time + repathInterval;
                refreshApproach = !MiningNavigation.IsMineableSegmentClear(oreApproachPoint,
                    oreApproachPoint + Vector3.forward * .05f, NavigationRadius, targetOre);
            }
            if (refreshApproach)
            {
                approachOre = targetOre;
                nextApproachRefresh = Time.time + repathInterval;
                oreApproachPoint = MiningNavigation.TryGetOreApproach(targetOre, currentPosition,
                    NavigationRadius, out Vector3 approach, out _)
                    ? approach : targetOre.GetClosestSurfacePoint(currentPosition);
            }
            return oreApproachPoint;
        }

        /// <summary>Prevents runtime ore placement on or directly beside an active miner.</summary>
        public static bool IsSpawnPositionClear(Vector3 position, float clearance)
        {
            clearance = Mathf.Max(0f, clearance);
            for (int index = ActiveNpcs.Count - 1; index >= 0; index--)
            {
                MiningNpc npc = ActiveNpcs[index];
                if (npc == null)
                {
                    ActiveNpcs.RemoveAt(index);
                    continue;
                }

                if (!npc.isActiveAndEnabled)
                {
                    continue;
                }

                Vector3 npcPosition = npc.body != null ? npc.body.position : npc.transform.position;
                Vector3 offset = position - npcPosition;
                offset.y = 0f;
                float npcRadius = npc.npcData != null
                    ? Mathf.Max(0f, npc.npcData.ColliderRadius)
                    : 0.5f;
                float requiredDistance = clearance + npcRadius;
                if (offset.sqrMagnitude < requiredDistance * requiredDistance)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsTargetValid()
        {
            if (targetChest != null) return CanMine(targetChest);
            return targetLuckyBlock != null
                ? CanMine(targetLuckyBlock)
                : CanMine(targetOre);
        }

        private bool CanMine(Ore ore)
        {
            return npcData != null && ore != null && ore.isActiveAndEnabled &&
                   !ore.IsDepleted && ore.Data != null &&
                   ore.Data.MiningPowerRequired <= CurrentMiningPower;
        }

        private bool CanMine(LuckyBlock block)
        {
            return npcData != null && block != null && block.isActiveAndEnabled &&
                   !block.IsResolved && block.CanAcceptMiner(this, CurrentMiningPower);
        }

        private bool CanMine(MiningChest chest)
        {
            return npcData != null && chest != null && chest.CanAcceptMiner(this, CurrentMiningPower);
        }

        private Vector3 GetTargetPosition()
        {
            if (targetChest != null) return targetChest.transform.position;
            return targetLuckyBlock != null
                ? targetLuckyBlock.transform.position
                : targetOre != null ? targetOre.transform.position : transform.position;
        }

        private float SqrDistanceToTargetSurface(Vector3 currentPosition)
        {
            if (targetChest != null) return targetChest.SqrDistanceToSurface(currentPosition);
            return targetLuckyBlock != null
                ? targetLuckyBlock.SqrDistanceToSurface(currentPosition)
                : targetOre != null
                    ? targetOre.SqrDistanceToSurface(currentPosition)
                    : float.PositiveInfinity;
        }

        private void ApplyDamageToTarget()
        {
            itemSystem ??= FindFirstObjectByType<MiningItemSystem>(FindObjectsInactive.Include);
            if (targetChest != null)
            {
                float damage = progressionSystem != null
                    ? progressionSystem.CurrentDamagePerHit : npcData.DamagePerHit;
                targetChest.ApplyNpcDamage(damage);
                return;
            }
            if (targetLuckyBlock != null)
            {
                float damage = progressionSystem != null
                    ? progressionSystem.CurrentDamagePerHit
                    : npcData.DamagePerHit;
                targetLuckyBlock.ApplyNpcDamage(itemSystem != null
                    ? itemSystem.RollOreLuckyDamage(damage) : damage);
            }
            else
            {
                float damage = progressionSystem != null
                    ? progressionSystem.CurrentDamagePerHit
                    : npcData.DamagePerHit;
                targetOre?.ApplyNpcDamage(itemSystem != null
                    ? itemSystem.RollOreLuckyDamage(damage) : damage);
            }
        }

        private void PlayMiningImpactAudio()
        {
            if (audioManager == null)
            {
                return;
            }

            if (targetOre != null && targetOre.LastDamageWasNpc)
            {
                audioManager.PlayMiningImpactSfx(targetOre.IsDepleted, targetOre.transform.position);
                return;
            }

            if (targetLuckyBlock != null && targetLuckyBlock.LastDamageWasNpc)
            {
                audioManager.PlayMiningImpactSfx(targetLuckyBlock.IsResolved, targetLuckyBlock.transform.position);
            }
        }

        private void TryAdoptVisibleOre(Vector3 currentPosition)
        {
            if (hasCommandedTarget || targetLuckyBlock != null || targetChest != null)
            {
                return;
            }

            Vector3 direction = targetOre != null
                ? targetOre.transform.position - currentPosition
                : transform.forward;
            direction.y = 0f;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            Vector3 origin = currentPosition + Vector3.up * npcData.OreSightOriginHeight;
            int hitCount = Physics.SphereCastNonAlloc(origin, npcData.OreSightProbeRadius,
                direction.normalized, obstacleHits, npcData.OreSightDistance,
                npcData.CollisionLayers, QueryTriggerInteraction.Ignore);
            Ore visibleOre = null;
            float closestHitDistance = float.PositiveInfinity;
            float miningRangeSqr = npcData.MiningRange * npcData.MiningRange;
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = obstacleHits[index];
                Ore ore = hit.collider != null ? hit.collider.GetComponentInParent<Ore>() : null;
                if (ore == null || ore == targetOre ||
                    (ore == ignoredOre && Time.time < ignoredOreUntil) ||
                    hit.distance >= closestHitDistance ||
                    !CanMine(ore) || ore.SqrDistanceToSurface(currentPosition) > miningRangeSqr ||
                    !ore.CanAcceptMiner(this, CurrentMiningPower))
                {
                    continue;
                }

                visibleOre = ore;
                closestHitDistance = hit.distance;
            }

            if (visibleOre != null)
            {
                TrySwitchTarget(visibleOre);
            }
        }

        private void ReleaseTarget()
        {
            if (targetOre != null)
            {
                targetOre.ReleaseMiner(this);
            }

            if (targetLuckyBlock != null)
            {
                targetLuckyBlock.ReleaseMiner(this);
            }

            if (targetChest != null) targetChest.ReleaseMiner(this);

            targetOre = null;
            targetLuckyBlock = null;
            targetChest = null;
            reservedSlot = -1;
            hasCommandedTarget = false;
            hasMoveTarget = false;
            stuckRepathAttempts = 0;
            SetMiningAnimationState(false);
            ClearDetour();
            ResetGlobalPath();
        }

    }
}
