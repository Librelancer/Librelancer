// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer;
using LibreLancer.ImUI;
using ImGuiNET;

namespace LancerEdit
{
    public class OptionsWindow
    {
        private EditorConfiguration config;
        private RenderContext rstate;
        private ImGuiHelper guiHelper;

        static readonly string[] defaultFilters =
        {
            "Linear", "Bilinear", "Trilinear"
        };

        int[] msaaLevels;
        string[] msaaStrings = {"None", "2x MSAA", "4x MSAA", "8x MSAA", "16x MSAA", "32x MSAA"};
        int cMsaa = 0;
        string[] filters;
        int[] anisotropyLevels;
        int cFilter = 2;
        private bool windowOpen = false;

        private static readonly DropdownOption[] camModesNormal = new[]
        {
            new DropdownOption("Arcball", Icons.Globe, CameraModes.Arcball),
            new DropdownOption("Walkthrough", Icons.StreetView, CameraModes.Walkthrough)
        };

        public OptionsWindow(MainWindow win)
        {
            config = win.Config;
            rstate = win.RenderContext;
            guiHelper = win.guiHelper;

            var texturefilters = new List<string>(defaultFilters);
            if (win.RenderContext.MaxAnisotropy > 0)
            {
                anisotropyLevels = win.RenderContext.GetAnisotropyLevels();
                foreach (var lvl in anisotropyLevels)
                {
                    texturefilters.Add(string.Format("Anisotropic {0}x", lvl));
                }
            }

            var msaa = new List<int> {0};
            int a = 2;
            while (a <= win.RenderContext.MaxSamples)
            {
                msaa.Add(a);
                a *= 2;
            }

            msaaLevels = msaa.ToArray();
            switch (config.MSAA)
            {
                case 2:
                    cMsaa = 1;
                    break;
                case 4:
                    cMsaa = 2;
                    break;
                case 8:
                    cMsaa = 3;
                    break;
                case 16:
                    cMsaa = 4;
                    break;
                case 32:
                    cMsaa = 5;
                    break;
            }

            filters = texturefilters.ToArray();
            cFilter = config.TextureFilter;
            SetTexFilter();
        }

        void SetTexFilter()
        {
            switch (cFilter)
            {
                case 0:
                    rstate.PreferredFilterLevel = TextureFiltering.Linear;
                    break;
                case 1:
                    rstate.PreferredFilterLevel = TextureFiltering.Bilinear;
                    break;
                case 2:
                    rstate.PreferredFilterLevel = TextureFiltering.Trilinear;
                    break;
                default:
                    rstate.AnisotropyLevel = anisotropyLevels[cFilter - 3];
                    rstate.PreferredFilterLevel = TextureFiltering.Anisotropic;
                    break;
            }
        }

        public void Show()
        {
            windowOpen = true;
        }

        Vector3 editCol;
        Vector3 editCol2;
        private bool editGrad;

        void EditorTab()
        {
            Controls.DropdownButton("Default Camera", ref config.DefaultCameraMode, camModesNormal);
            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Default Camera");
            var pastC = cFilter;
            ImGui.Combo("Texture Filter", ref cFilter, filters, filters.Length);
            if (cFilter != pastC)
            {
                SetTexFilter();
                config.TextureFilter = cFilter;
            }

            ImGui.Combo("Antialiasing", ref cMsaa, msaaStrings, Math.Min(msaaLevels.Length, msaaStrings.Length));
            config.MSAA = msaaLevels[cMsaa];
            ImGui.Checkbox("View Buttons", ref config.ViewButtons);
            ImGui.Checkbox("Pause When Unfocused", ref config.PauseWhenUnfocused);
            if (Controls.GradientButton("Viewport Background", config.Background, config.Background2,
                    new Vector2(22 * ImGuiHelper.Scale), config.BackgroundGradient))
            {
                ImGui.OpenPopup("Viewport Background");
                editCol = new Vector3(config.Background.R, config.Background.G, config.Background.B);
                editCol2 = new Vector3(config.Background2.R, config.Background2.G, config.Background2.B);
                editGrad = config.BackgroundGradient;
            }

            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Viewport Background");
            bool wOpen = true;
            if (ImGui.BeginPopupModal("Viewport Background", ref wOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Checkbox("Gradient", ref editGrad);

                ImGui.ColorPicker3(editGrad ? "Top###a" : "###a", ref editCol);
                if (editGrad)
                {
                    ImGui.SameLine();
                    ImGui.ColorPicker3("Bottom###b", ref editCol2);
                }

                if (ImGui.Button("OK"))
                {
                    config.Background = new Color4(editCol.X, editCol.Y, editCol.Z, 1);
                    config.Background2 = new Color4(editCol2.X, editCol2.Y, editCol2.Z, 1);
                    config.BackgroundGradient = editGrad;
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();
                if (ImGui.Button("Default"))
                {
                    var def = Color4.CornflowerBlue * new Color4(0.3f, 0.3f, 0.3f, 1f);
                    editCol = new Vector3(def.R, def.G, def.B);
                    editGrad = false;
                }

                ImGui.SameLine();
                if (ImGui.Button("Cancel")) ImGui.CloseCurrentPopup();
                ImGui.EndPopup();
            }

            if (ImGui.ColorButton("Grid Color", config.GridColor, ImGuiColorEditFlags.NoAlpha,
                    new Vector2(22 * ImGuiHelper.Scale)))
            {
                ImGui.OpenPopup("Grid Color");
                editCol = new Vector3(config.GridColor.R, config.GridColor.G, config.GridColor.B);
            }

            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Grid Color");
            if (ImGui.BeginPopupModal("Grid Color", ref wOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.ColorPicker3("###a", ref editCol);
                if (ImGui.Button("OK"))
                {
                    config.GridColor = new Color4(editCol.X, editCol.Y, editCol.Z, 1);
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();
                if (ImGui.Button("Default"))
                {
                    var def = Color4.CornflowerBlue;
                    editCol = new Vector3(def.R, def.G, def.B);
                    editGrad = false;
                }

                ImGui.EndPopup();
            }

            ImGui.AlignTextToFramePadding();
            ImGui.Text("Ui Scale (Requires Restart)");
            ImGui.SameLine();
            ImGui.SliderFloat("##uiscale", ref config.UiScale, 1, 2.5f);
            guiHelper.PauseWhenUnfocused = config.PauseWhenUnfocused;
        }

        void BlenderTab()
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Blender Path: ");
            ImGui.SameLine();
            string p = config.BlenderPath ?? "";
            ImGui.InputText("##blenderpath", ref p, 1024);
            if (p != config.BlenderPath)
                config.BlenderPath = p;
            ImGui.SameLine();
            if (ImGui.Button(".."))
                FileDialog.Open(path => config.BlenderPath = path);
            ImGui.TextDisabled("Leave blank to autodetect Blender");
        }

        void SystemViewerTab()
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Render Distance: ");
            ImGui.SameLine();
            ImGui.SliderFloat("##lodmultiplier", ref config.LodMultiplier, 1.0f, 8.0f);
        }

        public void Draw()
        {
            if (windowOpen)
            {
                ImGui.Begin("Options", ref windowOpen, ImGuiWindowFlags.AlwaysAutoResize);
                if (ImGui.BeginTabBar("##tabs", ImGuiTabBarFlags.NoCloseWithMiddleMouseButton))
                {
                    if (ImGui.BeginTabItem("Editor"))
                    {
                        EditorTab();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Blender"))
                    {
                        BlenderTab();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("System Viewer"))
                    {
                        SystemViewerTab();
                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }

                ImGui.End();
            }
        }
    }
}