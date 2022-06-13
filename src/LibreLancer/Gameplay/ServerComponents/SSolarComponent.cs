using System;
using System.Linq;

namespace LibreLancer
{
    public class SSolarComponent : GameComponent
    {
        public bool SendSolarUpdate = false;
        private int stopUpdateTimer = 0;
        private const int UPDATE_TIMEOUT_TICKS = 8 * 60;
        
        public SSolarComponent(GameObject parent) : base(parent)
        {
        }

        public override void Update(double time)
        {
            var shield = Parent.GetChildComponents<SShieldComponent>().FirstOrDefault();
            if (shield != null)
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