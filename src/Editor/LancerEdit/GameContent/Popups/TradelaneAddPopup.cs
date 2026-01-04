using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LancerEdit.GameContent.Lookups;
using LibreLancer.Data.GameData;
using ArchetypeType = LibreLancer.Data.Schema.Solar.ArchetypeType;
using LibreLancer.ImUI;
using LibreLancer.World;

namespace LancerEdit.GameContent.Popups;

public record struct TradelaneAddCommand(
    Vector3 Start,
    Vector3 End,
    int IdsLeft,
    int IdsRight,
    Faction Reputation,
    int Count,
    Archetype Archetype);

public class TradelaneAddPopup : PopupWindow
{
    public override string Title { get; set; } = "Add Tradelane";

    public override Vector2 InitSize => new Vector2(620, 400) * ImGuiHelper.Scale;

    private Vector3 start;
    private Vector3 end;
    private ArchetypeList archetypes;
    private SystemObjectLookup idsLeft;
    private GameObject leftObject;
    private SystemObjectLookup idsRight;
    private GameObject rightObject;
    private Faction selectedFaction;
    private GameDataContext gd;

    static bool ValidName(GameObject obj) => obj.Name is not null and not TradelaneName;

    private const float TRADELANE_DISTANCE = 8300;

    private int tradelaneCount;
    private int tradelaneMax;

    private Action<TradelaneAddCommand> onAdd;
    private Action onCancel;

    public TradelaneAddPopup(
        Vector3 start,
        Vector3 end,
        GameDataContext dc,
        IReadOnlyList<GameObject> gameObjects,
        Action<TradelaneAddCommand> onAdd,
        Action onCancel)
    {
        this.start = start;
        this.end = end;
        this.gd = dc;
        var allowed = dc.GameData.Items.Archetypes.Where(x => x.Type == ArchetypeType.tradelane_ring);
        archetypes = new(dc, null, allowed.ToArray());

        var orderRight = gameObjects
            .Where(ValidName)
            .OrderBy(x => Vector3.Distance(start, x.LocalTransform.Position)).ToArray();
        idsRight = new SystemObjectLookup(orderRight, dc);
        rightObject = orderRight.Length > 0 ? orderRight[0] : null;

        var orderLeft = gameObjects
            .Where(ValidName)
            .OrderBy(x => Vector3.Distance(end, x.LocalTransform.Position)).ToArray();
        idsLeft = new(orderLeft, dc);
        leftObject = orderLeft.Length > 0 ? orderLeft[0] : null;

        tradelaneCount = 1 + (int)(Math.Floor(((end - start).Length()) / TRADELANE_DISTANCE));
        if (tradelaneCount < 2)
            tradelaneCount = 2;
        tradelaneMax = 3 * tradelaneCount;
        this.onAdd = onAdd;
        this.onCancel = onCancel;
    }

    public override void Draw(bool appearing)
    {
        ImGui.Text($"Start Position: {start}");
        ImGui.Text($"End Position: {end}");
        ImGui.AlignTextToFramePadding();
        ImGui.Text($"Ring Count (2-{tradelaneMax}):");
        ImGui.SameLine();
        ImGui.InputInt("##tradelaneCount", ref tradelaneCount, 1, 1);
        if (tradelaneCount < 2)
            tradelaneCount = 2;
        if (tradelaneCount > tradelaneMax)
            tradelaneCount = tradelaneMax;
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Start Object:");
        ImGui.SameLine();
        idsRight.Draw("##right", ref rightObject);
        ImGui.AlignTextToFramePadding();
        ImGui.Text("End Object:");
        ImGui.SameLine();
        idsLeft.Draw("##left", ref leftObject);
        ImGui.AlignTextToFramePadding();
        ImGui.Text($"Reputation (required for name display):");
        ImGui.SameLine();
        ImGui.PushItemWidth(225 * ImGuiHelper.Scale);
        gd.Factions.Draw("##rep", ref selectedFaction);
        ImGui.PopItemWidth();
        ImGui.Text($"Archetype:");
        archetypes.Draw("##archetype");
        if (ImGuiExt.Button("Ok", archetypes.Selected != null))
        {
            onAdd(new(start, end,
                leftObject?.SystemObject?.IdsName ?? 0,
                rightObject?.SystemObject?.IdsName ?? 0,
                selectedFaction,
                tradelaneCount,
                archetypes.Selected
            ));
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
        {
            onCancel();
            ImGui.CloseCurrentPopup();
        }

    }
}
