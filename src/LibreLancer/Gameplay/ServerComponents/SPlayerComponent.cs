

namespace LibreLancer
{
    public class SPlayerComponent : GameComponent
    {
        public float Pitch;
        public float Yaw;
        public float Roll;
        public float EnginePower;
        
        public Player Player { get; private set; }
        public SPlayerComponent(Player player, GameObject parent) : base(parent)
        {
            this.Player = player;
        }

        public void Killed()
        {
            Player.Killed();
        }

        public override void Update(double time)
        {
            if (Parent.TryGetComponent<ShipPhysicsComponent>(out var phys))
            {
                phys.PlayerPitch = Pitch;
                phys.PlayerYaw = Yaw;
                phys.Roll = Roll;
                phys.EnginePower = EnginePower;
            }
        }
    }
}