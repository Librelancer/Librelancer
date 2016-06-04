using System;
using LibreLancer.Ini;
namespace LibreLancer.Compatibility.GameData.Universe
{
	public class TexturePanelsRef
	{
		public string File { get; private set; }

		public TexturePanelsRef (Section section, FreelancerData gameData)
		{
			foreach (Entry e in section)
			{
				switch (e.Name.ToLowerInvariant())
				{
				case "file":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (File != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					File = gameData.Freelancer.DataPath + e[0].ToString();
					break;
				default: throw new Exception("Invalid Entry in " + section.Name + ": " + e.Name);
				}
			}
		}
	}
}

