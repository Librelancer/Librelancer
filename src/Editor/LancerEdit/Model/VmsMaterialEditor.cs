using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.Data;
using LibreLancer.ImUI;
using LibreLancer.Resources;
using LibreLancer.Utf.Vms;

namespace LancerEdit;

public class VmsMaterialEditor : PopupWindow
{
    class EditMat
    {
        public string MaterialName;
        public uint MaterialCRC;
        private bool isCrcEdit = false;

        public EditMat(TMeshHeader tms, ResourceManager resources)
        {
            MaterialCRC = tms.MaterialCrc;
            MaterialName = resources.FindMaterial(MaterialCRC)?.Name;
            isCrcEdit = string.IsNullOrEmpty(MaterialName);
        }

        public unsafe void Draw(string id, ResourceManager resources)
        {
            ImGui.PushID(id);
            ImGuiExt.ButtonDivided("lred", "CRC", "Name", ref isCrcEdit);
            ImGui.SameLine();
            ImGui.PushItemWidth(250);
            if (isCrcEdit)
            {
                var val = MaterialCRC;
                ImGui.InputScalar("##crc", ImGuiDataType.U32, (IntPtr) (& val), 0, 0);
                if (val != MaterialCRC) {
                    MaterialCRC = val;
                    MaterialName = resources.FindMaterial(MaterialCRC)?.Name;
                }
            }
            else
            {
                var val = MaterialName ?? "";
                ImGui.InputText("##name", ref val, 250);
                if (val != MaterialName && !(val == "" && MaterialName == null))
                {
                    MaterialName = val;
                    MaterialCRC = CrcTool.FLModelCrc(MaterialName);
                }
            }
            ImGui.PopItemWidth();
            ImGui.SameLine();
            if(ImGui.Button(".."))
                ImGui.OpenPopup("mats");
            if (ImGui.BeginPopup("mats"))
            {
                foreach (var x in resources.MaterialDictionary) {
                    if (ImGui.MenuItem(x.Value.Name))
                    {
                        MaterialCRC = x.Key;
                        MaterialName = x.Value.Name;
                    }
                }
                ImGui.EndPopup();
            }
            ImGui.PopID();
        }
    }

    private List<EditMat> materials = new();

    private ResourceManager res;
    private UtfTab parent;
    private LUtfNode node;
    public VmsMaterialEditor(LUtfNode node, ResourceManager resources, UtfTab tab)
    {
        parent = tab;
        res = resources;
        this.node = node;
        var vms = new VMeshData(node.Data, node.Parent?.Name ?? "vms");
        foreach (var t in vms.Meshes) {
            materials.Add(new EditMat(t, resources));
        }
    }

    public override string Title { get; set; } = "Materials";
    public override void Draw(bool appearing)
    {
        int idx = 0;
        foreach (var e in materials) {
            e.Draw((idx++).ToString(), res);
        }
        if (ImGui.Button("Apply"))
        {
            for (int i = 0; i < materials.Count; i++) {
                //16b header + 12 byte tmeshheader stride
                var offset = 16 + (12 * i);
                var x = node.Data.AsSpan().Slice(offset);
                MemoryMarshal.Cast<byte, uint>(x)[0] = materials[i].MaterialCRC;
            }
            parent.ReloadResources();
            ImGui.CloseCurrentPopup();
        }
    }
}
