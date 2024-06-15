using System;
using System.Numerics;
using LibreLancer.Server.Components;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer.Server.Ai.ObjList
{
    //TODO: cruise over whole spline
    public class AiGotoSplineState : AiObjListState
    {
        private int index = 0;
        public Vector3[] Points;
        public AiGotoKind Cruise;
        public float MaxThrottle;
        public float Range;
        
        public AiGotoSplineState(Vector3[] points, AiGotoKind cruise, float maxThrottle, float range)
        {
            Points = points;
            Cruise = cruise;
            MaxThrottle = maxThrottle;
            Range = range;
        }

        public override void OnStart(GameObject obj, SNPCComponent ai)
        {
            index = 0;
            if (obj.TryGetComponent<AutopilotComponent>(out var ap))
            {
                ap.GotoVec(Eval(0), Cruise != AiGotoKind.GotoNoCruise, MaxThrottle);
            }
        }

        Vector3 Eval(float t)
        {
            var val = CatmullRom(Points[0], Points[1], Points[2], Points[3], t);
            FLLog.Debug("GotoSpline", $"heading to point t={t} - {val}");
            return val;
        }

        static float GetT(float t, float alpha, Vector3 p0, Vector3 p1)
        {
            var d = p1 - p0;
            var a = Vector3.Dot(d, d);
            float b = MathF.Pow(a, alpha * 0.5f);
            return (b + t);
        }

        static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t, float alpha = 0.5f)
        {
            float t0 = 0.0f;
            float t1 = GetT( t0, alpha, p0, p1 );
            float t2 = GetT( t1, alpha, p1, p2 );
            float t3 = GetT( t2, alpha, p2, p3 );
            t = MathHelper.Lerp(t1, t2, t);
            var A1 = ( t1-t )/( t1-t0 )*p0 + ( t-t0 )/( t1-t0 )*p1;
            var A2 = ( t2-t )/( t2-t1 )*p1 + ( t-t1 )/( t2-t1 )*p2;
            var A3 = ( t3-t )/( t3-t2 )*p2 + ( t-t2 )/( t3-t2 )*p3;
            var B1 = ( t2-t )/( t2-t0 )*A1 + ( t-t0 )/( t2-t0 )*A2;
            var B2 = ( t3-t )/( t3-t1 )*A2 + ( t-t1 )/( t3-t1 )*A3;
            var C  = ( t2-t )/( t2-t1 )*B1 + ( t-t1 )/( t2-t1 )*B2;
            return C;
        }

        private static float[] times = {0, 0.3333f, 0.6667f, 1f};
        public override void Update(GameObject obj, SNPCComponent ai, double dt)
        {
            if (obj.TryGetComponent<AutopilotComponent>(out var ap))
            {
                if (ap.CurrentBehavior == AutopilotBehaviors.None)
                {
                    if (index + 1 < 4)
                    {
                        index++;
                        //ap.GotoVec(Points[index], Cruise, MaxThrottle, Range);
                        ap.GotoVec(Eval(times[index]), Cruise != AiGotoKind.GotoNoCruise, MaxThrottle);
                    }
                    else
                        ai.SetState(Next);
                }
            }
            else
            {
                ai.SetState(Next);
            }
        }
    }
}