// MIT License - Copyright (c) Lazrius
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Entities.Character
{
    using LibreLancer.Entities.Abstract;

    public class CargoItem : BaseEntity
    {
        // The nickname of the item. 
        public string ItemName { get; set; }

        // The amount of the item present in the cargo hold
        public long ItemCount { get; set; }
        
        // Hardpoint item is mounted to, null if not mounted
        
        public string Hardpoint { get; set; }
        
        // Health of the item
        
        public float Health { get; set;  }

        // Can the item be dropped
        public bool IsMissionItem { get; set; }
    }
}
