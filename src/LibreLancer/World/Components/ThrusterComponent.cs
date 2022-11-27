// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.World.Components
{
    public class ThrusterComponent : GameComponent
    {
        public GameData.Items.ThrusterEquipment Equip;
        public bool Enabled;
        
        public ThrusterComponent(GameObject parent, GameData.Items.ThrusterEquipment equip) : base(parent)
        {
            Equip = equip;
        }
    }
}