// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
namespace LibreLancer.AI
{
    public class AiDockState : AiState
    {
        private GameObject target;
        public AiDockState(GameObject target)
        {
            this.target = target;
        }
        
        public override void OnStart(GameObject obj, SNPCComponent ai)
        {
            if (obj.TryGetComponent<AutopilotComponent>(out var ap) &&
               target.TryGetComponent<SDockableComponent>(out var dock))
            {
                dock.StartDock(obj, 0);
                ap.StartDock(target);
            }
        }

        public override void Update(GameObject obj, SNPCComponent ai, double time)
        {
        }
    }
}