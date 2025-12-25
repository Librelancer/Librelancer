using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [WattleScriptUserData]
    public class DisplayFaction
    {
        public int IdsName;
        public int IdsInfo;
        public float Relationship;

        public DisplayFaction()
        {
        }

        public DisplayFaction(int idsName, int idsInfo, float relationship)
        {
            IdsName = idsName;
            IdsInfo = idsInfo;
            Relationship = relationship;
        }
    }
}