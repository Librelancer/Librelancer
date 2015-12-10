/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * The Original Code is Starchart code (http://flapi.sourceforge.net/).
 * 
 * The Initial Developer of the Original Code is Malte Rupprecht (mailto:rupprema@googlemail.com).
 * Portions created by the Initial Developer are Copyright (C) 2011
 * the Initial Developer. All Rights Reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;

using LibreLancer.Ini;

namespace LibreLancer.GameData.Universe
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