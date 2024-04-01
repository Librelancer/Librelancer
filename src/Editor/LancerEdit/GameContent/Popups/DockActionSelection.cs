using System;
using System.Linq;
using ImGuiNET;
using LibreLancer.Data.Solar;
using LibreLancer.GameData;
using LibreLancer.GameData.World;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent.Popups;

public class DockActionSelection : PopupWindow
{
    public override string Title { get; set; } = "Dock";
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize;

    private Action<DockAction> onSelect;
    
    private string[] baseNames;
    private Base[] bases;
    private int selectedBase = -1;

    private string[] systemNames;
    private StarSystem[] systems;
    private int selectedSystem = -1;

    private StarSystem openSystemNames;
    private string[] objectNames;
    private int selectedObject = -1;
    private string currentTunnel = "";

    

    private string[] kindNames = new string[]
    {
        "None",
    };

    private int selectedKind;

    private string[] currentObjects;
    private int leftObject = -1;
    private int rightObject = -1;

    private int baseKind;
    private int jumpKind;
    private int tradelaneKind;
    
    
    public DockActionSelection(Action<DockAction> onSelect, DockAction initial, Archetype a, string[] currentObjects, GameDataContext gd)
    {
        this.onSelect = onSelect;
        bases = gd.GameData.Bases.OrderBy(x => x.Nickname).ToArray();
        baseNames = bases.Select(x => $"{x.Nickname} ({gd.GameData.GetString(x.IdsName)})").ToArray();
        
        systems = gd.GameData.Systems.OrderBy(x => x.Nickname).ToArray();
        systemNames = systems.Select(x => $"{x.Nickname} ({gd.GameData.GetString(x.IdsName)})").ToArray();

        this.currentObjects = currentObjects;

        switch (a?.Type ?? ArchetypeType.NONE)
        {
            case ArchetypeType.jump_gate:
            case ArchetypeType.jump_hole:
            case ArchetypeType.jumphole:
                kindNames = new[] {"None", "Jump"};
                jumpKind = 1;
                break;
            case ArchetypeType.docking_ring:
            case ArchetypeType.station:
                kindNames = new[] {"None", "Base"};
                baseKind = 1;
                break;
            case ArchetypeType.tradelane_ring:
                kindNames = new[] {"None", "Tradelane"};
                tradelaneKind = 1;
                break;
        }
        if (initial != null)
        {
            switch (initial.Kind)
            {
                case DockKinds.Base:
                    selectedKind = baseKind;
                    var b = gd.GameData.Bases.Get(initial.Target);
                    if (b != null)
                        selectedBase = Array.IndexOf(bases, b);
                    break;
                case DockKinds.Jump:
                    selectedKind = jumpKind;
                    var sys = gd.GameData.Systems.Get(initial.Target);
                    if (sys != null) {
                        selectedSystem = Array.IndexOf(systems, sys);
                        LoadObjectNames();
                        if(objectNames != null)
                            selectedObject = Array.IndexOf(objectNames, initial.Exit);
                        currentTunnel = initial.Tunnel ?? "";
                    }
                    break;
                case DockKinds.Tradelane:
                    selectedKind = tradelaneKind;
                    leftObject = Array.IndexOf(currentObjects, initial.TargetLeft);
                    rightObject = Array.IndexOf(currentObjects, initial.Target);
                    break;
            }
        }
    }

    void LoadObjectNames()
    {
        selectedObject = -1;
        if (selectedSystem < 0 || selectedSystem >= systems.Length) {
            objectNames = null;
            return;
        }
        objectNames = systems[selectedSystem].Objects
            .Select(x => x.Nickname)
            .OrderBy(x => x)
            .ToArray();
    }

    void Combo(string label, ref int selectedIndex, string[] names)
    {
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(label);
        ImGui.PushItemWidth(200 * ImGuiHelper.Scale);
        ImGui.Combo($"##{label}", ref selectedIndex, names, names.Length);
        ImGui.PopItemWidth();
    }
    
    
    public override void Draw()
    {
        Combo("Kind: ", ref selectedKind, kindNames);
        if (selectedKind == baseKind)
        {
            Combo("Base: ", ref selectedBase, baseNames);
        }
        else if (selectedKind == jumpKind)
        {
            Combo("System: ", ref selectedSystem, systemNames);
            if (selectedSystem != -1 && systems[selectedSystem] != openSystemNames) {
                LoadObjectNames();
                openSystemNames = systems[selectedSystem];
            }
            if(objectNames != null)
                Combo("Exit: ", ref selectedObject, objectNames);
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Tunnel: ");
            ImGui.SameLine();
            ImGui.InputText("##tunnel", ref currentTunnel, 1000);
        }
        else if (selectedKind == tradelaneKind)
        {
            Combo("Left: ", ref leftObject, currentObjects);
            Combo("Right: ", ref rightObject, currentObjects);
        }
        if (ImGui.Button("Ok"))
        {
            if (selectedKind == 0)
                onSelect(null);
            else if (selectedKind == baseKind)
            {
                onSelect(new DockAction()
                {
                    Kind = DockKinds.Base,
                    Target = selectedBase >= 0 ? bases[selectedBase].Nickname : null
                });
            }
            else if (selectedKind == jumpKind)
            {
                onSelect(new DockAction()
                {
                    Kind = DockKinds.Jump,
                    Target = selectedSystem >= 0 ? systems[selectedSystem].Nickname : null,
                    Exit = selectedObject >= 0 ? objectNames[selectedObject] : null,
                    Tunnel = string.IsNullOrWhiteSpace(currentTunnel) ? null : currentTunnel,
                });
            }
            else if (selectedKind == tradelaneKind)
            {
                onSelect(new DockAction()
                {
                    Kind = DockKinds.Tradelane,
                    Target = leftObject >= 0 ? currentObjects[leftObject] : null,
                    Exit =  rightObject >= 0 ? currentObjects[rightObject] : null,
                });
            }
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if(ImGui.Button("Cancel"))
            ImGui.CloseCurrentPopup();
    }
}