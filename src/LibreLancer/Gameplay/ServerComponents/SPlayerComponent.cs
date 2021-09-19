// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

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