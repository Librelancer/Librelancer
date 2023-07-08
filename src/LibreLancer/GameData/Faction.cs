using System.Collections.Generic;

namespace LibreLancer.GameData
{
    public class Faction : IdentifiableItem
    {
        public bool Hidden; //Hidden from the player status list
        public int IdsName;
        public int IdsShortName;
        public int IdsInfo;

        public float ObjectDestroyRepChange;
        public float MissionSucceedRepChange;
        public float MissionFailRepChange;
        public float MissionAbortRepChange;

        public Empathy[] FactionEmpathy;

        public Data.Missions.FactionProps Properties;
        public Dictionary<Faction, float> Reputations = new Dictionary<Faction, float>();

        public override string ToString()
        {
            return $"Faction: {Nickname}";
        }
    }
    
}