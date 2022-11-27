// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Render;
using LibreLancer.Sounds;
using LibreLancer.World;

namespace LibreLancer.GameData.Items
{
	public class LightEquipment : Equipment
	{
		public Color3f Color;
		public Color3f MinColor;
		public Color3f GlowColor;
		public float BulbSize;
		public float GlowSize;
		public bool Animated;
		public float AvgDelay;
		public float BlinkDuration;
        public bool AlwaysOn;
        public bool DockingLight;
        public float EmitRange;
        public Vector3 EmitAttenuation;
        
        static LightEquipment() => EquipmentObjectManager.RegisterType<LightEquipment>(AddEquipment);

        private static GameObject AddEquipment(GameObject parent, ResourceManager res, SoundManager snd, EquipmentType type, string hardpoint, Equipment equip)
        {
            var lq = (LightEquipment)equip;
            var obj = new GameObject();
            if (type != EquipmentType.Server && 
                type != EquipmentType.Cutscene)
            {
                obj.RenderComponent = new LightEquipRenderer(lq) {LightOn = !lq.DockingLight};
            }
            return obj;
        }
    }
}

