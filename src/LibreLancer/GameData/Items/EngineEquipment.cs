// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Client.Components;
using LibreLancer.Server.Components;
using LibreLancer.Sounds;
using LibreLancer.World;

namespace LibreLancer.GameData.Items
{
	public class EngineEquipment : Equipment
    {
        static EngineEquipment() => EquipmentObjectManager.RegisterType<EngineEquipment>(AddEquipment);
        static GameObject AddEquipment(GameObject parent, ResourceManager res, SoundManager snd, EquipmentType type, string hardpoint, Equipment equip)
        {
            var eng = (EngineEquipment) equip;
            if(type != EquipmentType.Server)
                parent.AddComponent(new CEngineComponent(parent, eng));
            else
            {
                parent.AddComponent(new SEngineComponent(parent) {Engine = eng});
            }

            if (snd != null)
            {
                snd.LoadSound(eng.Def.CruiseLoopSound);
                snd.LoadSound(eng.Def.CruiseStartSound);
                snd.LoadSound(eng.Def.CruiseStopSound);
                snd.LoadSound(eng.Def.CruiseBackfireSound);
                snd.LoadSound(eng.Def.CruiseStopSound);
                snd.LoadSound(eng.Def.EngineKillSound);
                snd.LoadSound(eng.Def.RumbleSound);
                snd.LoadSound(eng.Def.CharacterLoopSound);
                snd.LoadSound(eng.Def.CharacterStartSound);
            }
            return null;
        }
        public Data.Equipment.Engine Def;
        public float CruiseAccelTime = 5;
        public float CruiseSpeed = 300;
    }
}
