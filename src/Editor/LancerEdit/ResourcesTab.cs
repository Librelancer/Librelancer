using System;
using ImGuiNET;
using LibreLancer;
namespace LancerEdit
{
	public class ResourcesTab : DockTab
	{
		ResourceManager res;
		bool open = true;
		public ResourcesTab(ResourceManager res)
		{
			this.res = res;
		}

		public override bool Draw()
		{
			if (ImGuiExt.BeginDock("Resources##" + Unique, ref open, 0))
			{
				foreach (var t in res.TextureDictionary)
					ImGui.Text("Texture: " + t.Key);
				foreach (var m in res.MaterialDictionary)
					ImGui.Text(string.Format("Material: {0} (0x{1:X})", m.Value.Name, m.Key));
			}
			ImGuiExt.EndDock();
			return open;
		}
	}
}
