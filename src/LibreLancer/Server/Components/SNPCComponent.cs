using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.GameData;
using LibreLancer.Data.Schema.Pilots;
using LibreLancer.Missions;
using LibreLancer.Net.Protocol;
using LibreLancer.Server.Ai;
using LibreLancer.World;
using LibreLancer.World.Components;
using Pilot = LibreLancer.Data.GameData.Pilot;

namespace LibreLancer.Server.Components
{
    public class SNPCComponent : SRepComponent
    {
        public Bodypart CommHead;
        public Bodypart CommBody;
        public Accessory CommHelmet;

        public AiState CurrentDirective;
        private NPCManager manager;
        public MissionRuntime MissionRuntime;

        public Pilot Pilot;
        public StateGraph StateGraph;

        private Random random = new Random();

        public float GetStateValue(StateGraphEntry row, StateGraphEntry column, float defaultVal = 0.0f)
        {
            if (StateGraph == null) return defaultVal;
            if ((int) row >= StateGraph.Data.Count) return defaultVal;
            var tableRow = StateGraph.Data[(int) row];
            if ((int) column >= tableRow.Length) return defaultVal;
            return tableRow[(int) column];
        }



        public SNPCComponent(GameObject parent, NPCManager manager, StateGraph stateGraph) : base(parent)
        {
            this.manager = manager;
            StateGraph = stateGraph;
        }

        public void StartTradelane()
        {
            ShipPhysicsComponent component = Parent.GetComponent<ShipPhysicsComponent>();
            if (component is not null)
                component.Active = false;
        }

        public void Docked()
        {
            manager.Despawn(Parent, false);
        }

        private GameObject attack;
        public void Attack(GameObject tgt)
        {
            SetState(new AiAttackState(tgt));
        }

        public void SetState(AiState state)
        {
            this.CurrentDirective = state;
            state?.OnStart(Parent, this);
        }


        private Dictionary<GameObjectKind, int> attackPref = new Dictionary<GameObjectKind, int>();
        public void SetPilot(Pilot pilot)
        {
            Pilot = pilot;
            attackPref = new Dictionary<GameObjectKind, int>();
            if (pilot == null) return;

            if (Pilot.Job != null) {
                for (int i = 0; i < Pilot.Job.AttackPreferences.Count; i++)
                {
                    int weight = Pilot.Job.AttackPreferences.Count - i;
                    switch (Pilot.Job.AttackPreferences[i].Target)
                    {
                        case AttackTarget.Fighter:
                            attackPref[GameObjectKind.Ship] = weight;
                            break;
                        case AttackTarget.Solar:
                            attackPref[GameObjectKind.Solar] = weight;
                            break;
                    }
                }
            }
        }

        int GetHostileWeight(GameObject obj)
        {
            if ("player".Equals(obj.Nickname, StringComparison.OrdinalIgnoreCase) &&
                manager.AttackingPlayer > 2)
                return -100;
            if (attackPref.TryGetValue(obj.Kind, out var weight))
                return weight;
            return 0;
        }

        private double missileTimer;
        public bool ShouldFireMissiles(double time)
        {
            missileTimer -= time;
            if (missileTimer <= 0)
            {
                missileTimer = ValueWithVariance(Pilot?.Missile?.LaunchIntervalTime, Pilot?.Missile?.LaunchVariancePercent);
                return true;
            }
            return false;
        }

        float ValueWithVariance(float? value, float? variance)
        {
            if (value == null) return 0;
            var b = value.Value;
            var v = variance.HasValue ? random.NextFloat(-variance.Value, variance.Value) : 0;
            return b + (b * v);
        }

        private bool inBurst = false;
        private float burstTimer = 0;
        private float fireTimer = 0;
        private int fireCycle = 0; // Track cycles for weapon grouping
        private int weaponGroupIndex = 0; // Track which weapon group to fire

        public struct FireInfo
        {
            public bool ShouldFireRegular;
            public bool ShouldFireAutoTurrets;
        }

