

namespace LibreLancer
{
    public class SPlayerComponent : GameComponent
    {
        public Player Player { get; private set; }
        public SPlayerComponent(Player player, GameObject parent) : base(parent)
        {
            this.Player = player;
        }
    }
}