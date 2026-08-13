using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.GameData;
using LibreLancer.Data.Schema.Pilots;
using LibreLancer.Data.Schema.Ships;
using LibreLancer.Data.Schema.Solar;
using LibreLancer.Missions;
using LibreLancer.Server.Ai;
using LibreLancer.World;
using LibreLancer.World.Components;
using Pilot = LibreLancer.Data.GameData.Pilot;

namespace LibreLancer.Server.Components
{
    public class SNPCComponent : SRepComponent
    {
        public Bodypart? CommHead;
        public Bodypart? CommBody;
        public Accessory? CommHelmet;

        public AiState? CurrentDirective;
        private NPCManager manager;
        public MissionRuntime? MissionRuntime;

        public Pilot? Pilot;
        public StateGraph? StateGraph;

        private Random random = new();

        public float GetStateValue(StateGraphEntry row, StateGraphEntry column, float defaultVal = 0.0f)
        {
            if (StateGraph == null)
            {
                return defaultVal;
            }

            if ((int) row >= StateGraph.Data.Count)
            {
                return defaultVal;
            }

            var tableRow = StateGraph.Data[(int) row];

            if ((int) column >= tableRow.Length)
            {
                return defaultVal;
            }

            return tableRow[(int) column];
        }


        public SNPCComponent(GameObject parent, NPCManager manager, StateGraph stateGraph) : base(parent)
        {
            this.manager = manager;
            StateGraph = stateGraph;
        }

        public void StartTradelane()
        {
            if (Parent.TryGetComponent<ShipPhysicsComponent>(out var component))
            {
                component.Active = false;
            }
        }

        public void Docked()
        {
            manager.Despawn(Parent, false);
        }

        public void Attack(GameObject tgt, GameWorld world)
        {
            SetState(new AiAttackState(tgt), world);
        }

        public void SetState(AiState? state, GameWorld world)
        {
            this.CurrentDirective = state;
            lastStateChangeReason = state == null ? "directive cleared" : $"directive set: {state.GetDebugInfo()}";
            lastBlockReason = state == null ? "none" : "directive active";
            state?.OnStart(Parent, world, this);
        }

        private Dictionary<AttackTarget, int> attackPref = new();
        private GameObject? stayInRangeObject;
        private Vector3 stayInRangePoint;
        private float stayInRangeRadius;

        public void SetPilot(Pilot? pilot)
        {
            Pilot = pilot;
            attackPref = new Dictionary<AttackTarget, int>();

            if (pilot == null)
            {
                return;
            }

            if (Pilot!.Job == null)
            {
                return;
            }

            for (int i = 0; i < Pilot.Job.AttackPreferences.Count; i++)
            {
                int weight = Pilot.Job.AttackPreferences.Count - i;

                attackPref[Pilot.Job.AttackPreferences[i].Target] = weight;
            }
        }

        public void SetStayInRange(GameObject? target, Vector3 point, float radius)
        {
            stayInRangeObject = target;
            stayInRangePoint = point;
            stayInRangeRadius = MathF.Max(0, radius);
        }

        public void ClearStayInRange()
        {
            stayInRangeObject = null;
            stayInRangePoint = Vector3.Zero;
            stayInRangeRadius = 0;
        }

        private bool TryGetStayInRangeCenter(out Vector3 center)
        {
            if (stayInRangeRadius <= 0)
            {
                center = Vector3.Zero;
                return false;
            }
            center = stayInRangeObject?.WorldTransform.Position ?? stayInRangePoint;
            return stayInRangeObject == null || stayInRangeObject.Flags.HasFlag(GameObjectFlags.Exists);
        }

