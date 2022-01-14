// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Linq;

namespace LibreLancer
{
    public class CHealthComponent : GameComponent
    {
        public float MaxHealth { get; set; }
        public float CurrentHealth { get; set; }
        
        public float ShieldHealth { get; set; }
        
        public CHealthComponent(GameObject parent) : base(parent) { }
    }
}