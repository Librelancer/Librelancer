// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Characters
{
	public class BodypartsIni
	{
        public List<string> Animations { get; private set; }
        public List<Bodypart> Bodyparts { get; private set; }
		public List<Accessory> Accessories { get; private set; }

		public BodypartsIni(string path, FreelancerData gdata, IniStringPool stringPool = null)
        {
            Animations = new List<string>();
			Bodyparts = new List<Bodypart>();
			Accessories = new List<Accessory>();

            string currentSkeletonSex = "";
            using var stream = gdata.VFS.Open(path);
			foreach (Section s in IniFile.ParseFile(path, stream, true, false, stringPool))
			{
				switch (s.Name.ToLowerInvariant())
				{
				case "animations":
					// TODO: Bodyparts Animations
                    foreach (var e in s)
                    {
                        Animations.Add(e[0].ToString());
                    }
					break;
				case "detailswitchtable":
					// TODO: Bodyparts DetailSwitchTable
					break;
				case "petalanimations":
					// TODO: Bodyparts PetalAnimations
					break;
				case "skeleton":
                    foreach (var e in s)
                    {
                        if (e.Name.Equals("sex", StringComparison.OrdinalIgnoreCase))
                        {
                            currentSkeletonSex = e[0].ToString();
                        }
                    }
					// TODO: Bodyparts Skeleton
					break;
				case "body":
				case "head":
				case "righthand":
				case "lefthand":
                    if (Bodypart.TryParse(s, out var bp))
                    {
                        bp.Sex = currentSkeletonSex;
                        Bodyparts.Add(bp);
                    }
					break;
				case "accessory":
                    if (Accessory.TryParse(s, out var a))
                    {
                        Accessories.Add(a);
                    }
					break;
				default: throw new Exception("Invalid Section in " + path + ": " + s.Name);
				}
			}
		}

		public Bodypart FindBodypart(string nickname)
		{
			IEnumerable<Bodypart> candidates = from Bodypart b in Bodyparts where b.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase) select b;
			int count = candidates.Count<Bodypart>();
			if (count == 1) return candidates.First<Bodypart>();
			else if (count == 0) return null;
			else throw new Exception(count + " Bodyparts with nickname " + nickname);
		}

		public Accessory FindAccessory(string nickname)
		{
			IEnumerable<Accessory> candidates = from Accessory a in Accessories where a.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase) select a;
			int count = candidates.Count<Accessory>();
			if (count == 1) return candidates.First<Accessory>();
			else if (count == 0) return null;
			else throw new Exception(count + " Accessories with nickname " + nickname);
		}
	}
}
