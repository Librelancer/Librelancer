using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.AI;
using LibreLancer.Data.Pilots;
using LibreLancer.GameData;

namespace LibreLancer
{
    public class SNPCComponent : GameComponent
    {
        public AiState CurrentState;
        public NetShipLoadout Loadout;
        private NPCManager manager;
        public MissionRuntime MissionRuntime;

        public Action<GameObject, GameObject> ProjectileHitHook;
        public Action OnKilled;

        public List<GameObject> HostileNPCs = new List<GameObject>();
        public Faction Faction;
        
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
            Parent.GetComponent<ShipPhysicsComponent>().Active = false;
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
            FLLog.Debug("NPC", "");
            this.CurrentState = state;
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

        private double fireTimer;
        private double missileTimer;

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
                    HostileNPCs.Contains(other))
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
                        missileTimer = Pilot?.Missile?.LaunchIntervalTime ?? 0;
                    }
                }
                //Fire guns
                if (dist < gunRange)
                {
                    fireTimer -= time;
                    if (fireTimer <= 0)
                    {
                        weapons.FireAll();
                        fireTimer = Pilot?.Gun?.FireIntervalTime ?? 0;
                    }
                }
                else {
                    if (CurrentState == null && Parent.TryGetComponent<AutopilotComponent>(out var ap)) {
                        if (ap.CurrentBehaviour == AutopilotBehaviours.None)
                        {
                            ap.GotoObject(shootAt, false, 1, gunRange * 0.5f);
                        }
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
        
        public override void Update(double time)
        {
            CurrentState?.Update(Parent, this, time);

            var shootAt = GetHostileAndFire(time);
            
            if (CurrentState != null || shootAt == null) {
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