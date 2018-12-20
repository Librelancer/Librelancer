// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;

using LibreLancer.Ini;

namespace LibreLancer.Data.Universe
{
	public class DynamicAsteroids
	{
		/*
         [DynamicAsteroids]
asteroid = dasteroid_debris_small1
count = 10
placement_radius = 150
placement_offset = 90
max_velocity = 10
max_angular_velocity = 3
color_shift = 1, 1, 1
         */

		public DynamicAsteroids(Section section)
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