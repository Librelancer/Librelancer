using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes;

public abstract class Node(VertexDiffuse? color = null)
{
    public NodeId Id { get; } = NodeEditorId.Next();
    public abstract string Name  { get; }
    public virtual string InternalId => "";
    public List<NodePin> Inputs  { get; } = [];
    public List<NodePin> Outputs  { get; } = [];
    public VertexDiffuse Color  { get; } = color ?? (VertexDiffuse)Color4.White;

    public abstract void Render(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodeLookups lookups);

    private Vector2? cachedSizes = null;

    // May not ever be called
    protected void PinsChanged()
    {
        cachedSizes = null;
    }

    protected void LayoutNode(float fixedWidth)
    {
        var iconSize= 24 * ImGuiHelper.Scale;
        var padding = 15 * ImGuiHelper.Scale;

        if (cachedSizes == null)
        {
            var pinsIn = Inputs.Select(x => x.LinkType.ToString());
            var pinsOut = Outputs.Select(x => x.LinkType.ToString());
            var mIn = pinsIn.Select(p => ImGui.CalcTextSize(p).X).Select(x => x + iconSize + padding).Prepend(0).Max();
            var mOut = pinsOut.Select(p => ImGui.CalcTextSize(p).X).Select(x => x + iconSize + padding).Prepend(0).Max();
            cachedSizes = new Vector2(mIn, mOut);
        }

        var maxIn = cachedSizes.Value.X;
        var maxOut = cachedSizes.Value.Y;

        ImGui.BeginTable("##layout", 3, ImGuiTableFlags.PreciseWidths, new Vector2(maxIn + maxOut + fixedWidth + 4 * ImGuiHelper.Scale, 0),
            maxIn + maxOut + fixedWidth);
        ImGui.TableSetupColumn("##in", ImGuiTableColumnFlags.WidthFixed, maxIn);
        ImGui.TableSetupColumn("##fixed", ImGuiTableColumnFlags.WidthFixed, fixedWidth);
        ImGui.TableSetupColumn("##out", ImGuiTableColumnFlags.WidthFixed, maxOut);
        ImGui.TableNextRow();
    }

    protected static void StartInputs()
    {
        ImGui.TableSetColumnIndex(0);
    }

    protected static void StartFixed()
    {
        ImGui.TableSetColumnIndex(1);
    }

    protected static void StartOutputs()
    {
        ImGui.TableSetColumnIndex(2);
    }

    protected static void EndNodeLayout()
    {
        ImGui.EndTable();
    }

    public virtual void OnLinkCreated(NodeLink link)
    {
    }

    public virtual void OnLinkRemoved(NodeLink link)
    {
    }

    public virtual bool OnContextMenu(PopupManager popups, EditorUndoBuffer undoBuffer) =>
        ImGui.MenuItem("Delete");
}
