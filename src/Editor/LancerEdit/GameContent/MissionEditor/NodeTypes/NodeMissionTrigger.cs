using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LancerEdit.GameContent.MissionEditor.Popups;
using LibreLancer;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes;

public class NodeMissionTrigger : Node
{
    public readonly MissionTrigger Data;
    public List<NodeTriggerEntry> Conditions = new();
    public List<NodeTriggerEntry> Actions = new();

    private NodeSuspendState suspend = new();
    private MissionScriptEditorTab tab;

    public NodeMissionTrigger(MissionTrigger data, MissionScriptEditorTab tab) : base(NodeColours.Trigger)
    {
        this.Data = data ?? new MissionTrigger();

        Inputs.Add(new NodePin(this, LinkType.Trigger, PinKind.Input));
        Outputs.Add(new NodePin(this, LinkType.Trigger, PinKind.Output));

        foreach (var c in this.Data.Conditions)
        {
            Conditions.Add(NodeTriggerEntry.ConditionToNode(c.Type, c.Entry));
        }

        foreach (var a in this.Data.Actions)
        {
            Actions.Add(NodeTriggerEntry.ActionToNode(a.Type, a));
        }

        this.tab = tab;
    }

    public override string Name => "Mission Trigger";
    public override string InternalId => Data.Nickname;