        public static AttackTarget ClassifyAttackTarget(GameObject obj)
        {
            if (obj.TryGetComponent<ShipComponent>(out var ship))
            {
                return ship.Ship.ShipType switch
                {
                    ShipType.Fighter => AttackTarget.Fighter,
                    ShipType.Freighter => AttackTarget.Freighter,
                    ShipType.Gunboat => AttackTarget.Gunboat,
                    ShipType.Cruiser => AttackTarget.Cruiser,
                    ShipType.Transport => AttackTarget.Transport,
                    ShipType.Capital => AttackTarget.Capital,
                    _ => AttackTarget.Anything
                };
            }

            return obj.SystemObject?.Archetype?.Type switch
            {
                ArchetypeType.jump_gate or ArchetypeType.jump_hole or ArchetypeType.jumphole => AttackTarget.Jumpgate,
                ArchetypeType.weapons_platform => AttackTarget.Weapons_Platform,
                ArchetypeType.destroyable_depot => AttackTarget.Destroyable_Depot,
                ArchetypeType.tradelane_ring => AttackTarget.Tradelane,
                _ when obj.Kind == GameObjectKind.Solar => AttackTarget.Solar,
                _ => AttackTarget.Anything
            };
        }

        private int GetHostileWeight(GameObject obj)
        {
            if (manager.HostileClamp &&
                "player".Equals(obj.Nickname, StringComparison.OrdinalIgnoreCase))
            {
                if (manager.AttackingPlayer >= manager.PlayerEnemyClampMax)
                    return -100;
                if (manager.AttackingPlayer < manager.PlayerEnemyClampMin)
                    return 100;
            }

            var target = ClassifyAttackTarget(obj);
            if (attackPref.TryGetValue(target, out var weight))
                return weight;
            return attackPref.GetValueOrDefault(AttackTarget.Anything);
        }

        private double missileTimer;

        public bool ShouldFireMissiles(double time)
        {
            missileTimer -= time;

            if (missileTimer <= 0)
            {
                missileTimer = ValueWithVariance(Pilot?.Missile?.LaunchIntervalTime,
                    Pilot?.Missile?.LaunchVariancePercent);
                return true;
            }

            return false;
        }

        private float ValueWithVariance(float? value, float? variance)
        {
            if (value == null)
            {
                return 0;
            }

            var b = value.Value;
            var v = variance.HasValue ? random.NextFloat(-variance.Value, variance.Value) : 0;
            return b + (b * v);
        }

        private bool inBurst = false;
        private float burstTimer = 0;
        private float fireTimer = 0;
        private int weaponGroupIndex = 0;

        public bool RunFireTimers(float dt)
        {
            if (inBurst)
            {
                burstTimer -= dt;

                if (burstTimer <= 0)
                {
                    inBurst = false;
                    burstTimer = Pilot?.Gun?.FireNoBurstIntervalTime ?? 0;
                }
                else
                {
                    // Handle regular guns
                    fireTimer -= dt;

                    if (fireTimer <= 0)
                    {
                        var interval = Pilot?.Gun?.FireIntervalTime ?? 0;
                        if (interval == 0)
                            interval = 0.1f;

                        fireTimer = ValueWithVariance(interval,
                            Pilot?.Gun?.FireIntervalVariancePercent);
                        return true;
                    }
                }
            }
            else
            {
                burstTimer -= dt;

                if (burstTimer <= 0)
                {
                    inBurst = true;
                    burstTimer = ValueWithVariance(Pilot?.Gun?.FireBurstIntervalTime ?? 1f,
                        Pilot?.Gun?.FireBurstIntervalVariancePercent);
                    fireTimer = 0;
                }
            }

            return false;
        }

        public void FireWeaponGroups(WeaponControlComponent weapons, GameWorld world)
        {
            var regularGuns = new List<GunComponent>();

            foreach (var gun in Parent.GetChildComponents<GunComponent>())
            {
                if (!gun.Object.Def.AutoTurret)
                    regularGuns.Add(gun);
            }

            if (regularGuns.Count == 0)
                return;

            var burstInterval = Pilot?.Gun?.FireBurstIntervalTime ?? 1f;
            var weaponsToFire = burstInterval switch
            {
                < 0.3f => Math.Max(1, regularGuns.Count / 2),
                < 1.0f => Math.Max(1, regularGuns.Count / 3),
                _ => Math.Max(1, regularGuns.Count / 4)
            };

            for (var i = 0; i < weaponsToFire && i < regularGuns.Count; i++)
            {
                var weaponIndex = (weaponGroupIndex + i) % regularGuns.Count;
                regularGuns[weaponIndex].Fire(weapons.AimPoint, world);
            }

            weaponGroupIndex = (weaponGroupIndex + weaponsToFire) % regularGuns.Count;
        }

