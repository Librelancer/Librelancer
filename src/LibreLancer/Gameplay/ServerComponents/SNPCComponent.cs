using System;
using System.Numerics;
using LibreLancer.AI;

namespace LibreLancer
{
    public class SNPCComponent : GameComponent
    {
        public AiState CurrentState;
        public NetShipLoadout Loadout;
        private NPCManager manager;

        public SNPCComponent(GameObject parent, NPCManager manager) : base(parent)
        {
            this.manager = manager;
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

        public override void FixedUpdate(double time)
        {
            CurrentState?.Update(Parent, this, time);
        }

        public void DockWith(GameObject tgt)
        {
            SetState(new AiDockState(tgt));
        }
    }
}