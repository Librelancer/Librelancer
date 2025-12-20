using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.Data.Schema.Pilots;
using LibreLancer.Dialogs;
using LibreLancer.ImUI;
using LibreLancer.Server.Ai;

namespace LancerEdit;

public class StateGraphTab : EditorTab
{
    private MainWindow win;
    private List<StateGraph> graphs;
    private StateGraph selectedGraph;

    private NameInputConfig newName;
    private NameInputConfig dupName;
    private int modTypeIndex = 0;
    private string[] types = { "LEADER", "ESCORT" };

    public string FilePath;

    public StateGraphTab(MainWindow win, StateGraphDb stateGraphDb, string path)
    {
        this.win = win;
        Title = Path.GetFileName(path);
        FilePath = path;
        graphs = stateGraphDb.Tables.Values.ToList();
        newName = new NameInputConfig()
        {
            Extra = TypeModifier,
            Title = "New State Graph",
            ValueName = "Name",
            InUse = GraphInUse
        };
        dupName = new NameInputConfig()
        {
            Extra = TypeModifier,
            Title = "Duplicate State Graph",
            ValueName = "Name",
            InUse = GraphInUse
        };
        SaveStrategy = new StateGraphSaveStrategy(this);
    }

    public void Save(string filePath)
    {
        using var fs = File.Create(filePath ?? FilePath);
        var db = new StateGraphDb();
        db.BehaviorCount = 21;
        db.StateGraphCount = graphs.Count;
        foreach (var g in graphs)
            db.Tables[g.Description] = g;
        StateGraphWriter.Write(fs, db);
        win.OnSaved();
    }

    void NamePopup(NameInputConfig config, Action<string, string> callback)
    {
        modTypeIndex = 0;
        win.Popups.OpenPopup(new NameInputPopup(config, "", x => callback(x, types[modTypeIndex])));
    }

    bool GraphInUse(string name) => graphs.Any(x =>
        x.Description.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
        x.Description.Type.Equals(types[modTypeIndex], StringComparison.OrdinalIgnoreCase));

    void TypeModifier()
    {
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Type:");
        ImGui.SameLine();
        ImGui.Combo("##type", ref modTypeIndex, types, types.Length);
    }

    private int lastHoveredX = -1;
    private int lastHoveredY = -1;

    private Point editIndex = new(-100, -100);
    private float originalValue = 0;

    private EditorUndoBuffer undoBuffer = new EditorUndoBuffer();

    class SetValueAction(StateGraph db, int x, int y, float old, float updated) : EditorModification<float>(old, updated)
    {
        public override void Set(float value) => db.Data[y][x] = value;
        public override string ToString() => $"({(StateGraphEntry)y}, {(StateGraphEntry)x}) = {updated}";
    }

    class AddGraphAction(StateGraph db, List<StateGraph> graphs) : EditorAction
    {
        public override void Commit() => graphs.Add(db);
        public override void Undo() => graphs.Remove(db);
        public override string ToString() => $"Add {db.Description.Name} ({db.Description.Type})";
    }

    class DeleteGraphAction(StateGraph db, List<StateGraph> graphs) : EditorAction
    {
        private int idx;
        public override void Commit()
        {
            idx = graphs.IndexOf(db);
            if (idx == -1)
                throw new InvalidOperationException();
            graphs.RemoveAt(idx);
        }
        public override void Undo() => graphs.Insert(idx, db);
        public override string ToString() => $"Delete {db.Description.Name} ({db.Description.Type})";
    }

    static float ClampRound(float d) => MathHelper.Clamp(MathF.Round(d, 2), 0f, 1f);

    // ReSharper disable once CompareOfFloatsByEqualityOperator
    bool NeedsChange(float newValue) => ClampRound(newValue) != originalValue;

    string FmtName(StateGraph g) => g == null ? "" : $"{g.Description.Name} ({g.Description.Type})";

    public override void OnHotkey(Hotkeys hk, bool shiftPressed)
    {
        switch (hk)
        {
            case Hotkeys.Undo when undoBuffer.CanUndo:
                undoBuffer.Undo();
                break;
            case Hotkeys.Redo when undoBuffer.CanRedo:
                undoBuffer.Redo();
                break;
        }
    }

