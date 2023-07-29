using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ImUI;

namespace LancerEdit;

public class HotkeyHelp
{
    private static readonly HotkeyDescription[] modelViewer = new HotkeyDescription[] {
        new(1, 0, 0, "D", "Deselect"),
        new(1, 0, 0, "G", "Toggle Grid"),
        new(1, 0, 0, "R", "Reset Camera"),
    };

    private static readonly HotkeyDescription[] systemEditor = new HotkeyDescription[] {
        new(1, 0, 0, "D", "Deselect"),
        new(1, 0, 0, "G", "Toggle Grid"),
        new(1, 0, 0, "R", "Reset Camera"),
        new(1, 0, 0, "0", "Clear Rotation"),
    };

    private static readonly Dictionary<string, HotkeyDescription[]> tabs = new Dictionary<string, HotkeyDescription[]>()
    {
        { "Model Viewer", modelViewer },
        { "System Editor", systemEditor },
    };
    public void Draw()
    {
        if (!Open) return;
        ImGui.SetNextWindowSize(new Vector2(380,270) * ImGuiHelper.Scale, ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Hotkeys", ref Open))
        {
            if (ImGui.BeginTabBar("##tabs"))
            {
                foreach (var t in tabs) {
                    if (ImGui.BeginTabItem(t.Key))
                    {
                        DrawHotkeyTable(t.Value);
                        ImGui.EndTabItem();
                    }
                }
                ImGui.EndTabBar();
            }
        }    
    }
    
    
    public bool Open = false;
    
    static void DrawKeyboardButton(bool ctrl, bool alt, bool shift, string key)
    {
        ImGui.PushFont(ImGuiHelper.SystemMonospace);
        var plusCol = ImGui.GetStyle().Colors[(int)ImGuiCol.Text];
        ImGui.PushStyleColor(ImGuiCol.Text, new Color4(31, 35, 40, 255));
        ImGui.PushStyleColor(ImGuiCol.Button, new Color4(246, 248, 250, 255));
        ImGui.PushStyleVar(ImGuiStyleVar.DisabledAlpha, 1f);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(2, 1));
        if (ctrl)
        {
            ImGuiExt.Button("Ctrl", false);
            ImGui.SameLine();
            ImGui.TextColored(plusCol,"+");
            ImGui.SameLine();
        }
        if (alt)
        {
            ImGuiExt.Button("Alt", false);
            ImGui.SameLine();
            ImGui.TextColored(plusCol, "+");
            ImGui.SameLine();
        }
        if (shift)
        {
            ImGuiExt.Button("Shift", false);
            ImGui.SameLine();
            ImGui.TextColored(plusCol, "+");
            ImGui.SameLine();
        }
        ImGuiExt.Button(key, false);
        ImGui.PopStyleVar();
        ImGui.PopStyleVar();
        ImGui.PopStyleVar();
        ImGui.PopStyleColor();
        ImGui.PopStyleColor();
        ImGui.PopFont();
    }

    static void DrawHotkeyTable(HotkeyDescription[] hotkeys)
    {
        if (ImGui.BeginTable("##hks", 2, ImGuiTableFlags.ScrollY | ImGuiTableFlags.Borders))
        {
            ImGui.TableSetupColumn("Hotkey");
            ImGui.TableSetupColumn("Action");
            ImGui.TableHeadersRow();
            foreach (var hk in hotkeys)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                DrawKeyboardButton(hk.Control, hk.Alt, hk.Shift, hk.Key);
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(hk.Description);
            }

            ImGui.EndTable();
        }
    }

    struct HotkeyDescription
    {
        public string Key;
        public bool Control;
        public bool Alt;
        public bool Shift;
        public string Description;

        public HotkeyDescription(int ctrl, int alt, int shift, string key, string description)
        {
            Control = ctrl != 0;
            Alt = alt != 0;
            Shift = shift != 0;
            Key = key;
            Description = description;
        }
    }
    
   
    
}