// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Data.GameData.Items
{
	public class EngineEquipment : Equipment
    {
        public Data.Schema.Equipment.Engine Def;
        public float CruiseAccelTime = 5;
        public float CruiseSpeed = 300;
    }
}
