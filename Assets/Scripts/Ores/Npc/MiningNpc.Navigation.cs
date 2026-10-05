using UnityEngine;

namespace MiningSimulator.Ores
{
    public sealed partial class MiningNpc
    {
        private void TrackMovementProgress(Vector3 currentPosition)
        {
            Vector3 progress = currentPosition - lastProgressPosition;
            progress.y = 0f;
            if (progress.sqrMagnitude >= npcData.StuckProgressDistance * npcData.StuckProgressDistance)
            {
                lastProgressPosition = currentPosition;
                lastProgressTime = Time.time;
                return;
            }

            if (Time.time - lastProgressTime < npcData.StuckTimeout)
            {
                return;
            }

            // First remedy for any stuck miner: force a fresh route. The old route may be stale
            // (ore mined out from under it, pushed off the path by separation, carve boundary
            // shifted). This used to permanently disable pathfinding for the rest of the
            // approach and hand control to the reactive detour layer, which is what produced the
            // wander-off-and-never-commit behaviour - the final-approach handoff in
            // UpdateGlobalPath solves that case properly now, so re-routing is enough.
            if (useGlobalPathfinding && stuckRepathAttempts < maximumStuckRepathAttempts)
            {
                stuckRepathAttempts++;
                approachOre = null;
                ClearDetour();
                ResetGlobalPath();
                ResetProgressTracking();
                return;
            }

            if (hasCommandedTarget)
            {
                // A player command is stronger than the normal stuck-target timeout: never drop a
                // middle-clicked target back to the auto AI. Keep re-routing forever instead, and
                // let the attempt budget refill so the miner never stops trying.
                stuckRepathAttempts = 0;
                approachOre = null;
                ClearDetour();
                ResetGlobalPath();
                ResetProgressTracking();
                return;
            }

            if (targetLuckyBlock != null)
            {
                ignoredLuckyBlock = targetLuckyBlock;
                ignoredLuckyBlockUntil = Time.time + npcData.IgnoredTargetDuration;
            }
            else if (targetChest != null)
            {
                ignoredChest = targetChest;
                ignoredChestUntil = Time.time + npcData.IgnoredTargetDuration;
            }
            else
            {
                ignoredOre = targetOre;
                ignoredOreUntil = Time.time + npcData.IgnoredTargetDuration;
            }
            ReleaseTarget();
            nextTargetRefreshTime = 0f;
            ResetProgressTracking();
        }

        private bool IsBlockingOreCloser(Ore blockingOre, Vector3 currentPosition)
        {
            if (blockingOre == null || targetOre == null)
            {
                return false;
            }

            float blockingDistance = Mathf.Sqrt(blockingOre.SqrDistanceToSurface(currentPosition));
            float targetDistance = Mathf.Sqrt(targetOre.SqrDistanceToSurface(currentPosition));
            return blockingDistance + npcData.TargetSwitchDistanceAdvantage < targetDistance;
        }

        private void ResetProgressTracking()
        {
            lastProgressPosition = body != null ? body.position : transform.position;
            lastProgressTime = Time.time;
        }

        private bool TryGetBlockingMineable(Vector3 direction, float distance,
            out Component blocker,
            out Vector3 blockingPoint)
        {
            blocker = null;
            blockingPoint = Vector3.zero;
            Vector3 origin = body.position + Vector3.up * npcData.ColliderRadius;
            int hitCount = Physics.SphereCastNonAlloc(origin, GetObstacleProbeRadius(),
                direction, obstacleHits, distance, npcData.CollisionLayers,
                QueryTriggerInteraction.Ignore);
            float closestDistance = float.PositiveInfinity;
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = obstacleHits[index];
                Ore ore = hit.collider != null ? hit.collider.GetComponentInParent<Ore>() : null;
                if (ore != null) continue;
                MiningChest chest = hit.collider != null
                    ? hit.collider.GetComponentInParent<MiningChest>() : null;
                if (hit.distance >= closestDistance ||
                    (ore == null || ore == targetOre || ore.IsDepleted) &&
                    (chest == null || chest == targetChest || !chest.CanMine)) continue;

                blocker = ore != null && ore != targetOre && !ore.IsDepleted ? ore : chest;
                blockingPoint = hit.point;
                closestDistance = hit.distance;
            }

