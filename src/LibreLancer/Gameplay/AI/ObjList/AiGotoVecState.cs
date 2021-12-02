// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System.Numerics;

namespace LibreLancer.AI.ObjList
{
    public class AiGotoVecState : AiObjListState
    {
        public Vector3 Target;
        public bool Cruise;

        public AiGotoVecState(Vector3 target, bool cruise = false)
        {
            Target = target;
            Cruise = cruise;
        }
        
        public override void OnStart(GameObject obj, SNPCComponent ai)
        {
            if (obj.TryGetComponent<AutopilotComponent>(out var ap))
            {
                ap.GotoVec(Target, Cruise);
            }
        }

        public override void Update(GameObject obj, SNPCComponent ai)
        {
            if (obj.TryGetComponent<AutopilotComponent>(out var ap))
            {
                if (ap.CurrentBehaviour == AutopilotBehaviours.None) ;
                    ai.SetState(Next);
            }
            else
            {
                ai.SetState(Next);
            }
        }
    }
}