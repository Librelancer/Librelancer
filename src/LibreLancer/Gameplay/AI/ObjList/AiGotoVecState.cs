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
        public float MaxThrottle;

        public AiGotoVecState(Vector3 target, bool cruise, float maxThrottle)
        {
            Target = target;
            Cruise = cruise;
            MaxThrottle = maxThrottle;
        }
        
        public override void OnStart(GameObject obj, SNPCComponent ai)
        {
            if (obj.TryGetComponent<AutopilotComponent>(out var ap))
            {
                ap.GotoVec(Target, Cruise, MaxThrottle);
            }
        }

        public override void Update(GameObject obj, SNPCComponent ai, double time)
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