            return blocker != null;
        }

        private Vector3 CalculateObstacleAvoidance(Vector3 forward, Vector3 currentPosition,
            Component blocker, Vector3 blockingPoint)
        {
            Vector3 toBlocker = blockingPoint - currentPosition;
            toBlocker.y = 0f;
            Vector3 side = Vector3.Cross(Vector3.up, forward).normalized;
            if (blocker != avoidanceObstacle)
            {
                avoidanceObstacle = blocker;
                float sideDot = Vector3.Dot(toBlocker, side);
                if (Mathf.Abs(sideDot) <= Mathf.Epsilon)
                {
                    sideDot = 1f;
                }

                avoidanceSide = sideDot > 0f ? -1f : 1f;
            }

            return (forward + side * avoidanceSide * npcData.ObstacleAvoidanceStrength).normalized;
        }

        private Vector3 ResolveBlockedPath(Vector3 forward, Vector3 currentPosition,
            Component blocker, Vector3 blockingPoint)
        {
            if (TryCreateDetourWaypoint(forward, currentPosition, blocker,
                out Vector3 waypointDirection))
            {
                return waypointDirection;
            }

            if (Time.time < detourDirectionUntil && detourDirection.sqrMagnitude > Mathf.Epsilon &&
                GetOreClearance(detourDirection, npcData.DetourProbeDistance) >=
                npcData.DetourMinimumClearance)
            {
                return detourDirection;
            }

            Vector3 normalAvoidance = CalculateObstacleAvoidance(
                forward, currentPosition, blocker, blockingPoint);
            Vector3 bestDirection = normalAvoidance;
            float bestClearance = GetOreClearance(normalAvoidance, npcData.DetourProbeDistance);

            // Prefer the previously selected side when two directions have similar clearance.
            float signedAngle = npcData.DetourAngle * avoidanceSide;
            EvaluateDetourCandidate(forward, signedAngle, ref bestDirection, ref bestClearance);
            EvaluateDetourCandidate(forward, -signedAngle, ref bestDirection, ref bestClearance);
            EvaluateDetourCandidate(forward, signedAngle * 2f, ref bestDirection, ref bestClearance);
            EvaluateDetourCandidate(forward, -signedAngle * 2f, ref bestDirection, ref bestClearance);
            EvaluateDetourCandidate(forward, signedAngle * 3f, ref bestDirection, ref bestClearance);
            EvaluateDetourCandidate(forward, -signedAngle * 3f, ref bestDirection, ref bestClearance);

            if (bestClearance < npcData.DetourMinimumClearance)
            {
                // A closed pair or cluster needs space before another route can be evaluated.
                Vector3 reverseDirection = -forward;
                float reverseClearance = GetOreClearance(
                    reverseDirection, npcData.DetourProbeDistance);
                if (reverseClearance > bestClearance + 0.01f)
                {
                    bestDirection = reverseDirection;
                }
            }

            detourDirection = bestDirection.normalized;
            detourDirectionUntil = Time.time + npcData.DetourDirectionHoldTime;
            ResetProgressTracking();
            return detourDirection;
        }

        private static bool IsMineableObstacleActive(Component obstacle)
        {
            if (obstacle is Ore ore) return ore.isActiveAndEnabled && !ore.IsDepleted;
            if (obstacle is MiningChest chest) return chest.CanMine;
            return false;
        }

        private static bool TryGetMineableObstacleBounds(Component obstacle, out Bounds bounds)
        {
            if (obstacle is Ore ore) return ore.TryGetWorldBounds(out bounds);
            if (obstacle is MiningChest chest) return chest.TryGetWorldBounds(out bounds);
            bounds = default;
            return false;
        }

