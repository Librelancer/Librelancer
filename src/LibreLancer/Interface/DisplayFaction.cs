using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [WattleScriptUserData]
    public class DisplayFaction
    {
        public int IdsName;
        public float Relationship;

        public DisplayFaction()
        {
        }

        public DisplayFaction(int idsName, float relationship)
        {
            IdsName = idsName;
            Relationship = relationship;
        }
    }
}