using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.Pilots;
using LibreLancer.GameData;
using LibreLancer.Missions;
using LibreLancer.Net.Protocol;
using LibreLancer.Server.Ai;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer.Server.Components
{
    public class SNPCComponent : SRepComponent
    {
        public Bodypart CommHead;
        public Bodypart CommBody;
        public Accessory CommHelmet;

        public AiState CurrentDirective;
        public NetShipLoadout Loadout;
        private NPCManager manager;
        public MissionRuntime MissionRuntime;

        public Action<GameObject, GameObject> ProjectileHitHook;
        public Action OnKilled;


        private GameData.Pilot Pilot;
        private StateGraph _stateGraph;

        private Random random = new Random();

        public float GetStateValue(StateGraphEntry row, StateGraphEntry column, float defaultVal = 0.0f)
        {
            if (_stateGraph == null) return defaultVal;
            if ((int) row >= _stateGraph.Data.Count) return defaultVal;
            var tableRow = _stateGraph.Data[(int) row];
            if ((int) column >= tableRow.Length) return defaultVal;
            return tableRow[(int) column];
        }

        public void OnProjectileHit(GameObject attacker)
        {
            ProjectileHitHook?.Invoke(Parent, attacker);
        }

        public SNPCComponent(GameObject parent, NPCManager manager, StateGraph stateGraph) : base(parent)
        {
            this.manager = manager;
            _stateGraph = stateGraph;
        }

        public void StartTradelane()
        {
            ShipPhysicsComponent component = Parent.GetComponent<ShipPhysicsComponent>();
            if (component is not null)
                component.Active = false;
        }


        public void Killed()
        {
            OnKilled?.Invoke();
            manager.Despawn(Parent, true);
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

        public void EnterFormation(GameObject tgt, Vector3 offset)
        {
            if (tgt.Formation == null)
            {
                tgt.Formation = new ShipFormation(tgt, Parent);
            }
            else
            {
                if(!tgt.Formation.Contains(Parent))
                    tgt.Formation.Add(Parent);
            }
            Parent.Formation = tgt.Formation;
            if(offset != Vector3.Zero)
                tgt.Formation.SetShipOffset(Parent, offset);
            if (Parent.TryGetComponent<AutopilotComponent>(out var ap))
            {
                ap.StartFormation();
            }
        }

        private Dictionary<GameObjectKind, int> attackPref = new Dictionary<GameObjectKind, int>();
        public void SetPilot(GameData.Pilot pilot)
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
            if (obj.Nickname.Equals("player", StringComparison.OrdinalIgnoreCase) &&
                manager.AttackingPlayer > 2)
                return -100;
            if (attackPref.TryGetValue(obj.Kind, out var weight))
                return weight;
            return 0;
        }

        private double missileTimer;

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
        bool RunFireTimers(float dt)
        {
            bool retVal = false;
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
                    fireTimer -= dt;
                    if (fireTimer <= 0)
                    {
                        fireTimer = ValueWithVariance(Pilot?.Gun?.FireIntervalTime,
                            Pilot?.Gun?.FireIntervalVariancePercent);
                        retVal = true;
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
                }
            }
            return retVal;
        }

        Vector3 AddInaccuracy(Vector3 target, Vector3 local)
        {
            //This is not correct, but it's a small amount of inaccuracy at least
            if(Pilot?.Gun == null)
                return target;
            var coneAngle = Pilot.Gun.FireAccuracyConeAngle / 2;

            var offset = new Vector3(
                random.NextFloat(-coneAngle, coneAngle),
                random.NextFloat(-coneAngle, coneAngle),
                  random.NextFloat(-coneAngle, coneAngle)
            );
            return target + offset;
        }

        private GameObject lastShootAt;

        Vector3 GetAimPosition(GameObject other, WeaponControlComponent weapons)
        {
            if (other.PhysicsComponent == null)
                return other.WorldTransform.Position;
            var myPos = Parent.PhysicsComponent.Body.Position;
            var myVelocity = Parent.PhysicsComponent.Body.LinearVelocity;
            var otherPos = other.PhysicsComponent.Body.Position;
            var otherVelocity = other.PhysicsComponent.Body.LinearVelocity;
            var avgSpeed = weapons.GetAverageGunSpeed();
            if (Aiming.GetTargetLeading((otherPos - myPos), (otherVelocity - myVelocity), avgSpeed, out var t))
            {
                return AddInaccuracy(otherPos + otherVelocity * t, myPos);
            }
            return AddInaccuracy(otherPos, myPos);
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
                if (shootAt.Nickname.Equals("player", StringComparison.OrdinalIgnoreCase))
                    manager.AttackingPlayer++;

                var dist = Vector3.Distance(shootAt.WorldTransform.Position, myPos);

                var gunRange = weapons.GetGunMaxRange() * 0.95f;
                weapons.AimPoint = GetAimPosition(shootAt, weapons);

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
                    if (RunFireTimers((float)time))
                        weapons.FireGuns();
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

            AutopilotBehaviors beh = AutopilotBehaviors.None;
            if (Parent.TryGetComponent<AutopilotComponent>(out var ap))
            {
                beh = ap.CurrentBehavior;
            }
            return $"Autopilot: {beh}\nShooting At: {ls}\nDirective: {CurrentDirective?.ToString() ?? "null"}\nState: {currentState}\nMax Range: {maxRange}\nPhys Active: {physActive}\n{formation}";
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
            damageTimer -= time;
            if (damageTimer < 0)
            {
                damageTimer = 0;
                damageTaken = 0;
            }
            CurrentDirective?.Update(Parent, this, time);

            var shootAt = GetHostileAndFire(time);
            lastShootAt = shootAt;

            Parent.TryGetComponent<AutopilotComponent>(out var ap);

            if (CurrentDirective != null ||
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
                    ap.GotoVec(dest, false, 1, 0);
                    canTransition = timeInState >= (Pilot?.BuzzPassBy?.PassByTime ?? 5) ||
                                    Vector3.DistanceSquared(dest, mypos) < 16;
                    break;
                }
                case StateGraphEntry.Face:
                case StateGraphEntry.Trail:
                    ap.GotoObject(shootAt, false, 1, Pilot?.Trail?.Distance ?? 150);
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
            SetState(new AiDockState(tgt));
        }
    }
}
