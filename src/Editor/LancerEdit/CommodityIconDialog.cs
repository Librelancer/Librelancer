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
        private bool tmp;
        public CommodityIconDialog(MainWindow win)
        {
            this.win = win;
        }
        public void Open(string filename, string icoName = null, bool tmp = false)
        {
            iconName = icoName ?? Path.GetFileNameWithoutExtension(filename);
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
            this.tmp = tmp;
        }

        private static readonly string[] TYPES = {
            "Commodity", "Ship"
        };
        private static readonly IconType[] TYPE_Values = {
            IconType.Commodity, IconType.Ship
        };

        private int selType = 0;
        public void Draw()
        {
            if (doOpen)
            {
                ImGui.OpenPopup("New Icon");
                ImGui.SetNextWindowSize(new Vector2(275,288), ImGuiCond.FirstUseEver);
                doOpen = false;
            }
            bool pOpen = true;
            if (ImGui.BeginPopupModal("New Icon", ref pOpen, ImGuiWindowFlags.NoResize))
            {
                var w = ImGui.GetContentRegionAvail().X;
                ImGui.Dummy(new Vector2(w / 2 - 64 - 4, 1));
                ImGui.SameLine();
                bool dds = (loadType == TexLoadType.DDS);
                ImGui.Image((IntPtr)teximportid, new Vector2(128, 128),
                    new Vector2(0, dds ? 1 : 0), new Vector2(1, dds ? 0 : 1), Vector4.One, Vector4.Zero);
                ImGui.Text(string.Format("Dimensions: {0}x{1}", teximportprev.Width, teximportprev.Height));
                if (dds)
                {
                    ImGui.Text("Input file is .dds");
                } else
                    ImGui.Checkbox("Compress", ref compress);
                ImGui.Combo("Type", ref selType, TYPES, TYPES.Length);
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
                                utf = UiIconGenerator.Generate(TYPE_Values[selType], iconName, node, teximportprev.Format == SurfaceFormat.Dxt5);
                            }
                            else
                            {
                                utf = compress
                                    ? UiIconGenerator.CompressedFromFile(TYPE_Values[selType], iconName, texFilename, loadType == TexLoadType.Alpha)
                                    : UiIconGenerator.UncompressedFromFile(TYPE_Values[selType], iconName, texFilename, loadType == TexLoadType.Alpha);
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
            else {
                if (tmp) {
                    File.Delete(texFilename);
                    tmp = false;
                }
            }
        }
    }
}