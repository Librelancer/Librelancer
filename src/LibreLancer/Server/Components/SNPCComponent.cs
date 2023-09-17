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
            manager.Despawn(Parent);
        }
        public void Docked()
        {
            manager.Despawn(Parent);
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

        private GameObject lastShootAt;

        GameObject GetHostileAndFire(double time)
        {
            //Get hostile
            GameObject shootAt = null;
            int shootAtWeight = -1000;
            var myPos = Parent.WorldTransform.Translation;
            foreach (var other in Parent.GetWorld().SpatialLookup
                         .GetNearbyObjects(Parent, myPos, 5000))
            {
                if (Vector3.Distance(other.WorldTransform.Translation, myPos) < 5000 &&
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

                var dist = Vector3.Distance(shootAt.WorldTransform.Translation, myPos);

                var gunRange = weapons.GetMaxRange() * 0.95f;
                weapons.AimPoint = Vector3.Transform(Vector3.Zero, shootAt.WorldTransform);

                var missileRange = Pilot?.Missile?.LaunchRange ?? gunRange;
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

        void Transition(params StateGraphEntry[] possible) {
            foreach (var e in possible) {
                if (random.NextSingle() < GetStateValue(currentState, e)) {
                    currentState = e;
                    timeInState = 0;
                    break;
                }
            }
        }

        private double timeInState = 0;

        public string GetDebugInfo()
        {
            string ls = lastShootAt == null ? "none" :
                lastShootAt.Nickname ?? "no nickname";
            var maxRange = 0f;
            if(Parent.TryGetComponent<WeaponControlComponent>(out var wp))
                 maxRange = wp.GetMaxRange() * 0.95f;
            return $"Shooting At: {ls}\nDirective: {CurrentDirective?.ToString() ?? "null"}\nState: {currentState}\nMax Range: {maxRange}";
        }

        public override void Update(double time)
        {
            CurrentDirective?.Update(Parent, this, time);

            var shootAt = GetHostileAndFire(time);
            lastShootAt = shootAt;

            if (CurrentDirective != null || shootAt == null) {
                currentState = StateGraphEntry.NULL;
                timeInState = 0;
                return;
            }

            Parent.TryGetComponent<AutopilotComponent>(out var ap);
            Parent.TryGetComponent<ShipPhysicsComponent>(out var si);
            timeInState += time;

            switch (currentState) {
                case StateGraphEntry.NULL:
                    si.EnginePower = 0;
                    si.CruiseEnabled = false;
                    si.Steering = Vector3.Zero;
                    ap.Cancel();
                    break;
                case StateGraphEntry.Buzz:
                case StateGraphEntry.Face:
                case StateGraphEntry.Trail:
                    ap.GotoObject(shootAt, false, 1, 150);
                    break;
            }
            Transition(StateGraphEntry.Face, StateGraphEntry.Trail, StateGraphEntry.Buzz);
        }

        public void DockWith(GameObject tgt)
        {
            SetState(new AiDockState(tgt));
        }
    }
}