    static bool StartChild(NodeTriggerEntry e, out bool remove)
    {
        ImGui.BeginGroup();
        ImGui.PushStyleColor(ImGuiCol.Header, e.Color);
        ImGui.PushStyleColor(ImGuiCol.Button, e.Color);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0);
        remove = ImGui.Button(ImGuiExt.IDWithExtra($"{Icons.TrashAlt}", e.Id));
        ImGui.SameLine();
        var render = ImGui.CollapsingHeader(ImGuiExt.IDWithExtra(e.Name, (long)e.Id));
        ImGui.PopStyleVar();
        ImGui.PopStyleColor();
        ImGui.PopStyleColor();
        return render;
    }

    static void EndChild(bool render)
    {
        if(render)
            ImGui.Separator();
        ImGui.EndGroup();
    }

    float[] cachedHeightsCond;
    private float[] cachedHeightsAct;

    static float GetHeight(float[] cache, float fh, int index)
    {
        if (cache == null || index >= cache.Length ||
            cache[index] < float.Epsilon)
            return fh;
        return cache[index];
    }

    void RenderConditions(bool clipped, bool usePins, float szPin, float szContent, float pad,
        GameDataContext gameData, PopupManager popups, EditorUndoBuffer undoBuffer, ref NodePopups nodePopups,
        ref NodeLookups nodeLookups)
    {
        if (usePins)
        {
            ImGui.BeginTable("##conditions", 2, ImGuiTableFlags.PreciseWidths, new Vector2(szPin + szContent + pad, 0));
            ImGui.TableSetupColumn("##pins", ImGuiTableColumnFlags.WidthFixed, szPin);
            ImGui.TableSetupColumn("##content", ImGuiTableColumnFlags.WidthFixed, szContent);
        }

        var fh = ImGui.GetFrameHeightWithSpacing();

        for(var i = 0; i < Conditions.Count; i++)
        {
            var e = Conditions[i];
            if (usePins)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                foreach(var input in e.Inputs)
                {
                    var iconSize  = new Vector2(16);
                    NodeEditor.BeginPin(input.Id, PinKind.Input);
                    NodeEditor.PinPivotAlignment(new Vector2(0f, 0.5f));
                    NodeEditor.PinPivotSize(new Vector2(0, 0));
                    VectorIcons.Icon(iconSize, VectorIcon.Diamond, false, Color4.Green);
                    ImGui.SameLine();
                    ImGui.Text(input.LinkType.ToString());
                    NodeEditor.EndPin();
                }
                ImGui.TableNextColumn();
            }

            if (clipped)
            {
                ImGui.Dummy(new Vector2(1, GetHeight(cachedHeightsCond, fh, i)));
            }
            else
            {
                if (cachedHeightsCond == null ||
                    cachedHeightsCond.Length != Conditions.Count)
                    cachedHeightsCond = new float[Conditions.Count];
                var sp = ImGui.GetCursorPosY();
                bool c = StartChild(e, out var remove);
                if (c)
                {
                    ImGui.Dummy(new Vector2(1, 4)); //pad
                    e.RenderContent(gameData, popups, undoBuffer, ref nodePopups, ref nodeLookups);
                    ImGui.Dummy(new Vector2(1, 4)); //pad
                }
                EndChild(c);
                if (remove)
                {
                    tab.DeleteCondition(this, i);
                    i--;
                }

                if (i < cachedHeightsCond.Length && i >= 0)
                {
                    cachedHeightsCond[i] = ImGui.GetCursorPosY() - sp;
                }
            }
        }

        if (usePins)
        {
            ImGui.EndTable();
        }
    }

    void RenderActions(bool clipped, bool usePins, float szPin, float szContent, float pad,
        GameDataContext gameData, PopupManager popups, EditorUndoBuffer undoBuffer, ref NodePopups nodePopups,
        ref NodeLookups nodeLookups)
    {
        var fh = ImGui.GetFrameHeightWithSpacing();

        if (usePins)
        {
            ImGui.BeginTable("##actions", 2, ImGuiTableFlags.PreciseWidths, new Vector2(szPin + szContent + pad, 0));
            ImGui.TableSetupColumn("##content", ImGuiTableColumnFlags.WidthFixed, szContent);
            ImGui.TableSetupColumn("##pins", ImGuiTableColumnFlags.WidthFixed, szPin);
        }
        for (var i = 0; i < Actions.Count; i++)
        {
            var e = Actions[i];
            if (usePins)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
            }

            if (clipped)
            {
                ImGui.Dummy(new Vector2(1, GetHeight(cachedHeightsAct, fh, i)));
            }
            else
            {
                if (cachedHeightsAct == null ||
                    cachedHeightsAct.Length != Actions.Count)
                    cachedHeightsAct = new float[Actions.Count];
                var sp = ImGui.GetCursorPosY();
                var c = StartChild(e, out var remove);
                if (c)
                {
                    ImGui.Dummy(new Vector2(1, 4) ); //pad
                    e.RenderContent(gameData, popups, undoBuffer, ref nodePopups, ref nodeLookups);
                    ImGui.Dummy(new Vector2(1, 4)); //pad
                }
                EndChild(c);
                if (remove)
                {
                    tab.DeleteAction(this, i);
                    i--;
                }

                if (i < cachedHeightsCond.Length && i >= 0)
                {
                    cachedHeightsAct[i] = ImGui.GetCursorPosY() - sp;
                }
            }

            if (usePins)
            {
                ImGui.TableNextColumn();
                foreach(var o in e.Outputs)
                {
                    var iconSize  = new Vector2(16);
                    NodeEditor.BeginPin(o.Id, PinKind.Output);
                    NodeEditor.PinPivotAlignment(new Vector2(1f, 0.5f));
                    NodeEditor.PinPivotSize(new Vector2(0, 0));
                    ImGui.Text(o.LinkType.ToString());
                    ImGui.SameLine();
                    VectorIcons.Icon(iconSize, VectorIcon.Diamond, false, Color4.Green);
                    NodeEditor.EndPin();
                }
            }
        }

        if (usePins)
        {
            ImGui.EndTable();
        }
    }

    public float EstimateHeight()
    {
        var iHeight = 24;

        var maxItems = Math.Max(Conditions.Count, Actions.Count);

        return (maxItems * iHeight * 1.85f) + iHeight * 7; //7 controls + 1.85x items
    }

    public override bool OnContextMenu(PopupManager popups, EditorUndoBuffer undoBuffer)
    {
        if (ImGui.MenuItem("Add Action"))
        {
            popups.OpenPopup(new NewActionPopup(action =>
            {
                var node = NodeTriggerEntry.ActionToNode(action, null);
                undoBuffer.Commit(new ListAdd<NodeTriggerEntry>("Action", Actions, node));
            }));
        }

        if (ImGui.MenuItem("Add Condition"))
        {
            popups.OpenPopup(new NewConditionPopup(condition =>
            {
                var node = NodeTriggerEntry.ConditionToNode(condition, null);
                undoBuffer.Commit(new ListAdd<NodeTriggerEntry>("Condition", Conditions, node));
            }));
        }

        return ImGui.MenuItem("Delete Trigger");
    }

    public sealed override void Render(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodeLookups lookups)
    {
        // Measurements
        // Do we need to use pins?
        var conditionPin = Conditions.Any(t => t.Inputs.Count > 0);
        var actionPin = Actions.Any(t => t.Outputs.Count > 0);

        var szPin = 70;
        var szContent = 300;
        var szRight = szContent + (actionPin ? szPin : 0);
        var szLeft = szContent + (conditionPin ? szPin : 0);
        var pad = ImGui.GetStyle().FramePadding.X;


        var iconSize  = new Vector2(24);
        var nb = NodeBuilder.Begin(Id, suspend);

        nb.Header(Color);
        ImGui.Text(Name);
        nb.EndHeader();

        NodeEditor.BeginPin(Inputs[0].Id, PinKind.Input);
        NodeEditor.PinPivotAlignment(new Vector2(0f, 0.5f));
        NodeEditor.PinPivotSize(new Vector2(0, 0));
        VectorIcons.Icon(iconSize, VectorIcon.Flow, false, Color4.Green);
        ImGui.SameLine();
        ImGui.Text(Inputs[0].LinkType.ToString());
        NodeEditor.EndPin();

        var szTriggerPin = 75;
        var tWidth = szLeft + szRight + 8 * pad;
        ImGui.SameLine();
        ImGui.Dummy(new Vector2(tWidth - 2 * szTriggerPin, 1));
        ImGui.SameLine();

        NodeEditor.BeginPin(Outputs[0].Id, PinKind.Output);
        NodeEditor.PinPivotAlignment(new Vector2(1f, 0.5f));
        NodeEditor.PinPivotSize(new Vector2(0, 0));
        ImGui.Text(Outputs[0].LinkType.ToString());
        ImGui.SameLine();
        VectorIcons.Icon(iconSize, VectorIcon.Flow, false, Color4.Green);
        NodeEditor.EndPin();

        if (nb.Clipped)
        {
            ImGui.Dummy(new(180, ImGui.GetFrameHeightWithSpacing() * 3 + ImGui.GetFrameHeight()));
        }
        else
        {
            ImGui.PushItemWidth(180);
            ImGui.AlignTextToFramePadding();
            ImGui.Text("ID");
            ImGui.SameLine();
            ImGuiExt.InputTextLogged("##id", ref Data.Nickname, 255, (old, upd) =>
            {
                tab.OnRenameTrigger(this, old, upd);
            }, true);
            nb.Popups.StringCombo("System", undoBuffer, () => ref Data.System, gameData.SystemsByName, true);
            Controls.CheckboxUndo("Repeatable", undoBuffer, () => ref Data.Repeatable);
            nb.Popups.Combo("Initial State", undoBuffer, () => ref Data.InitState);
            ImGui.PopItemWidth();
        }


        // Draw conditions/actions
        ImGui.BeginTable("##trigger", 2, ImGuiTableFlags.PreciseWidths, new Vector2(szLeft + szRight + 8 * pad, 0));
        ImGui.TableSetupColumn("Conditions", ImGuiTableColumnFlags.WidthFixed, szLeft + 2 * pad);
        ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, szRight + 2 * pad);
        ImGui.TableHeadersRow();
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        RenderConditions(nb.Clipped, conditionPin, szPin, szContent, pad, gameData, popup, undoBuffer, ref nb.Popups, ref lookups);
        ImGui.TableNextColumn();
        RenderActions(nb.Clipped ,actionPin, szPin, szContent, pad, gameData, popup, undoBuffer, ref nb.Popups, ref lookups);
        ImGui.EndTable();
        nb.Dispose();

    }

    public void WriteNode(MissionScriptEditorTab missionEditor, IniBuilder builder)
    {
        var s = builder.Section("Trigger");


        if (string.IsNullOrWhiteSpace(Data.Nickname))
        {
            return;
        }

        s.Entry("nickname", Data.Nickname);
        if(Data.InitState != TriggerInitState.INACTIVE)
            s.Entry("InitState", Data.InitState.ToString());
        if (Data.System != string.Empty)
        {
            s.Entry("system", Data.System);
        }

        s.OptionalEntry("repeatable", Data.Repeatable);

        foreach (var condition in Conditions)
        {
            condition.WriteEntry(s);
        }

        foreach (var action in Actions)
        {
            action.WriteEntry(s);
        }
    }
}
