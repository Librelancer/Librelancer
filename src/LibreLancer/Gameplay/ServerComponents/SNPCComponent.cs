using System;
using System.Numerics;

namespace LibreLancer
{
    public class SNPCComponent : GameComponent
    {
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
            attack = tgt;
        }

        public override void FixedUpdate(double time)
        {
            if (attack != null) {
                if (Parent.TryGetComponent<WeaponControlComponent>(out var weapons))
                {
                    weapons.AimPoint = Vector3.Transform(Vector3.Zero, attack.WorldTransform);
                    weapons.FireAll();
                }
            }
        }

        public void DockWith(GameObject tgt)
        {
            if (Parent.TryGetComponent<AutopilotComponent>(out var ap) &&
                tgt.TryGetComponent<SDockableComponent>(out var dock))
            {
                ap.TargetObject = tgt;
                ap.CurrentBehaviour = AutopilotBehaviours.Dock;
                dock.StartDock(Parent, 0);
                ap.StartDock();
            }
        }
    }
}