        private Vector3 AddInaccuracy(Vector3 target, Vector3 myPos, float distance, float maxRange,
            bool isAutoTurret = false)
        {
            if (Pilot?.Gun == null || distance <= 0)
            {
                return target;
            }

            float angleDeg = Pilot.Gun.FireAccuracyConeAngle;

            if (angleDeg <= 0)
            {
                return target;
            }

            float cone = angleDeg * MathF.PI / 180f;

            Vector3 dir = Vector3.Normalize(target - myPos);

            Vector3 randomVec = random.NextUnitVector();

            float dot = Vector3.Dot(dir, randomVec);
            float currentAngle = MathF.Acos(dot);

            if (currentAngle > cone)
            {
                float t = cone / currentAngle;
                randomVec = Vector3.Normalize(Vector3.Lerp(dir, randomVec, t));
            }

            return myPos + randomVec * distance;
        }


        private GameObject? lastShootAt;

        public Vector3 GetAimPosition(GameObject other, WeaponControlComponent weapons, bool isAutoTurret = false)
            => GetAimPosition(other, weapons.GetAverageGunSpeed(), weapons.GetGunMaxRange(), isAutoTurret);

        private Vector3 GetAimPosition(GameObject other, float projectileSpeed, float maxRange,
            bool isAutoTurret)
        {
            var myPos = Parent.PhysicsComponent?.Body.Position ?? Parent.WorldTransform.Position;
            var myVelocity = Parent.PhysicsComponent?.Body.LinearVelocity ?? Vector3.Zero;
            var otherPos = other.PhysicsComponent?.Body.Position ?? other.WorldTransform.Position;
            var otherVelocity = other.PhysicsComponent?.Body.LinearVelocity ?? Vector3.Zero;

            if (projectileSpeed > float.Epsilon &&
                Aiming.GetTargetLeading(otherPos - myPos, otherVelocity - myVelocity, projectileSpeed, out var t))
            {
                var predictedPos = otherPos + otherVelocity * t;
                var leadDist = Vector3.Distance(myPos, predictedPos);
                return AddInaccuracy(predictedPos, myPos, leadDist, maxRange, isAutoTurret);
            }

            var staticDist = Vector3.Distance(myPos, otherPos);
            return AddInaccuracy(otherPos, myPos, staticDist, maxRange, isAutoTurret);
        }

