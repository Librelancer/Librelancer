using LibreLancer.Data.GameData;
using LibreLancer.World;

namespace LibreLancer.Client.Components
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
