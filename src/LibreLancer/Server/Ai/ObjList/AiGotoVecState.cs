// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;
using LibreLancer.Server.Components;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer.Server.Ai.ObjList
{

    public enum AiGotoKind
    {
        Goto,
        GotoCruise,
        GotoNoCruise
    }
    public class AiGotoVecState : AiObjListState
    {
        public Vector3 Target;
        public AiGotoKind Cruise;
        public float MaxThrottle;
        public float Range;

        public AiGotoVecState(Vector3 target, AiGotoKind cruise, float maxThrottle, float range)
        {
            Target = target;
            Cruise = cruise;
            MaxThrottle = maxThrottle;
            Range = range;
        }

        public override void OnStart(GameObject obj, SNPCComponent ai)
        {
            if (obj.TryGetComponent<AutopilotComponent>(out var ap))
            {
                ap.GotoVec(Target, Cruise != AiGotoKind.GotoNoCruise, MaxThrottle, Range);
                if (Cruise == AiGotoKind.GotoCruise)
                    obj.GetComponent<ShipSteeringComponent>().Cruise = true;
            }
        }

        public override void Update(GameObject obj, SNPCComponent ai, double time)
        {
            if (obj.TryGetComponent<AutopilotComponent>(out var ap))
            {
                if (ap.CurrentBehavior == AutopilotBehaviors.None)
                    ai.SetState(Next);
            }
            else
            {
                ai.SetState(Next);
            }
        }
    }
}
