// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
namespace LibreLancer
{
	public class ServerCharacter
    {
		public int ID;
		public long Credits;
		public string Name;
		public string Base;
        public string Ship;
        public List<ServerEquipment> Equipment = new List<ServerEquipment>();
	}
    public class ServerEquipment
    {
        public string Hardpoint;
        public string Equipment;
        public float Health;
    }
}
