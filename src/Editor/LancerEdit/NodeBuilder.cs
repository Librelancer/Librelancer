using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;

namespace LancerEdit;

public enum NodeStage
{
    Invalid,
    Begin,
    Header,
    Content,
    End
}

public struct NodeBuilder : IDisposable
{
    public static int HeaderTextureId;
    public static int HeaderTextureWidth;
    public static int HeaderTextureHeight;
    public static void LoadTexture()
    {
        if (HeaderTextureId != 0)
            return;

        using var stream = typeof(MainWindow).Assembly.GetManifestResourceStream("LancerEdit.BlueprintBackground.png");
        var icon = (Texture2D)LibreLancer.ImageLib.Generic.FromStream(stream);
        HeaderTextureId = ImGuiHelper.RegisterTexture(icon);
        HeaderTextureWidth = icon.Width;
        HeaderTextureHeight = icon.Height;
    }

    public bool HasHeader;
    public Vector2 NodeMin;
    public Vector2 NodeMax;
    public Vector2 ContentMin;
    public Vector2 ContentMax;
    public Vector2 HeaderMin;
    public Vector2 HeaderMax;
    public uint HeaderColor;
    public NodeId CurrentId;
    public NodeStage CurrentStage;
    private string setTooltip;
    private int comboIndex;
    private ComboData[] combos;
    record struct ComboData(bool Open, Action<int> Set, string Id, string[] Values);

    private StringComboData[] strCombos;
    private int strComboIndex;
    record struct StringComboData(bool Open, Action<string> Set, string Id, IEnumerable<string> Values);

    public static NodeBuilder Begin(NodeId id)
    {
        NodeEditor.PushStyleVar(StyleVar.NodePadding, new Vector4(8,4,8,8));
        NodeEditor.BeginNode(id);
        ImGui.PushID(id);
        var bp = new NodeBuilder()
        {
            CurrentId = id,
            HeaderColor = ImGui.GetColorU32(Color4.Blue),
            combos = ArrayPool<ComboData>.Shared.Rent(16),
        };

        bp.strCombos = ArrayPool<StringComboData>.Shared.Rent(16);
        bp.SetStage(NodeStage.Begin);
        return bp;
    }

    void SetStage(NodeStage stage)
    {
        if (stage == CurrentStage)
            return;
        var oldStage = CurrentStage;
        CurrentStage = stage;
        switch (oldStage) {
            case NodeStage.Begin:
                ImGui.BeginGroup();
                break;
            case NodeStage.Header:
                ImGui.EndGroup();
                HeaderMin = ImGui.GetItemRectMin();
                HeaderMax = ImGui.GetItemRectMax();
                break;
        }
        switch (stage)
        {
            case NodeStage.Header:
                HasHeader = true;
                ImGui.BeginGroup();
                break;
            case NodeStage.Content:
                ImGui.BeginGroup();
                break;
            case NodeStage.End:
                ImGui.EndGroup();
                ContentMin = ImGui.GetItemRectMin();
                ContentMax = ImGui.GetItemRectMax();
                ImGui.EndGroup();
                NodeMin = ImGui.GetItemRectMin();
                NodeMax = ImGui.GetItemRectMax();
                break;
        }
    }

    public void Header(Color4 color)
    {
        HeaderColor = ImGui.GetColorU32(color);
        SetStage(NodeStage.Header);
    }

    public void Content() => SetStage(NodeStage.Content);

    public void EndHeader()
    {
        SetStage(NodeStage.Content);
    }

    public void Tooltip(string tooltip)
    {
        setTooltip = tooltip;
    }

    public void Combo(string title, int selectedValue, Action<int> set, string[] values)
    {
        ImGui.AlignTextToFramePadding();
        ImGui.Text(title);
        ImGui.SameLine();
        combos[comboIndex++] = new ComboData(ImGuiExt.ComboButton(title, values[selectedValue]), set, title, values);
    }

    public void StringCombo(string title, string selectedValue, Action<string> set, IEnumerable<string> values)
    {
        ImGui.AlignTextToFramePadding();
        ImGui.Text(title);
        ImGui.SameLine();

        var enumerable = values as string[] ?? values.ToArray();
        strCombos[strComboIndex++] = new StringComboData(ImGuiExt.ComboButton(title, enumerable.First(x => x == selectedValue)), set, title, enumerable);
    }

