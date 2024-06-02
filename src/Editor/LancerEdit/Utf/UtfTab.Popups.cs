// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.Graphics;
using LibreLancer.ImUI;
namespace LancerEdit
{
    public partial class UtfTab
    {
        void RegisterPopups()
        {
            popups.AddPopup("Texture Import", TexImportDialog);
            popups.AddPopup("Confirm?##stringedit", StringConfirm, ImGuiWindowFlags.AlwaysAutoResize);
            popups.AddPopup("String Editor", StringEditor, ImGuiWindowFlags.AlwaysAutoResize);
            popups.AddPopup("Hex Editor", HexEditor, 0, false, new Vector2(300,200) * ImGuiHelper.Scale);
            popups.AddPopup("Color Picker", ColorPicker, ImGuiWindowFlags.AlwaysAutoResize);
            popups.AddPopup("New Node", AddPopup, ImGuiWindowFlags.AlwaysAutoResize);
            popups.AddPopup("Rename Node", Rename, ImGuiWindowFlags.AlwaysAutoResize);
        }

        string teximportpath = "";
        private string teximportwarn = "";
        Texture2D teximportprev;
        int teximportid;
        volatile bool texImportWaiting = false;
        byte[] texImportData;
        List<LUtfNode> texImportChildren;
        string[] texOptions = new string[] {
            "Uncompressed",
            "DXT1",
            "DXT1a",
            "DXT3",
            "DXT5",
            "Normal Map",
            "Metallic Map (B channel)",
            "Roughness Map (G channel)"
        };
        int compressOption = 0;
        string[] mipmapOptions = new string[] {
            "None",
            "Box",
            "Tent",
            "Lanczos4",
            "Mitchell",
            "Kaiser",
        };
        int mipmapOption = 3;
        bool compressSlow = false;
        bool texFlip = false;
        void TexImportDialog(PopupData data)
        {
            if (teximportprev == null)
            { //processing
                ImGuiExt.Spinner("##spinner", 10, 2, ImGui.GetColorU32(ImGuiCol.ButtonHovered, 1));
                ImGui.SameLine();
                ImGui.Text("Processing...");
                if (!texImportWaiting)
                {
                    if (texImportChildren != null)
                    {
                        selectedNode.Data = null;
                        foreach (var c in texImportChildren)
                            c.Parent = selectedNode;
                        selectedNode.Children = texImportChildren;
                    }
                    else
                    {
                        selectedNode.Children = null;
                        selectedNode.Data = texImportData;
                    }
                    texImportData = null;
                    texImportChildren = null;
                    ImGui.CloseCurrentPopup();
                }
            }
            else
            {
                data.NoClose = false;
                ImGui.Image((IntPtr)teximportid, new Vector2(64, 64),
                            new Vector2(0, 1), new Vector2(1, 0), Vector4.One, Vector4.Zero);
                ImGui.Text(string.Format("Dimensions: {0}x{1}", teximportprev.Width, teximportprev.Height));
                if (!string.IsNullOrWhiteSpace(teximportwarn))
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Color4.Orange);
                    ImGui.TextWrapped(teximportwarn);
                    ImGui.PopStyleColor();
                }
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Format");
                ImGui.SameLine();
                ImGui.Combo("##format", ref compressOption, texOptions, texOptions.Length);
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Mipmaps");
                ImGui.SameLine();
                ImGui.Combo("##mipmaps", ref mipmapOption, mipmapOptions, mipmapOptions.Length);
                ImGui.Checkbox("Flip Vertically", ref texFlip);
                ImGui.Checkbox("High Quality (slow)", ref compressSlow);
                if (ImGui.Button("Import"))
                {
                    ImGuiHelper.DeregisterTexture(teximportprev);
                    teximportprev.Dispose();
                    teximportprev = null;
                    texImportWaiting = true;
                    data.NoClose = true;
                    new System.Threading.Thread(() =>
                    {
                        var format = DDSFormat.Uncompressed;
                        switch (compressOption)
                        {
                            case 1:
                                format = DDSFormat.DXT1;
                                break;
                            case 2:
                                format = DDSFormat.DXT1a;
                                break;
                            case 3:
                                format = DDSFormat.DXT3;
                                break;
                            case 4:
                                format = DDSFormat.DXT5;
                                break;
                            case 5:
                                format = DDSFormat.RGTC2;
                                break;
                            case 6:
                                format = DDSFormat.MetallicRGTC1;
                                break;
                            case 7:
                                format = DDSFormat.RoughnessRGTC1;
                                break;
                        }
                        var mipm = MipmapMethod.None;
                        switch(mipmapOption) {
                            case 1:
                                mipm = MipmapMethod.Box;
                                break;
                            case 2:
                                mipm = MipmapMethod.Tent;
                                break;
                            case 3:
                                mipm = MipmapMethod.Lanczos4;
                                break;
                            case 4:
                                mipm = MipmapMethod.Mitchell;
                                break;
                            case 5:
                                mipm = MipmapMethod.Kaiser;
                                break;
                        }
                        if (mipm == MipmapMethod.None && format == DDSFormat.Uncompressed)
                            texImportData = TextureImport.TGANoMipmap(teximportpath, texFlip);
                        else if (format == DDSFormat.Uncompressed)
                            texImportChildren = TextureImport.TGAMipmaps(teximportpath, mipm, texFlip);
                        else
                            texImportData = TextureImport.CreateDDS(teximportpath, format, mipm, compressSlow, texFlip);
                        texImportWaiting = false;
                    }).Start();
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    ImGuiHelper.DeregisterTexture(teximportprev);
                    teximportprev.Dispose();
                    teximportprev = null;
                    ImGui.CloseCurrentPopup();
                }
            }
        }

        void StringConfirm(PopupData data)
        {
            ImGui.Text("Data is >255 bytes, string will be truncated. Continue?");
            if (ImGui.Button("Yes"))
            {
                text.SetBytes(selectedNode.Data, 255);
                popups.OpenPopup("String Editor");
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("No")) ImGui.CloseCurrentPopup();
        }

        void StringEditor(PopupData data)
        {
            ImGui.Text("String: ");
            ImGui.SameLine();
            text.InputText("##str", ImGuiInputTextFlags.None, 255);
            if (ImGui.Button("Ok"))
            {
                selectedNode.Data = text.GetByteArray();
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel")) ImGui.CloseCurrentPopup();
        }

        MemoryEditor mem;
        byte[] hexdata;
        void HexEditor(PopupData data)
        {
            ImGui.SameLine(ImGui.GetWindowWidth() - 90 * ImGuiHelper.Scale);
            if (ImGui.Button("Ok"))
            {
                selectedNode.Data = hexdata;
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine(ImGui.GetWindowWidth() - 60 * ImGuiHelper.Scale);
            if (ImGui.Button("Cancel")) ImGui.CloseCurrentPopup();
            ImGui.PushFont(ImGuiHelper.Default);
            mem.DrawContents(hexdata, hexdata.Length);
            ImGui.PopFont();
        }

        bool pickcolor4 = false;
        Vector4 color4;
        Vector3 color3;
        unsafe void ColorPicker(PopupData data)
        {
            bool old4 = pickcolor4;
            ImGui.Checkbox("Alpha?", ref pickcolor4);
            if (old4 != pickcolor4)
            {
                if (old4 == false) color4 = new Vector4(color3.X, color3.Y, color3.Z, 1);
                if (old4 == true) color3 = new Vector3(color4.X, color4.Y, color4.Z);
            }
            ImGui.Separator();
            if (pickcolor4)
                ImGui.ColorPicker4("Color", ref color4, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
            else
                ImGui.ColorPicker3("Color", ref color3);
            ImGui.Separator();
            if (ImGui.Button("Ok"))
            {
                ImGui.CloseCurrentPopup();
                if (pickcolor4)
                {
                    var bytes = new byte[16];
                    fixed (byte* ptr = bytes)
                    {
                        var f = (Vector4*)ptr;
                        f[0] = color4;
                    }
                    selectedNode.Data = bytes;
                }
                else
                {
                    var bytes = new byte[12];
                    fixed (byte* ptr = bytes)
                    {
                        var f = (Vector3*)ptr;
                        f[0] = color3;
                    }
                    selectedNode.Data = bytes;
                }
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                ImGui.CloseCurrentPopup();
            }
        }

        void Rename(PopupData data)
        {
            ImGui.Text("Name: ");
            ImGui.SameLine();
            bool entered = ImGui.InputText("##name", text.Pointer, (uint)text.Size, ImGuiInputTextFlags.EnterReturnsTrue, text.Callback);
            if (data.DoFocus) ImGui.SetKeyboardFocusHere();
            if (entered || ImGui.Button("Ok"))
            {
                var n = text.GetText().Trim();
                if (n.Length == 0)
                    ErrorPopup("Node name cannot be empty");
                else  {
                    renameNode.Name = text.GetText();
                    renameNode.ResolvedName = null;
                }
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                ImGui.CloseCurrentPopup();
            }
        }

        void AddPopup(PopupData data)
        {
            ImGui.Text("Name: ");
            ImGui.SameLine();
            bool entered = ImGui.InputText("##name", text.Pointer, (uint)text.Size, ImGuiInputTextFlags.EnterReturnsTrue, text.Callback);
            if (data.DoFocus) ImGui.SetKeyboardFocusHere();
            if (entered || ImGui.Button("Ok"))
            {
                var node = new LUtfNode() { Name = text.GetText().Trim(), Parent = addParent ?? addNode };
                if (node.Name.Length == 0)
                {
                    ErrorPopup("Node name cannot be empty");
                }
                else
                {
                    if (addParent != null)
                        addParent.Children.Insert(addParent.Children.IndexOf(addNode) + addOffset, node);
                    else
                    {
                        addNode.Data = null;
                        if (addNode.Children == null) addNode.Children = new List<LUtfNode>();
                        addNode.Children.Add(node);
                    }
                    selectedNode = node;
                }
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                ImGui.CloseCurrentPopup();
            }
        }

        string confirmText;
        bool doConfirm = false;
        Action confirmAction;

        void Confirm(string text, Action action)
        {
            doConfirm = true;
            confirmAction = action;
            confirmText = text;
        }


        bool doError = false;
        string errorText;
        void ErrorPopup(string error)
        {
            errorText = error;
            doError = true;
        }

        bool floatEditor = false;
        float[] floats;
        bool intEditor = false;
        int[] ints;
        bool intHex = false;

        void Popups()
        {
            popups.Run();
            //Float Editor
            if (floatEditor)
            {
                ImGui.OpenPopup("Float Editor##" + Unique);
                floatEditor = false;
            }
            DataEditors.FloatEditor("Float Editor##" + Unique, ref floats, selectedNode);
            if (intEditor)
            {
                ImGui.OpenPopup("Int Editor##" + Unique);
                intEditor = false;
            }
            DataEditors.IntEditor("Int Editor##" + Unique, ref ints, ref intHex, selectedNode);
            //Error
            if (doError)
            {
                ImGui.OpenPopup("Error##" + Unique);
                doError = false;
            }
            bool wOpen = true;

            if (ImGui.BeginPopupModal("Error##" + Unique, ref wOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text(errorText);
                if (ImGui.Button("Ok")) ImGui.CloseCurrentPopup();
                ImGui.EndPopup();
            }
            //Confirmation
            if (doConfirm)
            {
                ImGui.OpenPopup("Confirm?##generic" + Unique);
                doConfirm = false;
            }
            wOpen = true;
            if (ImGui.BeginPopupModal("Confirm?##generic" + Unique, ref wOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text(confirmText);
                if (ImGui.Button("Yes"))
                {
                    confirmAction();
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button("No")) ImGui.CloseCurrentPopup();
                ImGui.EndPopup();
            }
        }
    }
}
