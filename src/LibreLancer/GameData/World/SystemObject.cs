// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using System.Numerics;
using System.Text;
using LibreLancer.Data;
using LibreLancer.GameData.Items;

namespace LibreLancer.GameData.World
{
	public class SystemObject
	{
		public string Nickname;
        public int IdsName;
        public int[] IdsInfo;
        public string Base; //used for linking IdsInfo
		public Archetype Archetype;
		public Vector3 Position = Vector3.Zero;
        public Vector3 Spin = Vector3.Zero;
		public Matrix4x4? Rotation;
        public ObjectLoadout Loadout;
		public DockAction Dock;
        public Faction Reputation;
        public int Visit;

        public int TradelaneSpaceName;
        public int IdsLeft;
        public int IdsRight;
        
        //Properties not yet used in game, but copied from ini for round trip support
        public Pilot Pilot;
        public Faction Faction;
        public int DifficultyLevel;
        public string Behavior;
        public float AtmosphereRange;
        public string MsgIdPrefix;
        public Color4? BurnColor;
        public Color4? AmbientColor;
        
        public SystemObject ()
		{
		}

        public string Serialize()
        {
            var sb = new StringBuilder();
            sb.AppendSection("Object")
                .AppendEntry("nickname", Nickname)
                .AppendEntry("ids_name", IdsName, false);
            if (Position != Vector3.Zero)
                sb.AppendEntry("pos", Position);
            if (Rotation != null)
            {
                var rot = Rotation.Value.GetEulerDegrees();
                var ln = rot.Length();
                if(!float.IsNaN(ln) && ln > 0)
                    sb.AppendEntry("rotate", new Vector3(rot.Y, rot.X, rot.Z));
            }
            if (AmbientColor != null)
                sb.AppendEntry("ambient_color", AmbientColor.Value);
            sb.AppendEntry("Archetype", Archetype?.Nickname);
            sb.AppendEntry("msg_id_prefix", MsgIdPrefix);
            foreach (var i in IdsInfo)
                sb.AppendEntry("ids_info", i);
            if (Spin != Vector3.Zero)
                sb.AppendEntry("spin", Spin);
            sb.AppendEntry("atmosphere_range", AtmosphereRange, false);
            if (BurnColor != null)
                sb.AppendEntry("burn_color", BurnColor.Value);
            sb.AppendEntry("base", Base);
            if (Dock != null)
            {
                if (Dock.Kind == DockKinds.Base)
                {
                    sb.AppendEntry("dock_with", Dock.Target);
                }
                else if (Dock.Kind == DockKinds.Jump)
                {
                    sb.Append("goto = ")
                        .Append(Dock.Target)
                        .Append(", ")
                        .Append(Dock.Exit)
                        .Append(", ")
                        .AppendLine(Dock.Tunnel);
                }
                else if (Dock.Kind == DockKinds.Tradelane)
                {
                    sb.AppendEntry("prev_ring", Dock.TargetLeft);
                    sb.AppendEntry("next_ring", Dock.Target);
                }
            }
            
            sb.AppendEntry("behavior", Behavior);
            sb.AppendEntry("faction", Faction?.Nickname);
            sb.AppendEntry("difficulty_level", DifficultyLevel);
            sb.AppendEntry("loadout", Loadout?.Nickname);
            sb.AppendEntry("pilot", Pilot?.Nickname);
            sb.AppendEntry("reputation", Reputation?.Nickname);
            sb.AppendEntry("tradelane_space_name", TradelaneSpaceName, false);
            sb.AppendEntry("visit", Visit);
            return sb.ToString();
        }
        
	}
}