        private GameObject? GetHostileAndFire(double time, GameWorld world)
        {
            // Get hostile
            GameObject? shootAt = null;
            int shootAtWeight = -1000;
            float shootAtDistance = float.MaxValue;
            var myPos = Parent.WorldTransform.Position;
            var hasStayInRange = TryGetStayInRangeCenter(out var stayInRangeCenter);

            foreach (var other in world.SpatialLookup
                         .GetNearbyObjects(Parent, myPos, 5000))
            {
                if ((other.Flags & GameObjectFlags.Cloaked) == GameObjectFlags.Cloaked)
                {
                    continue;
                }

                if (other.TryGetComponent<STradelaneMoveComponent>(out _))
                {
                    continue;
                }

                if (!(Vector3.Distance(other.WorldTransform.Position, myPos) < 5000) ||
                    !IsHostileTo(other))
                {
                    continue;
                }

                if (hasStayInRange &&
                    Vector3.DistanceSquared(other.WorldTransform.Position, stayInRangeCenter) >
                    stayInRangeRadius * stayInRangeRadius)
                {
                    continue;
                }

                int weight = GetHostileWeight(other);
                var distance = Vector3.DistanceSquared(other.WorldTransform.Position, myPos);

                if (weight > shootAtWeight || weight == shootAtWeight && distance < shootAtDistance)
                {
                    shootAtWeight = weight;
                    shootAtDistance = distance;
                    shootAt = other;
                }
            }

            Parent.GetComponent<SelectedTargetComponent>()!.Selected = shootAt;

            // Shoot at hostile
            if (shootAt != null && Parent.TryGetComponent<WeaponControlComponent>(out var weapons))
            {
                if ("player".Equals(shootAt.Nickname, StringComparison.OrdinalIgnoreCase))
                {
                    manager.AttackingPlayer++;
                }

                var dist = Vector3.Distance(shootAt.WorldTransform.Position, myPos);

                var gunRange = weapons.GetGunMaxRange() * 0.95f;
                weapons.AimPoint = GetAimPosition(shootAt, weapons, false); // Regular guns aim

                var missileMax = weapons.GetMissileMaxRange();
                var missileRange = Pilot?.Missile?.LaunchRange ?? missileMax;

                if (missileMax < missileRange)
                {
                    missileRange = missileMax;
                }

                // Fire Missiles
                if ((Pilot?.Missile?.MissileLaunchAllowOutOfRange ?? false) ||
                    dist <= missileRange)
                {
                    missileTimer -= time;

                    if (missileTimer <= 0)
                    {
                        weapons.FireMissiles(world);
                        missileTimer = ValueWithVariance(Pilot?.Missile?.LaunchIntervalTime,
                            Pilot?.Missile?.LaunchVariancePercent);
                        missileTimer = Pilot?.Missile?.LaunchIntervalTime ?? 0;
                    }
                }

                // Fire guns
                if (dist < gunRange)
                {
                    if (RunFireTimers((float) time))
                        FireWeaponGroups(weapons, world);
                }
            }
            else
            {
                // fireTimer = Pilot?.Gun?.FireIntervalTime ?? 0;
                // missileTimer = Pilot?.Missile?.LaunchIntervalTime ?? 0;
            }

            return shootAt;
        }

        private StateGraphEntry currentState = StateGraphEntry.NULL;
        private StateGraphEntry previousState = StateGraphEntry.NULL;

        private double timeInState = 0;
        private string lastTransitionTrace = "none";
        private string lastStateChangeReason = "initial";
        private string lastBlockReason = "none";

