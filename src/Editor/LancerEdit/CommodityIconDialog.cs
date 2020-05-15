// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.ImUI;

namespace LancerEdit
{
    public class CommodityIconDialog
    {
        private string texFilename;
        private string iconName = "";
        private Texture2D teximportprev;
        private int teximportid;
        private bool compress = false;
        private bool error = false;
        private MainWindow win;
        private bool doOpen = false;
        private TexLoadType loadType;
        public CommodityIconDialog(MainWindow win)
        {
            this.win = win;
        }
        public void Open(string filename)
        {
            iconName = Path.GetFileNameWithoutExtension(filename);
            texFilename = filename;
            error = false;
            if (teximportprev != null)
            {
                ImGuiHelper.DeregisterTexture(teximportprev);
                teximportprev.Dispose();
                teximportprev = null;
            }
            var src = TextureImport.OpenFile(filename);
            loadType = src.Type;
            teximportprev = src.Texture;
            if (loadType == TexLoadType.ErrorLoad ||
                loadType == TexLoadType.ErrorNonSquare ||
                loadType == TexLoadType.ErrorNonPowerOfTwo)
            {
                win.ErrorDialog(TextureImport.LoadErrorString(loadType, filename));
                return;
            }
            teximportid = ImGuiHelper.RegisterTexture(teximportprev);
            doOpen = true;
        }

        public void Draw()
        {
            if (doOpen)
            {
                ImGui.OpenPopup("New Commodity Icon");
                ImGui.SetNextWindowSize(new Vector2(275,275), ImGuiCond.FirstUseEver);
                doOpen = false;
            }
            bool pOpen = true;
            if (ImGui.BeginPopupModal("New Commodity Icon", ref pOpen, ImGuiWindowFlags.NoResize))
            {
                var w = ImGui.GetWindowContentRegionWidth();
                ImGui.Dummy(new Vector2(w / 2 - 64 - 4, 1));
                ImGui.SameLine();
                ImGui.Image((IntPtr)teximportid, new Vector2(128, 128),
                    new Vector2(0, 0), new Vector2(1, 1), Vector4.One, Vector4.Zero);
                ImGui.Text(string.Format("Dimensions: {0}x{1}", teximportprev.Width, teximportprev.Height));
                if (loadType == TexLoadType.DDS)
                {
                    ImGui.Text("Input file is .dds");
                } else
                    ImGui.Checkbox("Compress", ref compress);
                ImGui.InputText("Icon Name", ref iconName, 128);
                if(error)
                    ImGui.TextColored(Color4.Red, "Icon name must not be empty");
                if (ImGui.Button("Create"))
                {
                    if (string.IsNullOrWhiteSpace(iconName))
                    {
                        error = true;
                    }
                    else
                    {
                        new Thread(() =>
                        {
                            win.StartLoadingSpinner();
                            EditableUtf utf;
                            if (loadType == TexLoadType.DDS)
                            {
                                var node = new LUtfNode() { Children = new List<LUtfNode>()};
                                node.Children.Add(new LUtfNode()
                                    {Name = "MIPS", Parent = node, Data = File.ReadAllBytes(texFilename)});
                                utf = UiIconGenerator.Generate(iconName, node, teximportprev.Format == SurfaceFormat.Dxt5);
                            }
                            else
                            {
                                utf = compress
                                    ? UiIconGenerator.CompressedFromFile(iconName, texFilename, loadType == TexLoadType.Alpha)
                                    : UiIconGenerator.UncompressedFromFile(iconName, texFilename, loadType == TexLoadType.Alpha);
                            }
                            win.QueueUIThread(() =>
                            {
                                win.tabs.Add(new UtfTab(win, utf, $"{iconName}.3db", true));
                            });
                            win.FinishLoadingSpinner();
                        }).Start();
                        ImGui.CloseCurrentPopup();
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
        }
    }
}