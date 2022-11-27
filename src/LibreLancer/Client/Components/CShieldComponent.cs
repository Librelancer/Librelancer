// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.GameData.Items;
using LibreLancer.World;

namespace LibreLancer.Client.Components
{
    public enum ShieldUpdate
    {
        Offline,
        Online,
        Failed,
        Restored
    }
    public class CShieldComponent : GameComponent
    {
        public float ShieldPercent { get; private set; } = 1;

        private ShieldEquipment equipment;
        
        public CShieldComponent(ShieldEquipment equip, GameObject parent) : base(parent)
        {
            this.equipment = equip;
        }

        public void SetShieldPercent(float value, Action<ShieldUpdate> callback = null)
        {
            //Notify important changes
            if (ShieldPercent <= -1 && value > 0) {
                callback?.Invoke(ShieldUpdate.Online);
            }
            else if (ShieldPercent <= 0 && value > 0) {
                callback?.Invoke(ShieldUpdate.Restored);
            } else if (value <= -1 && ShieldPercent > 0) {
                callback?.Invoke(ShieldUpdate.Offline);
            } else if (value <= 0 && ShieldPercent > 0) {
                callback?.Invoke(ShieldUpdate.Failed);
            }
            //Set value
            ShieldPercent = value;
        }
    }
}