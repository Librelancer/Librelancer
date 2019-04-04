// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using System.Collections.Generic;
using LibreLancer.Ini;
namespace LibreLancer.Data.Missions
{
    public class MissionShip
    {
        [Entry("nickname")]
        public string Nickname;
        [Entry("system")]
        public string System;
        [Entry("npc")]
        public string NPC;
        [Entry("label", Multiline = true)]
        public List<string> Labels = new List<string>(); //Multiple labels?
        [Entry("position")]
        public Vector3 Position;
        [Entry("rel_pos")]
        public string[] RelPos;
        [Entry("orientation")]
        public Quaternion Orientation;
        [Entry("random_name")]
        public bool RandomName;
        [Entry("radius")]
        public float Radius;
        [Entry("jumper")]
        public bool Jumper;
        [Entry("arrival_obj")]
        public string ArrivalObj;
        [Entry("init_objectives")]
        public string InitObjectives;
        public List<MissionShipCargo> Cargo = new List<MissionShipCargo>();

        bool HandleEntry(Entry e)
        {
            if(e.Name.Equals("cargo",StringComparison.OrdinalIgnoreCase))
            {
                Cargo.Add(new MissionShipCargo()
                {
                    Cargo = e[0].ToString(), Count = e[1].ToInt32()
                });
                return true;
            }
            return false;
        }
    }
    public class MissionShipCargo
    {
        public string Cargo;
        public int Count;
    }
}
