// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Data.GameData.Items;
using LibreLancer.World;
using LibreLancer.World.Components;

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
        public float ShieldPercent => Health / Equip.Def.MaxCapacity;

        public float Health { get; private set; }

        public ShieldEquipment Equip;

        private float MinHealth => Equip.Def.OfflineThreshold * Equip.Def.MaxCapacity;


        public CShieldComponent(ShieldEquipment equip, GameObject parent) : base(parent)
        {
            this.Equip = equip;
            this.Health = equip.Def.MaxCapacity;
        }

        public void SetShieldHealth(float value, Action<ShieldUpdate> callback = null)
        {
            //Notify important changes
            if (Health <= -1 && value > 0) {
                callback?.Invoke(ShieldUpdate.Online);
            }
            else if (value <= MinHealth && value > 0) {
                callback?.Invoke(ShieldUpdate.Restored);
            } else if (value <= -1 && Health > 0) {
                callback?.Invoke(ShieldUpdate.Offline);
            } else if (value <= 0 && Health > 0) {
                callback?.Invoke(ShieldUpdate.Failed);
            }
            //Set value
            Health = value;
        }

        private bool shieldHpActive = false;

        public override void Update(double time)
        {
            if (Health >= MinHealth && !shieldHpActive)
            {
                if (Parent.Parent.TryGetComponent<ShipComponent>(out var ship)) {
                    ship.ActivateShieldBubble(Parent.Attachment.Name);
                }
                shieldHpActive = true;
            }
            else if (Health < MinHealth && shieldHpActive)
            {
                if (Parent.Parent.TryGetComponent<ShipComponent>(out var ship)) {
                    ship.DeactivateShieldBubble(Parent.Attachment.Name);
                }
                shieldHpActive = false;
            }
        }
    }
}
