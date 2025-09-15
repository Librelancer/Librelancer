using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.ContentEdit.Model;
using LibreLancer.ImUI;
using LibreLancer.Utf;
using LibreLancer.Utf.Anm;
using LibreLancer.Utf.Cmp;

namespace LancerEdit;

partial class ModelViewer
{
    enum AnimEditAction { None, Play, Backwards, Rename, Delete }
    bool AnimationHeader(string name, float playTime, out AnimEditAction act)
    {
        // Draw play percent
        if (playTime > 0f)
        {
            var cpos = ImGui.GetCursorScreenPos();
            var fH = ImGui.GetFrameHeight();
            var width = ImGui.GetContentRegionAvail().X;
            var dList = ImGui.GetWindowDrawList();
            dList.AddRectFilled(cpos, cpos + new Vector2(width * playTime, fH), (VertexDiffuse)Color4.DarkOrange,
                ImGui.GetStyle().FrameRounding, ImDrawFlags.RoundCornersAll);
        }
        // Do header
        act = AnimEditAction.None;
        ImGui.PushStyleColor(ImGuiCol.Header, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0,0));
        ImGui.PushID(name);
        var isOpen = ImGui.CollapsingHeader("##Header", ImGuiTreeNodeFlags.AllowOverlap);
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            ImGui.OpenPopup("animcontext");
        ImGui.SameLine();
        if (ImGui.Button($"{Icons.Play}")) act = AnimEditAction.Play;
        ImGui.SameLine();
        if (ImGui.Button($"{Icons.Backward}")) act = AnimEditAction.Backwards;
        ImGui.PopStyleVar();
        ImGui.SameLine();
        ImGui.Text(name);
        if (ImGui.BeginPopupContextItem("animcontext"))
        {
            if (Theme.IconMenuItem(Icons.Edit, "Rename", true)) act = AnimEditAction.Rename;
            if (Theme.IconMenuItem(Icons.TrashAlt, "Delete", true)) act = AnimEditAction.Delete;
            ImGui.Separator();
            if (Theme.IconMenuItem(Icons.Copy, "Copy Nickname", true)) _window.SetClipboardText(name);
            ImGui.EndPopup();
        }
        ImGui.PopID();
        ImGui.PopStyleColor();
        return isOpen;
    }

    IEnumerable<AbstractConstruct> IterateConstructs()
    {
        foreach (var p in vmsModel.AllParts)
        {
            if (p.Construct is { } ac)
                yield return ac;
        }
    }

    void AnimationPanel()
    {
        var anm = ((CmpFile)drawable).Animation;
        if (ImGui.Button("Reset")) animator.ResetAnimations();
        ImGui.SameLine();
        if (ImGui.Button("New"))
        {
            var c = NameInputConfig.Nickname("New Animation", anm.Scripts.ContainsKey);
            c.IsId = false;
            popups.OpenPopup(new NameInputPopup(c, "", newName =>
            {
                anm.Scripts[newName] = new Script(newName);
                OnDirtyAnm();
            }));
        }

        ImGui.SameLine();
        if (ImGuiExt.Button("Apply", _isDirtyAnm))
        {
            SaveAnimations();
            _isDirtyAnm = false;
            parent.DirtyCountAnm--;
        }
        ImGui.Separator();
        int uniqueid = 0;
        foreach (var sc in anm.Scripts)
        {
            ImGui.PushID(uniqueid++);
            if (AnimationHeader(sc.Key, animator.GetPlayPosition(sc.Value),out var act))
            {
                ImGui.BeginGroup();
                if (ImGui.Button("Add Joint Map"))
                {
                    popups.OpenPopup(new AddJointMap(IterateConstructs(), sc.Value, con =>
                    {
                        int type = 0x1;
                        if (con is SphereConstruct) type = 0x4;
                        if (con is LooseConstruct) type = 0x2 | 0x4;
                        var jm = new JointMap();
                        jm.ParentName = con.ParentName;
                        jm.ChildName = con.ChildName;
                        jm.Channel = new(type, 0, -1, new EditableAnmBuffer(2048));
                        sc.Value.JointMaps.Add(jm);
                    }));
                }
                for (int i = 0; i < sc.Value.JointMaps.Count; i++)
                {
                    ImGui.PushID(i);
                    ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0,0));
                    if (ImGui.Button($"{Icons.Edit}"))
                    {
                        popups.OpenPopup(new JointMapEditor(sc.Value.JointMaps, i, sc.Value.Name));
                        OnDirtyAnm();
                    }
                    ImGui.SameLine();
                    if (ImGui.Button($"{Icons.TrashAlt}"))
                    {
                        int index = i;
                        _window.Confirm($"Are you sure you want to delete {sc.Value.JointMaps[i].ChildName}?", () =>
                        {
                            sc.Value.JointMaps.RemoveAt(index);
                            OnDirtyAnm();
                        });
                    }
                    ImGui.SameLine();
                    ImGui.PopStyleVar();
                    ImGui.Text(sc.Value.JointMaps[i].ChildName);
                    ImGui.PopID();
                }
                ImGui.EndGroup();
            }

            switch (act)
            {
                case AnimEditAction.Play:
                    animator.StartAnimation(sc.Value, false);
                    break;
                case AnimEditAction.Backwards:
                    animator.StartAnimation(sc.Value, false, 0, 1, 0, true);
                    break;
                case AnimEditAction.Delete:
                    _window.Confirm($"Are you sure you want to delete {sc.Key}?", () =>
                    {
                        anm.Scripts.Remove(sc.Key);
                        OnDirtyAnm();
                    });
                    break;
            }
            ImGui.PopID();
        }
    }


    void SaveAnimations()
    {
        var destNode = hprefs.RootNode.Children.FirstOrDefault(x =>
            x.Name.Equals("animation", StringComparison.OrdinalIgnoreCase));
        var anm = ((CmpFile)drawable).Animation;
        if (anm == null || anm.Scripts.Count == 0)
        {
            if (destNode != null)
            {
                hprefs.RootNode.Children.Remove(destNode);
            }
            return;
        }
        if (destNode == null)
        {
            destNode = new LUtfNode() { Name = "Animation", Parent = hprefs.RootNode };
            hprefs.RootNode.Children.Add(destNode);
        }
        AnimationWriter.WriteAnimations(destNode, anm);
    }
}
