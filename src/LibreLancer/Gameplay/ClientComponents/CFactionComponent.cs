using LibreLancer.GameData;

namespace LibreLancer
{

    public class CFactionComponent : GameComponent
    {
        public Faction Faction;

        public CFactionComponent(GameObject parent, Faction faction) : base(parent)
        {
            Faction = faction;
        }
    }
}