        private bool TryCreateDetourWaypoint(Vector3 forward, Vector3 currentPosition,
            Component blocker, out Vector3 waypointDirection)
        {
            waypointDirection = Vector3.zero;

            // Already mid-route around this exact ore - keep following that route instead of
            // recalculating from the current position/heading every frame. Recomputing here
            // was the actual pathfinding bug: inside a dense cluster, tiny frame-to-frame shifts
            // in position/forward flip the positive-vs-negative clearance comparison below back
            // and forth, so the NPC kept switching which side to go around on and never actually
            // made progress past the ore (looked "stuck"/jittering in place, most noticeable on
            // a middle-click commanded target since commanded NPCs aren't allowed to just switch
            // to a different ore instead - see the `!hasCommandedTarget` check in FixedUpdate).
            if (hasDetourWaypoint && blocker == detourWaypointObstacle &&
                IsMineableObstacleActive(blocker))
            {
                waypointDirection = detourWaypoint - currentPosition;
                waypointDirection.y = 0f;
                if (waypointDirection.sqrMagnitude > Mathf.Epsilon)
                {
                    waypointDirection.Normalize();
                    return true;
                }
            }

            if (!TryGetMineableObstacleBounds(blocker, out Bounds bounds))
            {
                return false;
            }

            forward.y = 0f;
            if (forward.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            forward.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, forward).normalized;
            float probeRadius = GetObstacleProbeRadius();
            float forwardExtent = Mathf.Abs(forward.x) * bounds.extents.x +
                                  Mathf.Abs(forward.z) * bounds.extents.z;
            float sideExtent = Mathf.Abs(side.x) * bounds.extents.x +
                               Mathf.Abs(side.z) * bounds.extents.z;
            float forwardPadding = probeRadius + npcData.StandSlotSpacingPadding;
            float sidePadding = probeRadius + npcData.StandSlotSpacingPadding * 2f;

            Vector3 nearCorner = bounds.center - forward * (forwardExtent + forwardPadding);
            nearCorner.y = currentPosition.y;
            Vector3 farCorner = bounds.center + forward * (forwardExtent + forwardPadding);
            farCorner.y = currentPosition.y;
            Vector3 positiveWaypoint = nearCorner + side * (sideExtent + sidePadding);
            Vector3 negativeWaypoint = nearCorner - side * (sideExtent + sidePadding);
            Vector3 positiveExit = farCorner + side * (sideExtent + sidePadding);
            Vector3 negativeExit = farCorner - side * (sideExtent + sidePadding);

            float positiveClearance = GetWaypointClearance(currentPosition, positiveWaypoint) +
                                      GetWaypointClearance(positiveWaypoint, positiveExit);
            float negativeClearance = GetWaypointClearance(currentPosition, negativeWaypoint) +
                                      GetWaypointClearance(negativeWaypoint, negativeExit);
            bool choosePositive;
            if (Mathf.Abs(positiveClearance - negativeClearance) <= 0.05f)
            {
                choosePositive = avoidanceSide >= 0f;
            }
            else
            {
                choosePositive = positiveClearance > negativeClearance;
            }

            detourWaypoint = choosePositive ? positiveWaypoint : negativeWaypoint;
            detourExitWaypoint = choosePositive ? positiveExit : negativeExit;
            avoidanceSide = choosePositive ? 1f : -1f;
            detourWaypointObstacle = blocker;
            hasDetourWaypoint = true;
            hasDetourExitWaypoint = true;
            avoidanceObstacle = blocker;
            detourDirectionUntil = Time.time + npcData.DetourDirectionHoldTime;

            waypointDirection = detourWaypoint - currentPosition;
            waypointDirection.y = 0f;
            if (waypointDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                ClearDetour();
                return false;
            }

            waypointDirection.Normalize();
            detourDirection = waypointDirection;
            ResetProgressTracking();
            return true;
        }

