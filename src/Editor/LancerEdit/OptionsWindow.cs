// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer;
using LibreLancer.ImUI;
using ImGuiNET;
using LibreLancer.ContentEdit;
using LibreLancer.Dialogs;
using LibreLancer.Graphics;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LancerEdit
{
    public class OptionsWindow
    {
        private MainWindow win;
        private EditorConfiguration config;
        private RenderContext rstate;
        private ImGuiHelper guiHelper;

        static readonly string[] defaultFilters =
        {
            "Linear", "Bilinear", "Trilinear"
        };

        int[] msaaLevels;
        string[] msaaStrings = {"None", "2x MSAA", "4x MSAA", "8x MSAA", "16x MSAA", "32x MSAA"};
        string[] updateChannels = { "Stable", "Daily" };
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

        private string autodetectedPath;
        private bool autodetectedValid;

        public OptionsWindow(MainWindow win)
        {
            this.win = win;
            config = win.Config;
            rstate = win.RenderContext;
            guiHelper = win.guiHelper;
            autodetectedPath = Blender.AutodetectBlender();
            autodetectedValid = Blender.BlenderPathValid();

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

        Vector4 editCol;
        Vector3 editCol2;
        private bool editGrad;

        void EditorTab()
        {
            if (ImGui.BeginCombo("Vertical Tab Style", config.TabStyle == 1 ? "Icons Only" : "Icons and Text"))
            {
                if (ImGui.Selectable("Icons and Text"))
                    config.TabStyle = 0;
                if (ImGui.Selectable("Icons Only"))
                    config.TabStyle = 1;
                ImGui.EndCombo();
            }
            ImGuiExt.DropdownButton("Default Camera", ref config.DefaultCameraMode, camModesNormal);
            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Default Camera");
            ImGuiExt.DropdownButton("Default View", ref config.DefaultRenderMode, ModelViewer.ViewModes);
            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Default View");
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
                editCol = config.Background;
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

                ImGui.ColorPicker4(editGrad ? "Top###a" : "###a", ref editCol, ImGuiColorEditFlags.NoAlpha);
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
                    editCol = def;
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
                editCol = config.GridColor;
            }

            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Grid Color");
            wOpen = true;
            if (ImGui.BeginPopupModal("Grid Color",  ref wOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.ColorPicker4("###a", ref editCol, ImGuiColorEditFlags.AlphaBar);
                if (ImGui.Button("OK"))
                {
                    config.GridColor = new Color4(editCol.X, editCol.Y, editCol.Z, editCol.W);
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();
                if (ImGui.Button("Default"))
                {
                    var def = Color4.CornflowerBlue;
                    editCol = new Vector4(def.R, def.G, def.B, 1) * new Vector4(0.5f, 0.5f, 0.5f, 1);
                    editGrad = false;
                }

                ImGui.EndPopup();
            }

            ImGui.AlignTextToFramePadding();
            ImGui.Text("Ui Scale");
            ImGui.SameLine();
            ImGui.SliderFloat("##uiscale", ref config.UiScale, 1, 2.5f);
            if (!ImGui.IsItemActive())
            {
                ImGuiHelper.UserScale = config.UiScale;
            }
            if (Platform.RunningOS == OS.Windows && ImGui.Button("Set File Assocations"))
            {
                Win32Integration.FileTypes();
            }

            ImGui.AlignTextToFramePadding();
            ImGui.Text("Auto Load Path");
            ImGui.SameLine();
            ImGui.InputText("##autoloadPath", ref config.AutoLoadPath, 255, ImGuiInputTextFlags.ReadOnly);
            ImGui.SameLine();
            if (ImGui.Button("Browse##autoloadPath"))
            {
                FileDialog.ChooseFolder((path) =>
                {
                    if (!GameConfig.CheckFLDirectory(path))
                    {
                        win.ErrorDialog("The provided path was not a valid Freelancer installation.");
                        return;
                    }

                    config.AutoLoadPath = path;
                });
            }
            ImGui.SameLine();
            if (ImGui.Button($"{Icons.TrashAlt}"))
                config.AutoLoadPath = "";

            guiHelper.PauseWhenUnfocused = config.PauseWhenUnfocused;
        }

        void ImportExportTab()
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
            ImGui.BeginDisabled(true);
            if (!string.IsNullOrWhiteSpace(autodetectedPath)) {
                ImGui.Text($"Blender was detected at '{autodetectedPath}'");
                if (!string.IsNullOrWhiteSpace(config.BlenderPath)) {
                    ImGui.Text("But importer will use specified path");
                }
                if (!autodetectedValid) {
                    ImGui.TextColored(Color4.DarkRed, "Autodetect Blender test run failed");
                }
            }
            else
            {
                ImGui.Text("Blender was not autodetected");
            }
            ImGui.EndDisabled();
            ImGui.Checkbox("Enable Collada (Not recommended)", ref config.ColladaVisible);
            ImGui.SetItemTooltip("Collada is not well supported by modelling programs, use GLB when possible");
        }

        void SystemViewerTab()
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Render Distance: ");
            ImGui.SameLine();
            ImGui.SliderFloat("##lodmultiplier", ref config.LodMultiplier, 1.0f, 8.0f);
            ImGuiExt.DropdownButton("Default Camera", ref config.DefaultSysEditCameraMode, camModesNormal);
            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Default Camera");
        }

        void UpdateTab()
        {
            int index = 0;
            for (int i = 0; i < updateChannels.Length; i++) {
                if (updateChannels[i].Equals(config.UpdateChannel, StringComparison.OrdinalIgnoreCase)) {
                    index = i;
                    break;
                }
            }
            ImGui.Combo("Channel", ref index, updateChannels, updateChannels.Length);
            config.UpdateChannel = updateChannels[index].ToLowerInvariant();
            win.Updater.Config.Channel = config.UpdateChannel;
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

                    if (ImGui.BeginTabItem("Import/Export"))
                    {
                        ImportExportTab();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("System Viewer"))
                    {
                        SystemViewerTab();
                        ImGui.EndTabItem();
                    }

                    if (win.Updater.Enabled && ImGui.BeginTabItem("Updates"))
                    {
                        UpdateTab();
                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }

                ImGui.End();
            }
        }
    }
}
