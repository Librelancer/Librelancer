// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;

using LibreLancer.Ini;

namespace LibreLancer.Data.Universe
{
	public class LootableZone
	{
		/*
[LootableZone]
asteroid_loot_container = lootcrate_ast_loot_metal
asteroid_loot_commodity = commodity_scrap_metal
dynamic_loot_container = lootcrate_ast_loot_metal
dynamic_loot_commodity = commodity_scrap_metal
asteroid_loot_count = 0, 0
dynamic_loot_count = 1, 1
asteroid_loot_difficulty = 40
dynamic_loot_difficulty = 4
         */

		public LootableZone(Section section)
		{
			/*if (section == null) throw new ArgumentNullException("section");

            foreach (Entry e in section)
            {
                switch (e.Name.ToLowerInvariant())
                {
                    //TODO
                }
            }*/
		}
	}
}