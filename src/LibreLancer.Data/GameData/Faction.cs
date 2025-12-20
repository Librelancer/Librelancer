using System.Collections.Generic;

namespace LibreLancer.Data.GameData
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

        public Schema.Missions.FactionProps Properties;
        public Dictionary<Faction, float> Reputations = new Dictionary<Faction, float>();

        public const float FriendlyThreshold = 0.6f;
        public const float HostileThreshold = -0.6f;

        public override string ToString()
        {
            return $"Faction: {Nickname}";
        }

        public float GetReputation(Faction f)
        {
            if (Reputations.TryGetValue(f, out var r))
                return r;
            return 0.0f;
        }
    }

}
