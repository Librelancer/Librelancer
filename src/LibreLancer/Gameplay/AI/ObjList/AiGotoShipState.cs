// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System.Numerics;
using System.Runtime.Remoting;

namespace LibreLancer.AI.ObjList
{
    
    public class AiGotoShipState : AiObjListState
    {
        public string Target;
        public AiGotoKind Cruise;
        public float MaxThrottle;
        public float Range;

        
        public AiGotoShipState(string target, AiGotoKind cruise, float maxThrottle, float range)
        {
            Target = target;
            Cruise = cruise;
            MaxThrottle = maxThrottle;
            Range = range;
        }
        
        public override void OnStart(GameObject obj, SNPCComponent ai)
        {
            var tgtObject = obj.World.GetObject(Target);
            if (obj.TryGetComponent<AutopilotComponent>(out var ap))
            {
                ap.GotoObject(tgtObject, Cruise != AiGotoKind.GotoNoCruise, MaxThrottle, Range);
                if (Cruise == AiGotoKind.GotoCruise)
                    obj.GetComponent<ShipSteeringComponent>().Cruise = true;
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