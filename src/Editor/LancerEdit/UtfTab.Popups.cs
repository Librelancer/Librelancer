using System;
using System.Collections.Generic;
using ImGuiNET;
using LibreLancer;
namespace LancerEdit
{
    public partial class UtfTab
    {
        void RegisterPopups()
        {
            popups.AddPopup("Texture Import", TexImportDialog);
            popups.AddPopup("Confirm?##stringedit", StringConfirm, WindowFlags.AlwaysAutoResize);
            popups.AddPopup("String Editor", StringEditor, WindowFlags.AlwaysAutoResize);
            popups.AddPopup("Hex Editor", HexEditor);
            popups.AddPopup("Color Picker", ColorPicker, WindowFlags.AlwaysAutoResize);
            popups.AddPopup("New Node", AddPopup, WindowFlags.AlwaysAutoResize);
            popups.AddPopup("Rename Node", Rename, WindowFlags.AlwaysAutoResize);
        }

        string teximportpath = "";
        Texture2D teximportprev;
        int teximportid;
        volatile bool texImportWaiting = false;
        byte[] texImportData;
        string[] texOptions = new string[] {
            "Uncompressed",
            "DXT1",
            "DXT1a",
            "DXT3",
            "DXT5"
        };
        int compressOption = 0;
        bool compressSlow = false;
        void TexImportDialog(PopupData data)
        {
            if (teximportprev == null)
            { //processing
                ImGui.Text("Processing...");
                if (!texImportWaiting)
                {
                    selectedNode.Children = null;
                    selectedNode.Data = texImportData;
                    texImportData = null;
                    ImGui.CloseCurrentPopup();
                }
            }
            else
            {
                ImGui.Image((IntPtr)teximportid, new Vector2(64, 64),
                            new Vector2(0, 1), new Vector2(1, 0), Vector4.One, Vector4.Zero);
                ImGui.Text(string.Format("Dimensions: {0}x{1}", teximportprev.Width, teximportprev.Height));
                ImGui.Combo("Format", ref compressOption, texOptions);
                ImGui.Checkbox("High Quality (slow)", ref compressSlow);
                if (ImGui.Button("Import"))
                {
                    ImGuiHelper.DeregisterTexture(teximportprev);
                    teximportprev.Dispose();
                    teximportprev = null;
                    texImportWaiting = true;
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
                        }
                        texImportData = TextureImport.CreateDDS(teximportpath, format, compressSlow);
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
            ImGui.InputText("", text.Pointer, 255, InputTextFlags.Default, text.Callback);
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
            ImGui.PushFont(ImGuiHelper.Default);
            int res;
            if ((res = mem.Draw("Hex", hexdata, hexdata.Length, 0)) != 0)
            {
                if (res == 1) selectedNode.Data = hexdata;
                ImGui.CloseCurrentPopup();
            }
            ImGui.PopFont();
        }

        bool pickcolor4 = false;
        System.Numerics.Vector4 color4;
        System.Numerics.Vector3 color3;
        unsafe void ColorPicker(PopupData data)
        {
            bool old4 = pickcolor4;
            ImGui.Checkbox("Alpha?", ref pickcolor4);
            if (old4 != pickcolor4)
            {
                if (old4 == false) color4 = new System.Numerics.Vector4(color3.X, color3.Y, color3.Z, 1);
                if (old4 == true) color3 = new System.Numerics.Vector3(color4.X, color4.Y, color4.Z);
            }
            ImGui.Separator();
            if (pickcolor4)
                ImGui.ColorPicker4("Color", ref color4, ColorEditFlags.AlphaPreview | ColorEditFlags.AlphaBar);
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
                        var f = (System.Numerics.Vector4*)ptr;
                        f[0] = color4;
                    }
                    selectedNode.Data = bytes;
                }
                else
                {
                    var bytes = new byte[12];
                    fixed (byte* ptr = bytes)
                    {
                        var f = (System.Numerics.Vector3*)ptr;
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
            bool entered = ImGui.InputText("", text.Pointer, text.Size, InputTextFlags.EnterReturnsTrue, text.Callback);
            if (data.First) ImGui.SetKeyboardFocusHere();
            if (entered || ImGui.Button("Ok"))
            {
                var n = text.GetText().Trim();
                if (n.Length == 0)
                    ErrorPopup("Node name cannot be empty");
                else
                    renameNode.Name = text.GetText();
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
            bool entered = ImGui.InputText("", text.Pointer, text.Size, InputTextFlags.EnterReturnsTrue, text.Callback);
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
            if (ImGui.BeginPopupModal("Error##" + Unique, WindowFlags.AlwaysAutoResize))
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
            if (ImGui.BeginPopupModal("Confirm?##generic" + Unique, WindowFlags.AlwaysAutoResize))
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