        private bool TryGetDetourWaypoint(Vector3 currentPosition, out Vector3 waypoint)
        {
            waypoint = default;
            if (!hasDetourWaypoint || !IsMineableObstacleActive(detourWaypointObstacle))
            {
                ClearDetour();
                return false;
            }

            Vector3 waypointOffset = detourWaypoint - currentPosition;
            waypointOffset.y = 0f;
            float reachedDistance = Mathf.Max(npcData.StoppingDistance,
                npcData.ColliderRadius * 0.35f);
            if (waypointOffset.sqrMagnitude <= reachedDistance * reachedDistance)
            {
                if (!hasDetourExitWaypoint)
                {
                    ClearDetour();
                    return false;
                }

                detourWaypoint = detourExitWaypoint;
                hasDetourExitWaypoint = false;
                waypointOffset = detourWaypoint - currentPosition;
                waypointOffset.y = 0f;
                if (waypointOffset.sqrMagnitude <= reachedDistance * reachedDistance)
                {
                    ClearDetour();
                    return false;
                }
            }

            waypoint = detourWaypoint;
            return true;
        }

        private float GetWaypointClearance(Vector3 currentPosition, Vector3 waypoint)
        {
            Vector3 offset = waypoint - currentPosition;
            offset.y = 0f;
            float distance = offset.magnitude;
            return distance <= Mathf.Epsilon
                ? 0f
                : GetOreClearanceFrom(currentPosition, offset / distance, distance);
        }

        private void EvaluateDetourCandidate(Vector3 forward, float angle,
            ref Vector3 bestDirection, ref float bestClearance)
        {
            Vector3 candidate = Quaternion.AngleAxis(angle, Vector3.up) * forward;
            candidate.y = 0f;
            candidate.Normalize();
            float clearance = GetOreClearance(candidate, npcData.DetourProbeDistance);
            if (clearance > bestClearance + 0.01f)
            {
                bestDirection = candidate;
                bestClearance = clearance;
            }
        }

        private float GetOreClearance(Vector3 direction, float distance)
        {
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return 0f;
            }

