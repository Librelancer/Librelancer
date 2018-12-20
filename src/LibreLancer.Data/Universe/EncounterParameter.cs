// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LibreLancer.Ini;

namespace LibreLancer.Data.Universe
{
	public class EncounterParameter
	{
		/*
[EncounterParameters]
nickname = tradelane_armored_prisoner
filename = missions\encounters\tradelane_armored_prisoner.ini
         */

		public EncounterParameter(Section section)
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