using System;
using System.Collections.Generic;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ImUI;

namespace LancerEdit;

public class NewHardpointPopup : PopupWindow
{
    public override string Title { get; set; } = "New Hardpoint";
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize;

    private string name = "";
    private bool newIsFixed;
    private Func<IEnumerable<string>> getExistingNames;
    private Action<string> onAdd;

    public NewHardpointPopup(bool newIsFixed, Func<IEnumerable<string>> getExistingNames, Action<string> onAdd)
    {
        this.newIsFixed = newIsFixed;
        this.getExistingNames = getExistingNames;
        this.onAdd = onAdd;
    }

    private const string NameEmpty = "Name must not be empty.";
    private const string NameExists = "Name is already in use.";

    private string error = NameEmpty;
    private bool valid = false;
    private string checkedName = "";
    bool CheckValid()
    {
        if (name == checkedName)
        {
            return valid;
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            error = NameEmpty;
            valid = false;
        }
        else
        {
            valid = true;
            foreach (var n in getExistingNames())
            {
                if (name.Equals(n, StringComparison.OrdinalIgnoreCase))
                {
                    valid = false;
                    error = NameExists;
                    break;
                }
            }
        }
        checkedName = name;
        return valid;
    }

    public override void Draw(bool appearing)
    {
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Name: ");
        ImGui.SameLine();
        if (appearing)
        {
            ImGui.SetKeyboardFocusHere();
        }
        ImGui.PushItemWidth(240);
        bool entered = ImGui.InputText("##nickname", ref name, 250, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGui.PopItemWidth();
        ImGui.SameLine();
        if (ImGui.Button(".."))
        {
            ImGui.OpenPopup("names");
        }
        if (ImGui.BeginPopup("names"))
        {
            var infos = newIsFixed ? HardpointInformation.Fix : HardpointInformation.Rev;
            foreach (var item in infos)
            {
                if (Theme.IconMenuItem(item.Icon, item.Name, true))
                {
                    switch (item.Autoname)
                    {
                        case HpNaming.None:
                            name = item.Name;
                            break;
                        case HpNaming.Number:
                            name = item.Name + HardpointInformation.GetHpNumbering(item.Name, getExistingNames()).ToString("00");
                            break;
                        case HpNaming.Letter:
                            name = item.Name + HardpointInformation.GetHpLettering(item.Name, getExistingNames());
                            break;
                    }
                }
            }
            ImGui.EndPopup();
        }
        ImGui.Text("Type: " + (newIsFixed ? "Fixed" : "Revolute"));
        bool valid = CheckValid();
        if (!valid)
        {
            ImGui.TextColored(Color4.Red, error);
        }
        if (ImGuiExt.Button("Ok", valid) || (valid && entered))
        {
            onAdd(name);
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if(ImGui.Button("Cancel"))
            ImGui.CloseCurrentPopup();
    }
}
