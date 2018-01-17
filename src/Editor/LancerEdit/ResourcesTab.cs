using System;
using System.Collections.Generic;
using ImGuiNET;
using LibreLancer;
namespace LancerEdit
{
	public class ResourcesTab : DockTab
	{
		ResourceManager res;
		bool open = true;
		List<MissingReference> missing;
		List<uint> referencedMats;
		List<string> referencedTex;
		public ResourcesTab(ResourceManager res, List<MissingReference> missing, List<uint> referencedMats, List<string> referencedTex)
		{
			this.res = res;
			this.missing = missing;
			this.referencedMats = referencedMats;
			this.referencedTex = referencedTex;
		}

		public override bool Draw()
		{
			if (ImGuiExt.BeginDock("Resources###" + Unique, ref open, WindowFlags.HorizontalScrollbar))
			{
				if (res.TextureDictionary.Count + res.MaterialDictionary.Count > 0)
				{
					ImGui.Text("Loaded:");
					var tcolor = (Vector4)ImGui.GetStyle().GetColor(ColorTarget.Text);
					foreach (var t in res.TextureDictionary)
					{
						var col = new Vector4(0.6f, 0.6f, 0.6f, 1f);
						foreach (var tex in referencedTex)
						{
							if (t.Key.Equals(tex, StringComparison.InvariantCultureIgnoreCase))
							{
								col = tcolor;
								break;
							}
						}
						ImGui.Text("Texture: " + t.Key, col);
					}

					foreach (var m in res.MaterialDictionary)
					{
						var col = referencedMats.Contains(m.Key) ? tcolor : new Vector4(0.6f, 0.6f, 0.6f, 1f);
						ImGui.Text(string.Format("Material: {0} (0x{1:X})", m.Value.Name, m.Key), col);
					}
				}
				else
					ImGui.Text("Loaded: None");
				if (missing.Count > 0)
				{
					ImGui.Separator();
					ImGui.Text("Missing:");
					foreach (var ln in missing)
						ImGui.Text(string.Format("{0} (Ref {1})", ln.Missing, ln.Reference), new Vector4(1, 0, 0, 1));
				}
			}
			ImGuiExt.EndDock();
			return open;
		}
	}
}
