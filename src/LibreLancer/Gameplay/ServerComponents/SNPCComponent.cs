using System;

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