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
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
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
                ImGui.Text("Texture", col);
                ImGui.NextColumn();
                ImGui.Text(t.Key, col);
                ImGui.NextColumn();
            }

            foreach (var m in res.MaterialDictionary)
            {
                var col = referencedMats.Contains(m.Key) ? tcolor : new Vector4(0.6f, 0.6f, 0.6f, 1f);
                ImGui.Text("Material", col);
                ImGui.NextColumn();
                ImGui.Text(string.Format("{0} (0x{1:X})", m.Value.Name, m.Key), col);
                ImGui.NextColumn();
            }
            foreach (var ln in missing)
            {
                ImGui.Text("Missing", new Vector4(1, 0, 0, 1));
                ImGui.NextColumn();
                ImGui.Text(string.Format("{0} (Ref {1})", ln.Missing, ln.Reference), new Vector4(1, 0, 0, 1));
                ImGui.NextColumn();
            }
            ImGui.Columns(1, null, false);
        }
    }
}