        public string GetDebugInfo()
        {
            string ls = lastShootAt == null ? "none" : lastShootAt.Nickname ?? "no nickname";
            var maxRange = 0f;

            if (Parent.TryGetComponent<WeaponControlComponent>(out var wp))
            {
                maxRange = wp.GetGunMaxRange() * 0.95f;
            }

            bool physActive = false;

            if (Parent.TryGetComponent<ShipPhysicsComponent>(out var ps))
            {
                physActive = ps.Active;
            }

            var formation = "";

            if (Parent.Formation != null)
            {
                formation = Parent.Formation.ToString();
            }

            // Debug weapon counts
            int totalGuns = 0;
            int autoTurrets = 0;
            int regularGuns = 0;

            foreach (var gun in Parent.GetChildComponents<GunComponent>())
            {
                totalGuns++;

                if (gun.Object.Def.AutoTurret)
                {
                    autoTurrets++;
                }
                else
                {
                    regularGuns++;
                }
            }

            AutopilotBehaviors beh = AutopilotBehaviors.None;

            if (Parent.TryGetComponent<AutopilotComponent>(out var ap))
            {
                beh = ap.CurrentBehavior;
            }

            var directive = CurrentDirective?.GetDebugInfo() ?? "null";
            var directiveRunnerActive = Parent.TryGetComponent<DirectiveRunnerComponent>(out var directiveRunner) && directiveRunner.Active;
            var selectedTarget = Parent.GetComponent<SelectedTargetComponent>()?.Selected;
            var target = selectedTarget ?? lastShootAt;
            var targetLabel = target == null ? "none" : string.IsNullOrWhiteSpace(target.Nickname) ? $"#{target.NetID}" : $"{target.Nickname} #{target.NetID}";
            var graphWeights =
                $"Face={GetStateValue(currentState, StateGraphEntry.Face):0.###}, " +
                $"Trail={GetStateValue(currentState, StateGraphEntry.Trail):0.###}, " +
                $"Buzz={GetStateValue(currentState, StateGraphEntry.Buzz):0.###}, " +
                $"Evade={GetStateValue(currentState, StateGraphEntry.Evade):0.###}";

            // Show accuracy info for debugging
            float npcPower = Pilot?.Gun?.FireAccuracyPowerNpc ?? 0;
            float npcAngle = Pilot?.Gun?.FireAccuracyConeAngle ?? 0;
            var autoTurretController = Parent.GetComponent<SAutoTurretComponent>();
            var autoTurretsTracking = autoTurretController?.TrackingCount ?? 0;
            var autoTurretFireTimer = autoTurretController?.FireTimer ?? 0;
            var autoTurretInBurst = autoTurretController?.InBurst ?? false;

            return
                $"Autopilot: {beh}\nShooting At: {ls}\n" +
                $"NPC AI\n" +
                $"Target: {targetLabel}\nBlock Reason: {lastBlockReason}\n" +
                $"Directive: {directive}\nDirective Runner Active: {directiveRunnerActive}\n" +
                $"State: {currentState} (previous {previousState}, {timeInState:F2}s)\n" +
                $"State Change: {lastStateChangeReason}\nTransition Weights: {graphWeights}\n" +
                $"Transition Trace: {lastTransitionTrace}\n" +
                $"Max Range: {maxRange}\nPhys Active: {physActive}\n" +
                $"Weapons: {totalGuns} total ({regularGuns} regular, {autoTurrets} auto-turrets)\n" +
                $"Fire Timer: {fireTimer:F2}\n" +
                $"Auto-Turrets Tracking: {autoTurretsTracking}, Fire Timer: {autoTurretFireTimer:F2}\n" +
                $"NPC Base Power: {npcPower} (higher=more inaccuracy)\n" +
                $"NPC Base Angle: {npcAngle}\n" +
                $"InBurst: {inBurst}, Auto-Turret InBurst: {autoTurretInBurst}\n{formation}";
        }

        private void Transition(params StateGraphEntry[] possible)
        {
            var from = currentState;
            var trace = new List<string>();

            foreach (var e in possible)
            {
                var weight = GetStateValue(currentState, e);
                var roll = random.NextSingle();
                var selected = roll < weight;
                trace.Add($"{e}: roll={roll:0.###}, weight={weight:0.###}, {(selected ? "selected" : "rejected")}");

                if (selected)
                {
                    EnterState(e, $"transition from {from}");
                    lastTransitionTrace = string.Join("; ", trace);
                    break;
                }
            }

            if (from == currentState)
            {
                lastTransitionTrace = trace.Count == 0 ? "no candidates" : string.Join("; ", trace);
            }
        }

        private float evadeX = 0;
        private float evadeY = 0;
        private float evadeZ = 0;
        private Vector3 buzzDirection;
        private bool evadeThrust = false;

        private void EnterState(StateGraphEntry e, string reason)
        {
            previousState = currentState;
            currentState = e;
            timeInState = 0;
            lastStateChangeReason = reason;

            if (e == StateGraphEntry.Evade)
            {
                var turnThrottle = Pilot?.EvadeBreak?.TurnThrottle ?? 1;
                var rollThrottle = Pilot?.EvadeBreak?.RollThrottle ?? 1;
                evadeX = turnThrottle * random.Next(-1, 2);
                evadeY = turnThrottle * random.Next(-1, 2);
                evadeZ = rollThrottle * random.Next(-1, 2);
                evadeThrust = random.Next(0, 2) == 1;
            }
            else if (e == StateGraphEntry.Buzz)
            {
                buzzDirection = random.NextUnitVector();
            }
        }

        private void ResetStateGraphState(string reason)
        {
            if (currentState != StateGraphEntry.NULL)
            {
                previousState = currentState;
            }

            currentState = StateGraphEntry.NULL;
            timeInState = 0;
            lastStateChangeReason = reason;
        }

        private double damageTimer = 3;
        private float damageTaken = 0;

