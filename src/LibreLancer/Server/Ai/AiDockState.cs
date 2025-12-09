// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Server.Components;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer.Server.Ai
{
    public class AiDockState : AiState
    {
        private GameObject target;
        public GotoKind GotoKind;
        public AiDockState(GameObject target, GotoKind gotoKind)
        {
            this.target = target;
            this.GotoKind = gotoKind;
        }

        public override void OnStart(GameObject obj, SNPCComponent ai)
        {
            if (obj.TryGetComponent<AutopilotComponent>(out var ap) &&
               target.TryGetComponent<SDockableComponent>(out var dock))
            {
                dock.StartDock(obj, 0);
                ap.StartDock(target, GotoKind);
            }
        }

        public override void Update(GameObject obj, SNPCComponent ai, double time)
        {
        }
    }
}
