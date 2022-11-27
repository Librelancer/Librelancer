using LibreLancer.Server.Components;
using LibreLancer.World;

namespace LibreLancer.Server.Ai.ObjList
{
    public class AiDelayState : AiObjListState
    {
        public double Duration;
        private double t = 0;
        public AiDelayState(double duration)
        {
            Duration = duration;
        }
        public override void OnStart(GameObject obj, SNPCComponent ai)
        {
            t = 0;
        }

        public override void Update(GameObject obj, SNPCComponent ai, double time)
        {
            t += time;
            if (t >= Duration) {
                ai.SetState(Next);
            }
        }
    }
}