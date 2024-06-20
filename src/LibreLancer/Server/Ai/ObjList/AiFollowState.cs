using System.Numerics;
using LibreLancer.Server.Components;
using LibreLancer.World;

namespace LibreLancer.Server.Ai.ObjList
{
    public class AiFollowState : AiObjListState
    {
        public string Target;
        public Vector3 Offset;

        public AiFollowState(string target, Vector3 offset)
        {
            Target = target;
            Offset = offset;
        }

        public override void OnStart(GameObject obj, SNPCComponent ai)
        {
            var tgtObject = obj.World.GetObject(Target);
            ai.EnterFormation(tgtObject, Offset);
        }

        public override void Update(GameObject obj, SNPCComponent ai, double dt) { }
    }
}
