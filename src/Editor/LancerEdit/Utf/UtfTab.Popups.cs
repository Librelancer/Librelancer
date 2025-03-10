﻿// MIT License - Copyright (c) Callum McGing
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
            popups.AddPopup("Confirm?##stringedit", StringConfirm, ImGuiWindowFlags.AlwaysAutoResize);
            popups.AddPopup("String Editor", StringEditor, ImGuiWindowFlags.AlwaysAutoResize);
            popups.AddPopup("Hex Editor", HexEditor, 0, false, new Vector2(300,200) * ImGuiHelper.Scale);
            popups.AddPopup("Color Picker", ColorPicker, ImGuiWindowFlags.AlwaysAutoResize);
            popups.AddPopup("New Node", AddPopup, ImGuiWindowFlags.AlwaysAutoResize);
            popups.AddPopup("Rename Node", Rename, ImGuiWindowFlags.AlwaysAutoResize);
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
