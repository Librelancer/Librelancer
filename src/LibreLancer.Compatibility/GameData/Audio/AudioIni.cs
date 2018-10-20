// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Ini;
namespace LibreLancer.Compatibility.GameData.Audio
{
	public class AudioIni : IniFile
	{
		public List<AudioEntry> Entries = new List<AudioEntry>();
		public void AddIni(string path, FreelancerIni ini)
		{
			foreach (Section s in ParseFile(path))
			{
				var au = new AudioEntry();
				if (s.Name.ToLowerInvariant() != "sound")
					throw new Exception("Invalid section " + s.Name + " in " + path);
				foreach (Entry e in s)
				{
					switch (e.Name.ToLowerInvariant())
					{
						case "nickname":
							if (au.Nickname != null)
								FLLog.Warning("Audio", "Invalid nickname entry in " + path + " (prev " + au.Nickname + ")");
							else
								au.Nickname = e[0].ToString();
							break;
						case "file":
							if (au.File != null)
								FLLog.Warning("Audio", "Invalid file entry in " + path+  " (nick: " + (au.Nickname ?? "null") + ")");
							else
								au.File = e[0].ToString();
							break;
						case "crv_pitch":
							au.CrvPitch = e[0].ToInt32();
							break;
						case "attenuation":
							au.Attenuation = e[0].ToInt32();
							break;
						case "is_2d":
							au.Is2d = e[0].ToBoolean();
							break;
					}
				}
				Entries.Add(au);
			}
		}
		}
	}