            return GetOreClearanceFrom(body.position, direction, distance);
        }

        private float GetOreClearanceFrom(Vector3 worldPosition, Vector3 direction,
            float distance)
        {
            Vector3 origin = worldPosition + Vector3.up * npcData.ColliderRadius;
            int hitCount = Physics.SphereCastNonAlloc(origin, GetObstacleProbeRadius(),
                direction.normalized, obstacleHits, distance, npcData.CollisionLayers,
                QueryTriggerInteraction.Ignore);
            float nearestDistance = distance;
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = obstacleHits[index];
                Ore ore = hit.collider != null ? hit.collider.GetComponentInParent<Ore>() : null;
                if (ore != null) continue;
                MiningChest chest = hit.collider != null
                    ? hit.collider.GetComponentInParent<MiningChest>() : null;
                if ((ore == null || ore == targetOre || ore.IsDepleted) &&
                    (chest == null || chest == targetChest || !chest.CanMine)) continue;

                nearestDistance = Mathf.Min(nearestDistance, hit.distance);
            }

            return nearestDistance;
        }

        private float GetObstacleProbeRadius()
        {
            // The route probe must be at least as wide as the physical capsule. The authored
            // data currently uses a 0.32 probe for a 0.5-radius NPC, which lets the query report
            // a clear route that the Rigidbody cannot physically fit through.
            return Mathf.Max(npcData.ObstacleProbeRadius,
                npcData.ColliderRadius + npcData.StandSlotSpacingPadding);
        }

        /// <summary>
        /// Requests (or reuses) a global shortest-path route to standPosition via
        /// MiningNavigation (NavMesh first, MiningNavGrid A* as fallback). Throttled by
        /// repathInterval/repathTargetMoveThreshold so it doesn't re-query every frame. If no
        /// backend can produce a route, currentPath is cleared and FixedUpdate falls straight back
        /// to the old direct-line reactive steering for standPosition.
        /// </summary>
        private void UpdateGlobalPath(Vector3 currentPosition, Vector3 standPosition)
        {
            if (!useGlobalPathfinding)
            {
                inFinalApproach = false;
                currentPathSource = MiningPathSource.None;
                if (currentPath.Count > 0)
                {
                    currentPath.Clear();
                }

                return;
            }

            if (CanUseDirectFinalApproach(currentPosition, standPosition))
            {
                inFinalApproach = true;
                if (currentPath.Count > 0)
                {
                    currentPath.Clear();
                    pathWaypointIndex = 0;
                }

                return;
            }

            // Left the final-approach radius (pushed out, or the target moved): allow routing
            // again immediately instead of waiting out the repath throttle.
            if (inFinalApproach)
            {
                inFinalApproach = false;
                nextRepathTime = 0f;
            }

            bool targetMoved = !hasPathTarget || (standPosition - lastPathTarget).sqrMagnitude >
                repathTargetMoveThreshold * repathTargetMoveThreshold;
            // Failed/exhausted paths still obey the throttle; don't query every frame.
            if (Time.time < nextRepathTime && !targetMoved)
            {
                return;
            }

            nextRepathTime = Time.time + repathInterval;
            lastPathTarget = standPosition;
            hasPathTarget = true;

            if (MiningNavigation.TryFindPath(currentPosition, standPosition, pathRequestBuffer,
                    out MiningPathSource pathSource, UnityEngine.AI.NavMesh.AllAreas,
                    navMeshSampleRadius))
            {
                currentPath.Clear();
                currentPath.AddRange(pathRequestBuffer);
                pathWaypointIndex = 0;
                currentPathSource = pathSource;
            }
            else
            {
                currentPath.Clear();
                currentPathSource = MiningPathSource.None;
            }
        }

        private float GetMoveSpeedMultiplier()
        {
            float multiplier = progressionSystem != null
                ? progressionSystem.CurrentMoveSpeedMultiplier
                : oreSpawner != null && oreSpawner.UpgradeSystem != null
                    ? oreSpawner.UpgradeSystem.GetMultiplier(MiningUpgradeType.NpcMoveSpeed)
                    : 1f;
            return Mathf.Max(0.01f, multiplier);
        }

        private bool CanUseDirectFinalApproach(Vector3 currentPosition, Vector3 standPosition)
        {
            Vector3 toStand = standPosition - currentPosition;
            toStand.y = 0f;
            float distance = toStand.magnitude;
            if (distance > finalApproachDistance)
            {
                return false;
            }

            if (distance <= npcData.StoppingDistance)
            {
                return true;
            }

            return !TryGetBlockingMineable(toStand / distance, distance, out _, out _);
        }

        /// <summary>
        /// Limits velocity before the next route corner. A NavMeshAgent normally provides this
        /// steering internally; this NPC deliberately uses a Rigidbody, so its route follower
        /// must perform the equivalent slowdown itself to avoid overshooting a left/right turn.
        /// </summary>
        private float GetCornerSpeedLimit(Vector3 currentPosition, float maximumSpeed,
            float brakingAcceleration)
        {
            if (pathWaypointIndex >= currentPath.Count - 1)
            {
                return maximumSpeed;
            }

            Vector3 corner = currentPath[pathWaypointIndex];
            Vector3 approach = corner - currentPosition;
            approach.y = 0f;
            float cornerDistance = approach.magnitude;
            if (cornerDistance <= Mathf.Epsilon)
            {
                return maximumSpeed;
            }

            Vector3 exit = currentPath[pathWaypointIndex + 1] - corner;
            exit.y = 0f;
            if (exit.sqrMagnitude <= Mathf.Epsilon)
            {
                return maximumSpeed;
            }

            float turnAmount = Mathf.InverseLerp(15f, 150f,
                Vector3.Angle(approach, exit));
            if (turnAmount <= Mathf.Epsilon)
            {
                return maximumSpeed;
            }

            // Keep enough motion to round gentle corners, while tight turns slow substantially.
            float turnSpeed = Mathf.Lerp(maximumSpeed, maximumSpeed * 0.2f, turnAmount);
            float turnRadius = Mathf.Max(npcData.ColliderRadius, npcData.StoppingDistance);
            return Mathf.Sqrt(turnSpeed * turnSpeed + 2f * brakingAcceleration *
                Mathf.Max(0f, cornerDistance - turnRadius));
        }

        /// <summary>
        /// Returns the point along the global path to steer toward. Advances past every waypoint
        /// already reached, then aims pathLookAheadDistance further along the polyline rather than
        /// exactly at the next corner - steering at the corner itself makes the miner hug it and
        /// then snap onto the next heading, which is the zig-zag. Falls back to fallbackTarget
        /// (the direct stand position) when there is no active path or the route is used up.
        /// </summary>
        private Vector3 GetNavigationTarget(Vector3 currentPosition, Vector3 fallbackTarget)
        {
            if (currentPath.Count == 0)
            {
                return fallbackTarget;
            }

            float reach = Mathf.Max(npcData.StoppingDistance, npcData.ColliderRadius * 0.5f);
            // Note this advances past the LAST waypoint too. The previous version stopped at
            // Count - 1, so an arrived miner kept steering at a corner it was already standing
            // on: zero movement, and the stuck timer was the only way out.
            while (pathWaypointIndex < currentPath.Count)
            {
                Vector3 offset = currentPath[pathWaypointIndex] - currentPosition;
                offset.y = 0f;
                if (offset.sqrMagnitude > reach * reach)
                {
                    break;
                }

                pathWaypointIndex++;
            }

            if (pathWaypointIndex >= currentPath.Count)
            {
                // Whole route consumed - whatever is left is the final approach.
                currentPath.Clear();
                pathWaypointIndex = 0;
                return fallbackTarget;
            }

            Vector3 lookAhead = GetLookAheadPoint(currentPosition, fallbackTarget);
            // Smoothing must not cut through an ore at a tight corner. Shorten the
            // look-ahead before falling back to the actual NavMesh corner.
            for (int attempt = 0; attempt < 4; attempt++)
            {
                if (MiningNavigation.IsMineableSegmentClear(currentPosition, lookAhead,
                    NavigationRadius, targetOre != null ? (Component)targetOre : targetChest)) return lookAhead;
                lookAhead = Vector3.Lerp(currentPath[pathWaypointIndex], lookAhead, .5f);
            }
            return currentPath[pathWaypointIndex];
        }

        /// <summary>
        /// Walks forward along the remaining route accumulating distance, and returns the point
        /// pathLookAheadDistance along it (interpolated inside whichever segment that lands in).
        /// Running off the end of the route returns the real destination, so the miner aims at
        /// where it is actually going rather than at the last corner.
        /// </summary>
        private Vector3 GetLookAheadPoint(Vector3 currentPosition, Vector3 fallbackTarget)
        {
            float remaining = pathLookAheadDistance;
            Vector3 segmentStart = currentPosition;
            for (int index = pathWaypointIndex; index < currentPath.Count; index++)
            {
                Vector3 segmentEnd = currentPath[index];
                Vector3 segment = segmentEnd - segmentStart;
                segment.y = 0f;
                float segmentLength = segment.magnitude;
                if (segmentLength >= remaining)
                {
                    return segmentLength <= Mathf.Epsilon
                        ? segmentEnd
                        : segmentStart + segment * (remaining / segmentLength);
                }

                remaining -= segmentLength;
                segmentStart = segmentEnd;
            }

            return fallbackTarget;
        }

        private void ResetGlobalPath()
        {
            currentPath.Clear();
            pathWaypointIndex = 0;
            hasPathTarget = false;
            nextRepathTime = 0f;
            inFinalApproach = false;
            currentPathSource = MiningPathSource.None;
        }

    }
}
