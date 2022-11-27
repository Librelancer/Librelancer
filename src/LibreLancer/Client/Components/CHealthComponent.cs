// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.World;

namespace LibreLancer.Client.Components
{
    public class CHealthComponent : GameComponent
    {
        public float MaxHealth { get; set; }
        public float CurrentHealth { get; set; }
        
        public CHealthComponent(GameObject parent) : base(parent) { }
    }
}