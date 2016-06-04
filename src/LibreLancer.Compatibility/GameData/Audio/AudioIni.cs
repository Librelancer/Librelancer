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
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
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
								throw new Exception("Invalid nickname entry in " + path);
							au.Nickname = e[0].ToString();
							break;
						case "file":
							if (au.File != null)
								throw new Exception("Invalid file entry in " + path);
							au.File = VFS.GetPath(ini.DataPath + e[0].ToString());
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


