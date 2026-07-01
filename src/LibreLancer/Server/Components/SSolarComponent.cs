using System.Linq;
using LibreLancer.World;

namespace LibreLancer.Server.Components
{
    public class SSolarComponent : SRepComponent
    {
        public bool SendSolarUpdate = false;
        public bool SendPartsUpdate = false;
        private int stopUpdateTimer = 0;
        private int stopPartsUpdateTimer = 0;
        private const int UPDATE_TIMEOUT_TICKS = 8 * 60;

        public SSolarComponent(GameObject parent) : base(parent)
        {
        }

        public override void Update(double time, GameWorld world)
        {
            if (SendPartsUpdate || SendSolarUpdate)
            {
                if (Parent.TryGetComponent<SHealthComponent>(out var health))
                {
                    if (health.EquipmentHealths.Count == 0)
                    {
                        stopPartsUpdateTimer--;
                        if (stopPartsUpdateTimer <= 0)
                            SendPartsUpdate = false;
                    }
                    else
                    {
                        stopPartsUpdateTimer = UPDATE_TIMEOUT_TICKS;
                    }

                }
                if (Parent.TryGetFirstChildComponent<SShieldComponent>(out var shield))
                {
                    if (shield.Health < shield.Equip.Def.MaxCapacity) {
                        SendSolarUpdate = true;
                        stopUpdateTimer = UPDATE_TIMEOUT_TICKS;
                    }
                    else {
                        if (stopUpdateTimer > 0)
                            stopUpdateTimer--;
                        SendSolarUpdate = stopUpdateTimer > 0;
                    }
                }
            }
        }
    }
}
