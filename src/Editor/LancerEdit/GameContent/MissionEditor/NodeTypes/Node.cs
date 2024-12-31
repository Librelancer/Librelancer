using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes;

public abstract class Node(int id, VertexDiffuse? color = null)
{
    protected NodeId Id { get; } = id;
    protected abstract string Name  { get; }
    public List<NodePin> Inputs  { get; } = [];
    public List<NodePin> Outputs  { get; } = [];
    public VertexDiffuse Color  { get; } = color ?? (VertexDiffuse)Color4.White;

    protected static readonly Dictionary<Type, NodeValueRenderer<object>> NodeValueRenders = new();

    public delegate void NodeValueRenderer<in T>(GameDataContext context, MissionScript script, ref NodePopups popups, T item);
    public static void RegisterNodeValueRenderer<T>(NodeValueRenderer<T> values)
    {
        NodeValueRenders[typeof(T)] = (GameDataContext context, MissionScript script, ref NodePopups popups, object obj)
            => values(context, script, ref popups, (T)obj);
    }

    public abstract void Render(GameDataContext gameData, PopupManager popup, MissionIni missionIni);

    protected static void LayoutNode(IEnumerable<string> pinsIn, IEnumerable<string> pinsOut, float fixedWidth)
    {
        var iconSize= 24 * ImGuiHelper.Scale;
        var padding = 15 * ImGuiHelper.Scale;

        var maxIn = pinsIn.Select(p => ImGui.CalcTextSize(p).X).Select(x => x + iconSize + padding).Prepend(0).Max();
        var maxOut = pinsOut.Select(p => ImGui.CalcTextSize(p).X).Select(x => x + iconSize + padding).Prepend(0).Max();

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
}