    private static readonly Dictionary<Type, string[]> _enums = new Dictionary<Type, string[]>();
    public void Combo<T>(string title, T selectedValue, Action<T> set) where T : struct, Enum
    {
        if (!_enums.TryGetValue(typeof(T), out var values)) {
            values = Enum.GetNames<T>();
            _enums[typeof(T)] = values;
        }
        Combo(title, Unsafe.As<T, int>(ref selectedValue), x => set(Unsafe.As<int, T>(ref x)), values);
    }
    public void Dispose() => End();
    public unsafe void End()
    {
        SetStage(NodeStage.End);
        NodeEditor.EndNode();
        if (ImGui.IsItemVisible())
        {
            var alpha = ImGui.GetStyle().Alpha;
            var drawList = NodeEditor.GetNodeBackgroundDrawList(CurrentId);
            var halfBorderWidth = NodeEditor.GetStyle()->NodeBorderWidth * 0.5f;
            var color = ImGui.GetColorU32(new Vector4(0, 0, 0, alpha)) |
                        (HeaderColor & ImGui.GetColorU32(new Vector4(1, 1, 1, 0)));
            if ((HeaderMax.X > HeaderMin.X) && (HeaderMax.Y > HeaderMin.Y) && (HeaderTextureId != 0))
            {
                if (HeaderMax.X < NodeMax.X) //no spring layout, get max
                    HeaderMax.X = NodeMax.X;

                var uv = new Vector2(
                    (HeaderMax.X - HeaderMin.X) / (float)(4.0f * HeaderTextureWidth),
                (HeaderMax.Y - HeaderMin.Y) / (float)(4.0f * HeaderTextureHeight));
                drawList.AddImageRounded((IntPtr)HeaderTextureId,
                    HeaderMin - new Vector2(8 - halfBorderWidth, 4 - halfBorderWidth),
                    HeaderMax + new Vector2(8 - halfBorderWidth, 0),
                    Vector2.Zero, uv,
                    HeaderColor,
                    NodeEditor.GetStyle()->NodeRounding, ImDrawFlags.RoundCornersTop
                    );
                if (ContentMin.Y > HeaderMax.Y)
                {
                    drawList.AddLine(
                        new Vector2(HeaderMin.X - (8 - halfBorderWidth), HeaderMax.Y - 0.5f),
                        new Vector2(HeaderMax.X + (8 - halfBorderWidth), HeaderMax.Y - 0.5f),
                        ImGui.GetColorU32(new Vector4(1,1,1, 0.37f * (alpha / 3f))), 1.0f);
                }
            }
        }

        ImGui.PopID();
        NodeEditor.PopStyleVar();

        SetStage(NodeStage.Invalid);
        NodeEditor.Suspend();

        if(!string.IsNullOrWhiteSpace(setTooltip))
            ImGui.SetTooltip(setTooltip);

        ImGui.PushID((int)CurrentId);

        for (var i = 0; i < comboIndex; i++)
        {
            var c = combos[i];
            combos[i] = default;
            if(c.Open)
                ImGui.OpenPopup(c.Id);
            if (!ImGui.BeginPopup(c.Id, ImGuiWindowFlags.Popup))
                continue;

            for (var j = 0; j < c.Values.Length; j++)
            {
                ImGui.PushID(j);
                if (ImGui.MenuItem(c.Values[j]))
                    c.Set(j);
                ImGui.PopID();
            }
            ImGui.EndPopup();
        }

        for (var i = 0; i < strComboIndex; i++)
        {
            var c = strCombos[i];
            strCombos[i] = default;
            if(c.Open)
                ImGui.OpenPopup(c.Id);
            if (!ImGui.BeginPopup(c.Id, ImGuiWindowFlags.Popup))
            {
                continue;
            }

            var j = 0;
            foreach(var v in c.Values)
            {
                ImGui.PushID(j++);
                if (ImGui.MenuItem(v))
                    c.Set(v);
                ImGui.PopID();
            }
            ImGui.EndPopup();
        }

        ImGui.PopID();
        ArrayPool<ComboData>.Shared.Return(combos);
        ArrayPool<StringComboData>.Shared.Return(strCombos);
        NodeEditor.Resume();
    }
}
