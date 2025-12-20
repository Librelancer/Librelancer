using System;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Data;
using LibreLancer.Data.Schema;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent.Popups;

public class AttenuationPopup : PopupWindow
{
    private Vector3 attenuation3 = new Vector3(1, 0, 0);
    private float range = 1000f;

    public override string Title { get; set; } = "Attenuation";

    public bool EnableAttenCurve = true;

    public bool IsAttenCurve = false;

    public GameDataManager GameData;

    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize;

    private Action<string, Vector3> onSet;


    public AttenuationPopup(
        FloatGraph curve,
        Vector3 atten3,
        bool isAttenCurve,
        bool enableAttenCurve,
        float range,
        Action<string, Vector3> onSet,
        GameDataManager gameData)
    {
        IsAttenCurve = isAttenCurve;
        EnableAttenCurve = enableAttenCurve;
        attenuation3 = atten3;
        this.range = range;
        this.GameData = gameData;
        this.onSet = onSet;
        SetCurve(curve);
    }

    public override void Draw(bool appearing)
    {
        if (EnableAttenCurve)
        {
            if (ImGuiExt.ToggleButton("AttenCurve", IsAttenCurve))
                IsAttenCurve = true;
            ImGui.SameLine();
            if (ImGuiExt.ToggleButton("D3D Attenuation", !IsAttenCurve))
                IsAttenCurve = false;
        }
        if(!EnableAttenCurve || !IsAttenCurve)
            EditAttenuation3();
        else
        {
            EditAttenCurve();
        }
        if (ImGui.Button("Ok"))
        {
            onSet(IsAttenCurve ? selectedCurve.Name : null, IsAttenCurve ? quadratic : attenuation3);
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
        {
            ImGui.CloseCurrentPopup();
        }
    }


    private Vector3 quadratic;
    private FloatGraph selectedCurve;

    void SetCurve(FloatGraph graph)
    {
        selectedCurve = graph;
        if (graph != null)
        {
            quadratic = ApproximateCurve.GetQuadraticFunction(graph.Points.ToArray());
        }
    }

    void EditAttenCurve()
    {
        if (selectedCurve == null)
            SetCurve(GameData.Items.Ini.Graphs.FloatGraphs[0]);

        if (ImGui.BeginCombo("Curve", selectedCurve.Name))
        {
            foreach (var graph in GameData.Items.Ini.Graphs.FloatGraphs) {
                if (ImGui.Selectable(graph.Name, selectedCurve == graph))
                {
                    SetCurve(graph);
                }
            }
            ImGui.EndCombo();
        }

        ImGui.Text($"Previewing over {range} units");
        Span<float> points = stackalloc float[200];
        for (int i = 0; i < points.Length; i++)
        {
            var t = (1.0f / 199) * i;
            var value = (t * t) * quadratic.X +
                        t * quadratic.Y +
                        quadratic.Z;
            points[i] = value;
        }
        PlotAdv.PlotLines("##preview", points, (i, f) =>
        {
            var d = i * (range / 199f);
            return $"{d}: {f}";
        }, null, 0, 1.2f, new Vector2(150 * ImGuiHelper.Scale));
    }

    void EditAttenuation3()
    {
        ImGui.Text("Attenuation = 1.0 / (a + bd + cd^2)");
        ImGui.Text("Where d = distance from vertex/pixel to light");
        ImGui.PushItemWidth(150 * ImGuiHelper.Scale);
        ImGui.AlignTextToFramePadding();
        ImGui.Text("a: ");
        ImGui.SameLine();
        ImGui.InputFloat("##valueX", ref attenuation3.X, 0, 0, "%.7f");
        if (attenuation3.X < 0.0000001f)
            attenuation3.X = 0.0000001f;
        ImGui.AlignTextToFramePadding();
        ImGui.Text("b: ");
        ImGui.SameLine();
        ImGui.InputFloat("##valueY", ref attenuation3.Y, 0, 0, "%.7f");
        ImGui.AlignTextToFramePadding();
        ImGui.Text("c: ");
        ImGui.SameLine();
        ImGui.InputFloat("##valueZ",  ref attenuation3.Z, 0, 0, "%.7f");
        ImGui.PopItemWidth();
        ImGui.Text($"Previewing over {range} units");
        Span<float> points = stackalloc float[200];
        for (int i = 0; i < points.Length; i++)
        {
            var d = i * (range / 199f);
            points[i] = 1.0f / (attenuation3.X + (attenuation3.Y * d) + (attenuation3.Z * (d * d)));
        }

        PlotAdv.PlotLines("##preview", points, (i, f) =>
        {
            var d = i * (range / 199f);
            return $"{d}: {f}";
        }, null, 0, 1.2f, new Vector2(150 * ImGuiHelper.Scale));
    }

}
