// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Data.GameData.Items;
using LibreLancer.World;

namespace LibreLancer.Server.Components
{
    public class SEngineComponent(GameObject parent, EngineEquipment engine) : GameComponent(parent)
    {
        public EngineEquipment Engine = engine;
        public float Speed;
        public bool EngineKill;
    }
}
