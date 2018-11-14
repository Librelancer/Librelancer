// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using ImGuiNET;
using LibreLancer;
namespace LancerEdit
{
    public class ResourcesTab : EditorTab
    {
        ResourceManager res;
        List<MissingReference> missing;
        List<uint> referencedMats;
        List<string> referencedTex;
        public ResourcesTab(ResourceManager res, List<MissingReference> missing, List<uint> referencedMats, List<string> referencedTex)
        {
            this.res = res;
            this.missing = missing;
            this.referencedMats = referencedMats;
            this.referencedTex = referencedTex;
            Title = "Resources";
        }

        public override void Draw()
        {
            ImGui.Columns(2, "cols", true);
            ImGui.Text("Type");
            ImGui.NextColumn();
            ImGui.Text("Reference");
            ImGui.Separator();
            ImGui.NextColumn();
            var tcolor = (Vector4)ImGui.GetStyle().Colors[(int)ImGuiCol.Text];
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
                ImGui.TextColored(col, "Texture");
                ImGui.NextColumn();
                ImGui.TextColored(col, t.Key);
                ImGui.NextColumn();
            }

            foreach (var m in res.MaterialDictionary)
            {
                var col = referencedMats.Contains(m.Key) ? tcolor : new Vector4(0.6f, 0.6f, 0.6f, 1f);
                ImGui.TextColored(col, "Material");
                ImGui.NextColumn();
                ImGui.TextColored(col, string.Format("{0} (0x{1:X})", m.Value.Name, m.Key));
                ImGui.NextColumn();
            }
            foreach (var ln in missing)
            {
                ImGui.TextColored(new Vector4(1, 0, 0, 1), "Missing");
                ImGui.NextColumn();
                ImGui.TextColored(new Vector4(1, 0, 0, 1), string.Format("{0} (Ref {1})", ln.Missing, ln.Reference));
                ImGui.NextColumn();
            }
            ImGui.Columns(1, null, false);
        }
    }
}
