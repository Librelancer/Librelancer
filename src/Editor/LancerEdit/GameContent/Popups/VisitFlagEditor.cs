using System;
using System.Text;
using ImGuiNET;
using LibreLancer.Data.GameData.World;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent.Popups;

public class VisitFlagEditor : PopupWindow
{
    public override string Title { get; set; } = "Visit";

    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize;

    private VisitFlags flags;
    private Action<VisitFlags> onSelect;

    public VisitFlagEditor(VisitFlags flags, Action<VisitFlags> onSelect)
    {
        this.flags = flags;
        this.onSelect = onSelect;
    }

    void Flag(char icon, VisitFlags f)
    {
        if (Controls.Flag($"{icon} {f}", flags, f, out var set))
        {
            if (set) flags |= f;
            else flags &= ~f;
        }
    }

    public static string FlagsString(VisitFlags f)
    {
        if (f == 0) return "(none)";
        var b = new StringBuilder();
        if ((f & VisitFlags.Visited) == VisitFlags.Visited) b.Append(Icons.Check);
        if ((f & VisitFlags.Wreck) == VisitFlags.Wreck) b.Append(Icons.Gift);
        if ((f & VisitFlags.Hidden) == VisitFlags.Hidden) b.Append(Icons.EyeSlash);
        b.Append(" (").Append(f.ToString()).Append(")");
        return b.ToString();
    }

    public override void Draw(bool appearing)
    {
        Flag(Icons.Check, VisitFlags.Visited);
        Flag(Icons.Gift, VisitFlags.Wreck);
        Flag(Icons.EyeSlash, VisitFlags.Hidden);
        if (ImGui.Button("Ok"))
        {
            onSelect(flags);
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
            ImGui.CloseCurrentPopup();
    }
}