        public void TakingDamage(float amount)
        {
            damageTimer = 3;
            damageTaken += amount;

            if (damageTaken > 100 &&
                currentState != StateGraphEntry.Evade &&
                GetStateValue(currentState, StateGraphEntry.Evade) > 0)
            {
                lastTransitionTrace = $"damage trigger: damage={damageTaken:0.#}, evadeWeight={GetStateValue(currentState, StateGraphEntry.Evade):0.###}";
                EnterState(StateGraphEntry.Evade, $"damage trigger: {damageTaken:0.#}");
            }
        }

        public override void Update(double time, GameWorld world)
        {
            if (!Parent.TryGetComponent<AutopilotComponent>(out var ap))
            {
                lastBlockReason = "missing autopilot";
                return;
            }

            if (ap.CurrentBehavior == AutopilotBehaviors.Undock)
            {
                lastBlockReason = "undocking";
                return; // no npc yet
            }

            damageTimer -= time;

            if (damageTimer < 0)
            {
                damageTimer = 0;
                damageTaken = 0;
            }

            CurrentDirective?.Update(Parent, world, this, time);

            var shootAt = GetHostileAndFire(time, world);
            lastShootAt = shootAt;

            var runningDirective = Parent.TryGetComponent<DirectiveRunnerComponent>(out var directiveRunner) &&
                                   directiveRunner.Active;

            if (CurrentDirective != null ||
                runningDirective ||
                shootAt == null ||
                ap.CurrentBehavior == AutopilotBehaviors.Formation)
            {
                if (CurrentDirective != null)
                {
                    lastBlockReason = "directive active";
                }
                else if (runningDirective)
                {
                    lastBlockReason = "directive runner active";
                }
                else if (shootAt == null)
                {
                    lastBlockReason = "no hostile target";
                }
                else
                {
                    lastBlockReason = "formation";
                }

                ResetStateGraphState(lastBlockReason);
                return;
            }

            lastBlockReason = "none";

            var si = Parent.GetComponent<ShipSteeringComponent>()!;
            timeInState += time;

            bool canTransition = false;

            var mypos = Parent.WorldTransform.Position;

            si.InThrottle = 0;
            si.InPitch = 0;
            si.InYaw = 0;
            si.InRoll = 0;
            si.Cruise = false;
            si.Thrust = false;

            switch (currentState)
            {
                case StateGraphEntry.NULL:
                    ap.Cancel();
                    canTransition = true;
                    break;
                case StateGraphEntry.Evade:
                    ap.Cancel();
                    si.InThrottle = 1;
                    si.Cruise = false;
                    si.Thrust = evadeThrust;
                    si.InPitch = evadeX;
                    si.InYaw = evadeY;
                    si.InRoll = evadeZ;
                    canTransition = timeInState >= (Pilot?.EvadeBreak?.Time ?? 5);
                    break;
                case StateGraphEntry.Buzz:
                {
                    var dist = Pilot?.BuzzPassBy?.DistanceToPassBy ?? 100;
                    var dest = shootAt.WorldTransform.Transform(buzzDirection * dist);
                    ap.GotoVec(dest, GotoKind.GotoNoCruise, 1, 0);
                    canTransition = timeInState >= (Pilot?.BuzzPassBy?.PassByTime ?? 5) ||
                                    Vector3.DistanceSquared(dest, mypos) < 16;
                    break;
                }
                case StateGraphEntry.Face:
                case StateGraphEntry.Trail:
                    ap.GotoObject(shootAt, GotoKind.GotoNoCruise, 1, Pilot?.Trail?.Distance ?? 150);
                    canTransition = timeInState >= 5;
                    break;
                default:
                    canTransition = true;
                    break;
            }

            if (canTransition)
            {
                Transition(StateGraphEntry.Face, StateGraphEntry.Trail, StateGraphEntry.Buzz);
            }
        }

        public void DockWith(GameObject tgt, GameWorld world)
        {
            SetState(new AiDockState(tgt, GotoKind.Goto), world);
        }
    }
}