        public FireInfo RunFireTimers(float dt)
        {
            var fireInfo = new FireInfo { ShouldFireRegular = false, ShouldFireAutoTurrets = false };

            // Check if ship has auto-turret weapons
            bool hasAutoTurrets = false;
            if (Parent.TryGetComponent<WeaponControlComponent>(out var weapons))
            {
                foreach (var gun in Parent.GetChildComponents<GunComponent>())
                {
                    if (gun.Object.Def.AutoTurret)
                    {
                        hasAutoTurrets = true;
                        break;
                    }
                }
            }

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
                        if (interval == 0) interval = 0.1f; // minimum interval for NPCs
                        fireTimer = ValueWithVariance(interval,
                            Pilot?.Gun?.FireIntervalVariancePercent);
                        fireInfo.ShouldFireRegular = true;

                        // Auto-turrets fire based on their interval timing
                        if (hasAutoTurrets)
                        {
                            fireCycle++;
                            // Use auto-turret interval timing from INI
                            float autoTurretInterval = Pilot?.Gun?.AutoTurretIntervalTime ?? 0.2f;
                            if (autoTurretInterval <= 0 || fireCycle >= Math.Max(1, (int)(autoTurretInterval / 0.1f)))
                            {
                                fireInfo.ShouldFireAutoTurrets = true;
                                fireCycle = 0; // Reset cycle counter
                            }
                        }
                    }
                }
            }
            else
            {
                burstTimer -= dt;
                if (burstTimer <= 0){
                    inBurst = true;
                    burstTimer = ValueWithVariance(Pilot?.Gun?.FireBurstIntervalTime ?? 1f,
                        Pilot?.Gun?.FireBurstIntervalVariancePercent);
                    // Reset timer when starting new burst
                    fireTimer = 0;
                }
            }
            return fireInfo;
        }

        public void FireWeaponGroups(WeaponControlComponent weapons, FireInfo fireInfo)
        {
            // Get all weapons and group them by type
            var regularGuns = new List<GunComponent>();
            var autoTurrets = new List<GunComponent>();

            foreach (var gun in Parent.GetChildComponents<GunComponent>())
            {
                if (gun.Object.Def.AutoTurret)
                {
                    autoTurrets.Add(gun);
                }
                else
                {
                    regularGuns.Add(gun);
                }
            }

            // Create separate aim points for different weapon types due to accuracy differences
            Vector3 regularAim = weapons.AimPoint; // Use existing aim point for regular guns
            Vector3 autoTurretAim = weapons.AimPoint; // Will be recalculated with more inaccuracy

            // If auto-turrets are firing, get a less accurate aim point
            if (fireInfo.ShouldFireAutoTurrets && Parent.GetComponent<SelectedTargetComponent>()?.Selected is GameObject target)
            {
                autoTurretAim = GetAimPosition(target, weapons, true); // More inaccurate aim point
            }

            // Fire regular weapons in groups based on burst timing
            if (fireInfo.ShouldFireRegular && regularGuns.Count > 0)
            {
                // Use INI parameters to determine weapon grouping
                float burstInterval = Pilot?.Gun?.FireBurstIntervalTime ?? 1f;
                float fireInterval = Pilot?.Gun?.FireIntervalTime ?? 0.1f;
                float noBurstInterval = Pilot?.Gun?.FireNoBurstIntervalTime ?? 2f;

                // Determine weapon grouping strategy based on timing parameters
                int weaponsToFire;
                if (burstInterval < 0.3f)
                {
                    // Rapid fire - fire more weapons per burst
                    weaponsToFire = Math.Max(1, regularGuns.Count / 2); // 50% of weapons
                }
                else if (burstInterval < 1.0f)
                {
                    // Medium fire rate - fire moderate number of weapons
                    weaponsToFire = Math.Max(1, regularGuns.Count / 3); // 33% of weapons
                }
                else
                {
                    // Slow fire rate - fire fewer weapons per burst
                    weaponsToFire = Math.Max(1, regularGuns.Count / 4); // 25% of weapons
                }

                // Use weapon group cycling to distribute firing
                for (int i = 0; i < weaponsToFire && i < regularGuns.Count; i++)
                {
                    int weaponIndex = (weaponGroupIndex + i) % regularGuns.Count;
                    regularGuns[weaponIndex].Fire(regularAim);
                }

                // Advance weapon group for next firing cycle
                weaponGroupIndex = (weaponGroupIndex + weaponsToFire) % regularGuns.Count;
            }

            // Fire auto-turrets in groups with their own timing
            if (fireInfo.ShouldFireAutoTurrets && autoTurrets.Count > 0)
            {
                // Use auto-turret specific parameters for grouping
                float autoTurretBurstInterval = Pilot?.Gun?.AutoTurretBurstIntervalTime ?? 1f;

                // Auto-turrets typically fire fewer weapons per cycle
                int turretsToFire;
                if (autoTurretBurstInterval < 0.5f)
                {
                    turretsToFire = Math.Max(1, autoTurrets.Count / 2); // 50% for rapid auto-turrets
                }
                else
                {
                    turretsToFire = Math.Max(1, autoTurrets.Count / 4); // 25% for normal auto-turrets
                }

                for (int i = 0; i < turretsToFire && i < autoTurrets.Count; i++)
                {
                    int turretIndex = (fireCycle * turretsToFire + i) % autoTurrets.Count;
                    autoTurrets[turretIndex].Fire(autoTurretAim);
                }
            }
        }

        Vector3 AddInaccuracy(Vector3 target, Vector3 myPos, float distance, float maxRange, bool isAutoTurret = false)
        {
            if (Pilot?.Gun == null || distance <= 0)
                return target;

            float angleDeg = Pilot.Gun.FireAccuracyConeAngle;
            if (angleDeg <= 0)
                return target;

            float cone = angleDeg * MathF.PI / 180f;

            Vector3 dir = Vector3.Normalize(target - myPos);

            Vector3 randomVec;
            do
            {
                randomVec = new Vector3(
                    random.NextFloat(-1f, 1f),
                    random.NextFloat(-1f, 1f),
                    random.NextFloat(-1f, 1f)
                );
            } while (randomVec.LengthSquared() < 0.01f);

            randomVec = Vector3.Normalize(randomVec);

            float dot = Vector3.Dot(dir, randomVec);
            float currentAngle = MathF.Acos(dot);

            if (currentAngle > cone)
            {
                float t = cone / currentAngle;
                randomVec = Vector3.Normalize(Vector3.Lerp(dir, randomVec, t));
            }

            return myPos + randomVec * distance;
        }



        private GameObject lastShootAt;

        public Vector3 GetAimPosition(GameObject other, WeaponControlComponent weapons, bool isAutoTurret = false)
        {
            if (other.PhysicsComponent == null)
                return other.WorldTransform.Position;
            var myPos = Parent.PhysicsComponent.Body.Position;
            var myVelocity = Parent.PhysicsComponent.Body.LinearVelocity;
            var otherPos = other.PhysicsComponent.Body.Position;
            var otherVelocity = other.PhysicsComponent.Body.LinearVelocity;
            var avgSpeed = weapons.GetAverageGunSpeed();
            var maxRange = weapons.GetGunMaxRange();
            if (Aiming.GetTargetLeading((otherPos - myPos), (otherVelocity - myVelocity), avgSpeed, out var t))
            {
                var predictedPos = otherPos + otherVelocity * t;
                var leadDist = Vector3.Distance(myPos, predictedPos);
                return AddInaccuracy(predictedPos, myPos, leadDist, maxRange, isAutoTurret);
            }
            var staticDist = Vector3.Distance(myPos, otherPos);
            return AddInaccuracy(otherPos, myPos, staticDist, maxRange, isAutoTurret);
        }

        GameObject GetHostileAndFire(double time)
        {
            //Get hostile
            GameObject shootAt = null;
            int shootAtWeight = -1000;
            var myPos = Parent.WorldTransform.Position;
            foreach (var other in Parent.GetWorld().SpatialLookup
                         .GetNearbyObjects(Parent, myPos, 5000))
            {
                if ((other.Flags & GameObjectFlags.Cloaked) == GameObjectFlags.Cloaked)
                    continue;
                if (other.TryGetComponent<STradelaneMoveComponent>(out _))
                    continue;
                if (Vector3.Distance(other.WorldTransform.Position, myPos) < 5000 &&
                    IsHostileTo(other))
                {
                    int weight = GetHostileWeight(other);
                    if (weight > shootAtWeight)
                    {
                        shootAtWeight = weight;
                        shootAt = other;
                    }
                }
            }
            Parent.GetComponent<SelectedTargetComponent>().Selected = shootAt;
            //Shoot at hostile
            if (shootAt != null && Parent.TryGetComponent<WeaponControlComponent>(out var weapons))
            {
                if ("player".Equals(shootAt.Nickname, StringComparison.OrdinalIgnoreCase))
                    manager.AttackingPlayer++;

                var dist = Vector3.Distance(shootAt.WorldTransform.Position, myPos);

                var gunRange = weapons.GetGunMaxRange() * 0.95f;
                weapons.AimPoint = GetAimPosition(shootAt, weapons, false); // Regular guns aim

                var missileMax = weapons.GetMissileMaxRange();
                var missileRange = Pilot?.Missile?.LaunchRange ?? missileMax;
                if(missileMax < missileRange)
                    missileRange = missileMax;
                //Fire Missiles
                if ((Pilot?.Missile?.MissileLaunchAllowOutOfRange ?? false) ||
                    dist <= missileRange)
                {
                    missileTimer -= time;
                    if (missileTimer <= 0)
                    {
                        weapons.FireMissiles();
                        missileTimer = ValueWithVariance(Pilot?.Missile?.LaunchIntervalTime,
                            Pilot?.Missile?.LaunchVariancePercent);
                        missileTimer = Pilot?.Missile?.LaunchIntervalTime ?? 0;
                    }
                }
                //Fire guns
                if (dist < gunRange)
                {
                    var fireInfo = RunFireTimers((float)time);
                    if (fireInfo.ShouldFireRegular || fireInfo.ShouldFireAutoTurrets)
                    {
                        // Fire regular guns and auto-turrets separately based on their timers
                        FireWeaponGroups(weapons, fireInfo);
                    }
                }
            }
            else
            {
                //fireTimer = Pilot?.Gun?.FireIntervalTime ?? 0;
                //missileTimer = Pilot?.Missile?.LaunchIntervalTime ?? 0;
            }

            return shootAt;
        }

        private StateGraphEntry currentState = StateGraphEntry.NULL;

        private double timeInState = 0;

        public string GetDebugInfo()
        {
            string ls = lastShootAt == null ? "none" :
                lastShootAt.Nickname ?? "no nickname";
            var maxRange = 0f;
            if(Parent.TryGetComponent<WeaponControlComponent>(out var wp))
                 maxRange = wp.GetGunMaxRange() * 0.95f;
            bool physActive = false;
            if (Parent.TryGetComponent<ShipPhysicsComponent>(out var ps))
                physActive = ps.Active;

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
                    autoTurrets++;
                else
                    regularGuns++;
            }

            AutopilotBehaviors beh = AutopilotBehaviors.None;
            if (Parent.TryGetComponent<AutopilotComponent>(out var ap))
            {
                beh = ap.CurrentBehavior;
            }
            // Show accuracy info for debugging
            float npcPower = Pilot?.Gun?.FireAccuracyPowerNpc ?? 0;
            float npcAngle = Pilot?.Gun?.FireAccuracyConeAngle ?? 0;

            return $"Autopilot: {beh}\nShooting At: {ls}\nDirective: {CurrentDirective?.ToString() ?? "null"}\nState: {currentState}\nMax Range: {maxRange}\nPhys Active: {physActive}\nWeapons: {totalGuns} total ({regularGuns} regular, {autoTurrets} auto-turrets)\nTimer: {fireTimer:F2}, Cycle: {fireCycle}\nNPC Base Power: {npcPower} (higher=more inaccuracy)\nAccuracy: Regular=min 5.0, Auto-Turret=10x base power\nInBurst: {inBurst}\n{formation}";
        }


        void Transition(params StateGraphEntry[] possible) {
            foreach (var e in possible) {
                if (random.NextSingle() < GetStateValue(currentState, e)) {
                    EnterState(e);
                    break;
                }
            }
        }

        private float evadeX = 0;
        private float evadeY = 0;
        private float evadeZ = 0;
        private Vector3 buzzDirection;
        private bool evadeThrust = false;


        void EnterState(StateGraphEntry e)
        {
            currentState = e;
            timeInState = 0;
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
                buzzDirection = new Vector3(random.NextSingle(),
                    random.NextSingle(), random.NextSingle()).Normalized();
            }
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
                EnterState(StateGraphEntry.Evade);
            }
        }
        public override void Update(double time)
        {
            Parent.TryGetComponent<AutopilotComponent>(out var ap);
            if (ap.CurrentBehavior == AutopilotBehaviors.Undock)
            {
                return; //no npc yet
            }

            damageTimer -= time;
            if (damageTimer < 0)
            {
                damageTimer = 0;
                damageTaken = 0;
            }
            CurrentDirective?.Update(Parent, this, time);

            var shootAt = GetHostileAndFire(time);
            lastShootAt = shootAt;

            var runningDirective = Parent.TryGetComponent<DirectiveRunnerComponent>(out var directiveRunner) &&
                     directiveRunner.Active;

            if (CurrentDirective != null ||
                runningDirective ||
                shootAt == null ||
                ap.CurrentBehavior == AutopilotBehaviors.Formation) {
                currentState = StateGraphEntry.NULL;
                timeInState = 0;
                return;
            }

            Parent.TryGetComponent<ShipSteeringComponent>(out var si);
            timeInState += time;

            bool canTransition = false;

            var mypos = Parent.WorldTransform.Position;

            si.InThrottle = 0;
            si.InPitch = 0;
            si.InYaw = 0;
            si.InRoll = 0;
            si.Cruise = false;
            si.Thrust = false;

            switch (currentState) {
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
                Transition(StateGraphEntry.Face, StateGraphEntry.Trail, StateGraphEntry.Buzz);
        }

        public void DockWith(GameObject tgt)
        {
            SetState(new AiDockState(tgt, GotoKind.Goto));
        }
    }
}
