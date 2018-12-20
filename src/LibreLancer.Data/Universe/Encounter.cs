// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;

namespace LibreLancer.Data.Universe
{
	public class Encounter
	{
		public string EncounterType { get; set; }
		public int Attr2 { get; set; }
		public float Attr3 { get; set; }
		public Dictionary<string, float> Factions { get; set; }

		public Encounter(string attr1, int attr2, float attr3)
		{
			EncounterType = attr1; 
			Attr2 = attr2; 
			Attr3 = attr3;

			Factions = new Dictionary<string, float>();
		}
	}
}