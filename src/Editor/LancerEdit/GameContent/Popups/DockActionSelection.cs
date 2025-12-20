using System;
using System.Linq;
using ImGuiNET;
using LancerEdit.GameContent.Lookups;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.Solar;
using LibreLancer.Data.Schema.Solar;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent.Popups;

public class DockActionSelection : PopupWindow
{
    public override string Title { get; set; } = "Dock";
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize;

    private Action<DockAction> onSelect;

    private BaseLookup baseLookup;
    private SystemLookup systemLookup;

    private StarSystem openSystemNames;
    private string[] objectNames;
    private int selectedObject = -1;
    private string currentTunnel = "";

    private string[] kindNames = ["None"];

    private int selectedKind;

    private string[] currentObjects;
    private int leftObject = -1;
    private int rightObject = -1;

    private int baseKind = -1;
    private int jumpKind = -1;
    private int tradelaneKind = -1;


    public DockActionSelection(Action<DockAction> onSelect, DockAction initial, Archetype a, string[] currentObjects, GameDataContext gd)
    {
        this.onSelect = onSelect;
        Base initialBase = null;
        StarSystem initialSystem = null;

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
                    initialBase = gd.GameData.Items.Bases.Get(initial.Target);
                    break;
                case DockKinds.Jump:
                    selectedKind = jumpKind;
                    initialSystem = gd.GameData.Items.Systems.Get(initial.Target);
                    if (initialSystem != null) {

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
        baseLookup = new BaseLookup("##dockbase", gd, initialBase);
        systemLookup = new SystemLookup("##jumpsys", gd, initialSystem);
        if (initialSystem != null) {
            LoadObjectNames();
            if(objectNames != null)
                selectedObject = Array.IndexOf(objectNames, initial.Exit);
        }
    }

    void LoadObjectNames()
    {
        selectedObject = -1;
        if (systemLookup.Selected == null) {
            objectNames = null;
            openSystemNames = null;
            return;
        }
        objectNames = systemLookup.Selected.Objects
            .Select(x => x.Nickname)
            .OrderBy(x => x)
            .ToArray();
        openSystemNames = systemLookup.Selected;
    }

    void Combo(string label, ref int selectedIndex, string[] names)
    {
        ImGui.Text(label);
        ImGui.SetNextItemWidth(300 * ImGuiHelper.Scale);
        ImGui.Combo($"##{label}", ref selectedIndex, names, names.Length);
    }

    void Combo<T>(string label, ObjectLookup<T> lookup) where T : class
    {
        ImGui.Text(label);
        ImGui.SetNextItemWidth(300 * ImGuiHelper.Scale);
        lookup.Draw();
    }


    public override void Draw(bool appearing)
    {
        Combo("Kind: ", ref selectedKind, kindNames);
        if (selectedKind == baseKind)
        {
            Combo("Base: ", baseLookup);
        }
        else if (selectedKind == jumpKind)
        {
            Combo("System: ", systemLookup);
            if (systemLookup.Selected != openSystemNames) {
                LoadObjectNames();
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
                    Target = baseLookup.Selected?.Nickname
                });
            }
            else if (selectedKind == jumpKind)
            {
                onSelect(new DockAction()
                {
                    Kind = DockKinds.Jump,
                    Target = systemLookup.Selected?.Nickname,
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
