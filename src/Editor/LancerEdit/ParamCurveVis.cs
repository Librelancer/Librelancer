// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ImUI;
using LibreLancer.Thn;

namespace LancerEdit
{
    public class ParamCurveVis : EditorTab
    {
        List<Vector4> points =new List<Vector4>();
        public ParamCurveVis()
        {
            Title = "ParamCurve";
            RecreateCurve();
        }

        private ParameterCurve currentCurve;
        private Vector4 p = Vector4.Zero;
        private float[] values;
        private float scaleMin = 0;
        private float scaleMax = 1;
        void RecreateCurve()
        {
            currentCurve = new ParameterCurve(types[typeIndex], points);
            const int STEPS = 1000;
            values = new float[STEPS + 1];
            scaleMax = 1;
            for (int i = 0; i < STEPS + 1; i++)
            {
                var t = (1.0f / STEPS) * i;
                values[i] = currentCurve.GetValue(t, 1);
                if (values[i] > scaleMax)
                    scaleMax = values[i];
            }
        }

        private int typeIndex = 0;
        private PCurveType[] types =
        {
            PCurveType.FreeForm,
            PCurveType.Linear,
            PCurveType.Step,
            PCurveType.RampUp,
            PCurveType.RampDown,
            PCurveType.BumpIn,
            PCurveType.BumpOut,
            PCurveType.Smooth
        };

        private string[] typeNames =
        {
            "FreeForm",
            "Linear",
            "Step",
            "RampUp",
            "RampDown",
            "BumpIn",
            "BumpOut",
            "Smooth"
        };
        public override void Draw(double elapsed)
        {
            int idx = typeIndex;
            ImGui.Combo("Type", ref typeIndex, typeNames, typeNames.Length);
            if(idx != typeIndex) RecreateCurve();
            ImGui.BeginTabBar("##tabs");
            if (ImGui.BeginTabItem("Points"))
            {
                ImGui.InputFloat4("Point", ref p);
                if (ImGui.Button("Add"))
                {
                    points.Add(p);
                    p = Vector4.Zero;
                    RecreateCurve();
                }

                ImGui.SameLine();
                if (ImGui.Button("Clear"))
                {
                    points.Clear();
                    RecreateCurve();
                }
                var height = ImGui.GetWindowHeight() - (95 * ImGuiHelper.Scale);
                ImGui.BeginChild("##points", new Vector2(-1, height), ImGuiChildFlags.Border);
                int ik = 0;
                foreach (var p in points)
                {
                    ImGui.Selectable(ImGuiExt.IDWithExtra(p.ToString(), ik++));
                }
                ImGui.EndChild();


                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Graph"))
            {
                var gWidth = ImGui.GetContentRegionAvail().X - (32 * ImGuiHelper.Scale);
                var gHeight = ImGui.GetWindowHeight() - (48 * ImGuiHelper.Scale);
                ImGui.PlotLines("##graph", ref values[0], values.Length, 0, "", scaleMin, scaleMax, new Vector2(gWidth, gHeight));
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }
}
