using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.AI;
using LibreLancer.Data.Pilots;

namespace LibreLancer
{
    public class SNPCComponent : GameComponent
    {
        public AiState CurrentState;
        public NetShipLoadout Loadout;
        private NPCManager manager;

        public Action<GameObject, GameObject> ProjectileHitHook;
        public Action OnKilled;

        public List<GameObject> HostileNPCs = new List<GameObject>();

        private GameData.Pilot Pilot;
        
        public void OnProjectileHit(GameObject attacker)
        {
            ProjectileHitHook?.Invoke(Parent, attacker);
        }

        public SNPCComponent(GameObject parent, NPCManager manager) : base(parent)
        {
            this.manager = manager;
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
            this.CurrentState = state;
            state?.OnStart(Parent, this);
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
            if (attackPref.TryGetValue(obj.Kind, out var weight))
                return weight;
            return 0;
        }

        private double fireTimer;
        
        public override void Update(double time)
        {
            CurrentState?.Update(Parent, this, time);
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
            //Fly towards hostile if needed
            if (CurrentState == null && shootAt != null){
                var dist = Vector3.Distance(shootAt.WorldTransform.Translation, myPos);
                if (dist > 150 && Parent.TryGetComponent<AutopilotComponent>(out var ap))
                {
                    ap.GotoObject(shootAt, false, 1, 150);
                }
            }
            //Shoot at hostile
            if (shootAt != null && Parent.TryGetComponent<WeaponControlComponent>(out var weapons))
            {
                var dist = Vector3.Distance(shootAt.WorldTransform.Translation, myPos);
                var range = weapons.GetMaxRange() * 0.95f;
                if (dist < range)
                {
                    fireTimer -= time;
                    weapons.AimPoint = Vector3.Transform(Vector3.Zero, shootAt.WorldTransform);
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
                            ap.GotoObject(shootAt, false, 1, range * 0.5f);
                        }
                    }
                }
            }
            else
            {
                fireTimer = Pilot?.Gun?.FireIntervalTime ?? 0;
            }
        }

        public void DockWith(GameObject tgt)
        {
            SetState(new AiDockState(tgt));
        }
    }
}