    private bool showHistory;
    public override void Draw(double elapsed)
    {
        Span<char> chars = stackalloc char[1024];

        var style = ImGui.GetStyle();
        var szX = ImGui.CalcTextSize($"State Graph:{Icons.PlusCircle}{Icons.Copy}{Icons.TrashAlt}History").X +
                  style.ItemSpacing.X * 6 + style.FramePadding.X * 10; //6 items, 5 frames (4 buttons)

        ImGui.AlignTextToFramePadding();
        ImGui.Text("State Graph: ");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - szX);
        if(ImGui.BeginCombo("##stateGraph", FmtName(selectedGraph)))
        {
            foreach (var g in graphs) {
                if (ImGui.Selectable(FmtName(g)))
                {
                    selectedGraph = g;
                }
            }
            ImGui.EndCombo();
        }
        ImGui.SameLine();
        if (ImGui.Button($"{Icons.PlusCircle}")) {
            NamePopup(newName, (name, type) =>
            {
                var newg = new StateGraph(new StateGraphDescription(name, type));
                for (int i = 0; i < (int)StateGraphEntry._Count; i++) {
                    newg.Data.Add(new float[(int)StateGraphEntry._Count]);
                }
                undoBuffer.Commit(new AddGraphAction(newg, graphs));
            });
        }
        ImGui.SameLine();
        if (ImGuiExt.Button($"{Icons.Copy}", selectedGraph != null)) {
            NamePopup(newName, (name, type) =>
            {
                var newg = new StateGraph(new StateGraphDescription(name, type));
                foreach (var line in selectedGraph.Data) {
                    newg.Data.Add(line.ToArray());
                }
                undoBuffer.Commit(new AddGraphAction(newg, graphs));
            });
        }
        ImGui.SameLine();
        bool delCurrent = ImGuiExt.Button($"{Icons.TrashAlt}", selectedGraph != null);
        ImGui.SameLine();
        if (ImGuiExt.ToggleButton("History", showHistory)) {
            showHistory = !showHistory;
        }
        if (showHistory) {
            undoBuffer.DisplayStack();
        }
        if (selectedGraph == null)
            return;
        var tab = selectedGraph;
        int hoveredX = -1, hoveredY = -1;
        if (ImGui.BeginTable("stategraphTable", (int) StateGraphEntry._Count + 1,
                ImGuiTableFlags.Resizable | ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableHeadersRow();
            for (int i = 0; i < (int)StateGraphEntry._Count; i++)
            {
                ImGui.TableSetColumnIndex(i + 1);
                if(lastHoveredX == i)
                    ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, (VertexDiffuse)Color4.CornflowerBlue);
                ImGui.Text(((StateGraphEntry)i).ToString());
            }
            for (int y = 0; y < tab.Data.Count; y++)
            {
                ImGui.PushID(y);
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.TableHeader(((StateGraphEntry)y).ToString());
                if(lastHoveredY == y)
                    ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, (VertexDiffuse)Color4.CornflowerBlue);
                ImGui.PushFont(ImGuiHelper.SystemMonospace, 0);
                for (int x = 0; x < (int) StateGraphEntry._Count; x++)
                {
                    ImGui.PushID(x);
                    ImGui.TableSetColumnIndex(x + 1);
                    if (editIndex.X == x && editIndex.Y == y)
                    {
                        var d = tab.Data[y][x];
                        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
                        ImGui.SetNextItemWidth(-1);
                        bool didEnter = false;
                        ImGui.InputFloat("##edit", ref d, 0.00f, 0.0f, "%.2f");
                        if(ImGui.IsItemDeactivatedAfterEdit())
                        {
                            didEnter = true;
                            if(NeedsChange(ClampRound(d)))
                                undoBuffer.Commit(new SetValueAction(tab, x, y, originalValue, ClampRound(d)));
                            editIndex = new Point(-100, -100);
                        }
                        ImGui.PopStyleVar();
                        d = ClampRound(d);
                        tab.Data[y][x] = d;
                        if (!didEnter && ImGui.IsItemDeactivated())
                        {
                            tab.Data[y][x] = originalValue;
                            editIndex = new Point(-100, -100);
                        }
                    }
                    else
                    {
                        if (ImGui.Selectable(tab.Data[y][x].ToString("F2"), false,
                                ImGuiSelectableFlags.AllowDoubleClick) &&
                            ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                        {
                            if (editIndex.X >= 0 && editIndex.Y >= 0 && NeedsChange(tab.Data[editIndex.Y][editIndex.X]))
                            {
                                undoBuffer.Commit(new SetValueAction(tab, editIndex.X, editIndex.Y, originalValue, ClampRound(tab.Data[y][x])));
                            }
                            originalValue = tab.Data[y][x];
                            editIndex = new Point(x, y);
                        }
                    }
                    if (ImGui.IsItemHovered()) {
                        hoveredX = x;
                        hoveredY = y;
                    }

                    ImGui.PopID();
                }
                ImGui.PopFont();
                ImGui.PopID();
            }
            ImGui.EndTable();
        }
        lastHoveredX = hoveredX;
        lastHoveredY = hoveredY;
        if (delCurrent) {
            undoBuffer.Commit(new DeleteGraphAction(tab, graphs));
            selectedGraph = null;
        }
    }
}
