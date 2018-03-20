/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.Collections.Generic;
using System.IO;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Utf.Ale;
namespace LancerEdit
{
    public class UtfTab : DockTab
    {
        bool open = true;
        public EditableUtf Utf;
        LUtfNode selectedNode = null;
        MainWindow main;
        public UtfTab(MainWindow main, EditableUtf utf, string title)
        {
            this.main = main;
            Utf = utf;
            Title = title;
            text = new TextBuffer();
            main.Resources.AddResources(utf.Export(), Unique.ToString());
        }
        public override void SetActiveTab(MainWindow win)
        {
            win.ActiveTab = this;
        }
        MemoryEditor mem;
        byte[] hexdata;
        bool hexEditor = false;
        TreeNodeFlags tflags = TreeNodeFlags.OpenOnArrow | TreeNodeFlags.OpenOnDoubleClick;
        TextBuffer text;

        public override void Dispose()
        {
            text.Dispose();
            main.Resources.RemoveResourcesForId(Unique.ToString());
        }

        bool HasChild(LUtfNode node, string name)
        {
            if (node.Children == null) return false;
            foreach (var child in node.Children)
                if (child.Name == name) return true;
            return false;
        }

        public string GetUtfPath()
        {
            if (selectedNode == null) return "None";
            List<string> strings = new List<string>();
            LUtfNode node = selectedNode;
            while (node != Utf.Root)
            {
                strings.Add(node.Name);
                node = node.Parent;
            }
            strings.Reverse();
            var path = "/" + string.Join("/", strings);
            return path;
        }

        public override bool Draw()
        {
            //Child Window
            var size = ImGui.GetWindowSize();
            ImGui.BeginChild("##utfchild", new Vector2(size.X - 15, size.Y - 50), false, 0);
            //Layout
            if (selectedNode != null)
            {
                ImGui.Columns(2, "NodeColumns", true);
            }
            //Headers
            ImGui.Separator();
            ImGui.Text("Nodes");
            if (selectedNode != null)
            {
                ImGui.NextColumn();
                ImGui.Text("Node Information");
                ImGui.NextColumn();
            }
            ImGui.Separator();
            //Tree
            ImGui.BeginChild("##scroll", false, 0);
            var flags = selectedNode == Utf.Root ? TreeNodeFlags.Selected | tflags : tflags;
            var isOpen = ImGui.TreeNodeEx("/", flags);
            if (ImGuiNative.igIsItemClicked(0))
            {
                selectedNode = Utf.Root;
            }
            ImGui.PushID("/##ROOT");
            DoNodeMenu("/##ROOT", Utf.Root, null);
            ImGui.PopID();
            if (isOpen)
            {
                for (int i = 0; i < Utf.Root.Children.Count; i++)
                {
                    DoNode(Utf.Root.Children[i], Utf.Root, i);
                }
                ImGui.TreePop();
            }
            ImGui.EndChild();
            //End Tree
            if (selectedNode != null)
            {
                //Node preview
                ImGui.NextColumn();
                NodeInformation();
            }
            //Action Bar
            ImGui.EndChild();
            ImGui.Separator();
            if (ImGui.Button("Actions"))
                ImGui.OpenPopup("actions");
            if (ImGui.BeginPopup("actions"))
            {
                if (ImGui.MenuItem("View Model"))
                {
                    IDrawable drawable = null;
                    try
                    {
                        drawable = LibreLancer.Utf.UtfLoader.GetDrawable(Utf.Export(), main.Resources);
                        drawable.Initialize(main.Resources);
                    }
                    catch (Exception) { ErrorPopup("Could not open as model"); drawable = null; }
                    if (drawable != null)
                    {
                        main.AddTab(new ModelViewer("Model Viewer (" + Title + ")", Title, drawable, main, this));
                    }
                }
                if (ImGui.MenuItem("View Ale"))
                {
                    AleFile ale = null;
                    try
                    {
                        ale = new AleFile(Utf.Export());
                    }
                    catch (Exception)
                    {
                        ErrorPopup("Could not open as ale");
                        ale = null;
                    }
                    if (ale != null)
                        main.AddTab(new AleViewer("Ale Viewer (" + Title + ")", Title, ale, main));
                }
                if (ImGui.MenuItem("Refresh Resources"))
                {
                    main.Resources.RemoveResourcesForId(Unique.ToString());
                    main.Resources.AddResources(Utf.Export(), Unique.ToString());
                }
                ImGui.EndPopup();
            }
            Popups();
            return open;
        }

        bool doError = false;
        string errorText;
        void ErrorPopup(string error)
        {
            errorText = error;
            doError = true;
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

        unsafe int DummyCallback(TextEditCallbackData* data)
        {
            return 0;
        }

        unsafe void NodeInformation()
        {
            ImGui.BeginChild("##scrollnode", false, 0);
            ImGui.Text("Name: " + selectedNode.Name);
            if (selectedNode.Children != null)
            {
                ImGui.Text(selectedNode.Children.Count + " children");
                if (selectedNode != Utf.Root)
                {
                    ImGui.Separator();
                    ImGui.Text("Actions:");
                    if (ImGui.Button("Add Data"))
                    {
                        Confirm("Adding data will delete this node's children. Continue?", () =>
                        {
                            selectedNode.Children = null;
                            selectedNode.Data = new byte[0];
                        });
                    }
                    if (ImGui.Button("Import Data"))
                        ImGui.OpenPopup("importactions");
                    if (ImGui.BeginPopup("importactions"))
                    {
                        if (ImGui.MenuItem("File"))
                        {
                            Confirm("Importing data will delete this node's children. Continue?", () =>
                            {
                                string path;
                                if ((path = FileDialog.Open()) != null)
                                {
                                    selectedNode.Children = null;
                                    selectedNode.Data = File.ReadAllBytes(path);
                                }
                            });
                        }
                        if (ImGui.MenuItem("Texture"))
                            Confirm("Importing data will delete this node's children. Continue?", () =>
                            {
                                ImportTexture();
                            });
                        ImGui.EndPopup();
                    }
                }
            }
            else if (selectedNode.Data != null)
            {
                ImGui.Text(string.Format("Size: {0}", LibreLancer.DebugDrawing.SizeSuffix(selectedNode.Data.Length)));
                ImGui.Separator();
                if (selectedNode.Data.Length > 0)
                {
                    ImGui.Text("Previews:");
                    //String Preview
                    ImGui.Text("String:");
                    ImGui.SameLine();
                    ImGui.InputText("", selectedNode.Data, (uint)Math.Min(selectedNode.Data.Length, 32), InputTextFlags.ReadOnly, DummyCallback);
                    //Float Preview
                    ImGui.Text("Float:");
                    fixed (byte* ptr = selectedNode.Data)
                    {
                        for (int i = 0; i < 4 && (i < selectedNode.Data.Length / 4); i++)
                        {
                            ImGui.SameLine();
                            ImGui.Text(((float*)ptr)[i].ToString());
                        }
                    }
                    //Int Preview
                    ImGui.Text("Int:");
                    fixed (byte* ptr = selectedNode.Data)
                    {
                        for (int i = 0; i < 4 && (i < selectedNode.Data.Length / 4); i++)
                        {
                            ImGui.SameLine();
                            ImGui.Text(((int*)ptr)[i].ToString());
                        }
                    }
                }
                else
                {
                    ImGui.Text("Empty Data");
                }
                ImGui.Separator();
                ImGui.Text("Actions:");
                if (ImGui.Button("Edit"))
                {
                    ImGui.OpenPopup("editactions");
                }
                if (ImGui.BeginPopup("editactions"))
                {
                    if (ImGui.MenuItem("String Editor"))
                    {
                        if (selectedNode.Data.Length > 255)
                            stringConfirm = true;
                        else
                        {
                            text.SetBytes(selectedNode.Data, selectedNode.Data.Length);
                            stringEditor = true;
                        }
                    }
                    if (ImGui.MenuItem("Hex Editor"))
                    {
                        hexdata = new byte[selectedNode.Data.Length];
                        selectedNode.Data.CopyTo(hexdata, 0);
                        mem = new MemoryEditor();
                        hexEditor = true;
                    }
                    if (ImGui.MenuItem("Float Editor"))
                    {
                        floats = new float[selectedNode.Data.Length / 4];
                        for (int i = 0; i < selectedNode.Data.Length / 4; i++)
                        {
                            floats[i] = BitConverter.ToSingle(selectedNode.Data, i * 4);
                        }
                        floatEditor = true;
                    }
                    if (ImGui.MenuItem("Int Editor"))
                    {
                        ints = new int[selectedNode.Data.Length / 4];
                        for (int i = 0; i < selectedNode.Data.Length / 4; i++)
                        {
                            ints[i] = BitConverter.ToInt32(selectedNode.Data, i * 4);
                        }
                        intEditor = true;
                    }
                    if (ImGui.MenuItem("Color Picker"))
                    {
                        var len = selectedNode.Data.Length / 4;
                        if (len < 3)
                        {
                            pickcolor4 = true;
                            color4 = new System.Numerics.Vector4(0, 0, 0, 1);
                        }
                        else if (len == 3)
                        {
                            pickcolor4 = false;
                            color3 = new System.Numerics.Vector3(
                                BitConverter.ToSingle(selectedNode.Data, 0),
                                BitConverter.ToSingle(selectedNode.Data, 4),
                                BitConverter.ToSingle(selectedNode.Data, 8));
                        }
                        else if (len > 3)
                        {
                            pickcolor4 = true;
                            color4 = new System.Numerics.Vector4(
                                BitConverter.ToSingle(selectedNode.Data, 0),
                                BitConverter.ToSingle(selectedNode.Data, 4),
                                BitConverter.ToSingle(selectedNode.Data, 8),
                                BitConverter.ToSingle(selectedNode.Data, 12));
                        }
                        colorPicker = true;
                    }
                    ImGui.EndPopup();
                }
                ImGui.NextColumn();
                if (ImGui.Button("Texture Viewer"))
                {
                    Texture2D tex = null;
                    try
                    {
                        using (var stream = new MemoryStream(selectedNode.Data))
                        {
                            tex = LibreLancer.ImageLib.Generic.FromStream(stream);
                        }
                        var title = string.Format("{0} ({1})", selectedNode.Name, Title);
                        var tab = new TextureViewer(title, tex);
                        main.AddTab(tab);
                    }
                    catch (Exception)
                    {
                        ErrorPopup("Node data couldn't be opened as texture");
                    }
                }
                if (ImGui.Button("Play Audio"))
                {
                    var data = main.Audio.AllocateData();
                    using (var stream = new MemoryStream(selectedNode.Data))
                    {
                        main.Audio.PlaySound(stream);
                    }
                }
                if (ImGui.Button("Import Data"))
                    ImGui.OpenPopup("importactions");
                if (ImGui.BeginPopup("importactions"))
                {
                    if (ImGui.MenuItem("File"))
                    {
                        string path;
                        if ((path = FileDialog.Open()) != null)
                        {
                            selectedNode.Data = File.ReadAllBytes(path);
                        }
                    }
                    if (ImGui.MenuItem("Texture"))
                        ImportTexture();
                    ImGui.EndPopup();
                }
                if (ImGui.Button("Export Data"))
                {
                    string path;
                    if ((path = FileDialog.Save()) != null)
                    {
                        File.WriteAllBytes(path, selectedNode.Data);
                    }
                }
            }
            else
            {
                ImGui.Text("Empty");
                ImGui.Separator();
                ImGui.Text("Actions:");
                if (ImGui.Button("Add Data"))
                {
                    selectedNode.Data = new byte[0];
                }
                if (ImGui.Button("Import Data"))
                    ImGui.OpenPopup("importactions");
                if(ImGui.BeginPopup("importactions"))
                {
                    if(ImGui.MenuItem("File")) {
                        string path;
                        if ((path = FileDialog.Open()) != null)
                        {
                            selectedNode.Data = File.ReadAllBytes(path);
                        }
                    }
                    if(ImGui.MenuItem("Texture"))
                        ImportTexture();
                    ImGui.EndPopup();
                }
            }
            ImGui.EndChild();
        }

        void ImportTexture()
        {
            string path;
            if ((path = FileDialog.Open()) != null)
            {
                bool isDDS;
                using (var stream = File.OpenRead(path))
                {
                    isDDS = LibreLancer.ImageLib.DDS.StreamIsDDS(stream);
                }
                if(isDDS) {
                    selectedNode.Children = null;
                    selectedNode.Data = File.ReadAllBytes(path);
                } else {
                    try
                    {
                        teximportprev = LibreLancer.ImageLib.Generic.FromFile(path);
                        teximportpath = path;
                        teximportid = ImGuiHelper.RegisterTexture(teximportprev);
                        openTexImport = true;
                    }
                    catch (Exception)
                    {
                        ErrorPopup("Could not open file as image");
                    }
                }
            }
        }
        string teximportpath = "";
        Texture2D teximportprev;
        int teximportid;
        bool openTexImport = false;
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
        void TexImportDialog()
        {
            if(teximportprev == null) { //processing
                ImGui.Text("Processing...");
                if (!texImportWaiting)
                {
                    selectedNode.Children = null;
                    selectedNode.Data = texImportData;
                    texImportData = null;
                    ImGui.CloseCurrentPopup();
                }
            } else {
                ImGui.Image((IntPtr)teximportid, new Vector2(64, 64),
                            new Vector2(0,1), new Vector2(1,0), Vector4.One, Vector4.Zero);
                ImGui.Text(string.Format("Dimensions: {0}x{1}", teximportprev.Width, teximportprev.Height));
                ImGui.Combo("Format", ref compressOption, texOptions);
                ImGui.Checkbox("Production Quality (slow)", ref compressSlow);
                if(ImGui.Button("Import")) {
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
                if(ImGui.Button("Cancel")) {
                    ImGuiHelper.DeregisterTexture(teximportprev);
                    teximportprev.Dispose();
                    teximportprev = null;
                    ImGui.CloseCurrentPopup();
                }
            }
        }
       

        bool stringConfirm = false;
        bool stringEditor = false;
        bool floatEditor = false;
        float[] floats;
        bool intEditor = false;
        int[] ints;
        bool intHex = false;
        bool colorPicker = false;
        bool pickcolor4 = false;
        System.Numerics.Vector4 color4;
        System.Numerics.Vector3 color3;

        unsafe void Popups()
        {
            //TextureImport
            if (openTexImport)
            {
                ImGui.OpenPopup("Import Texture");
                openTexImport = false;
            }
            if(ImGui.BeginPopupModal("Import Texture"))
            {
                TexImportDialog();
                ImGui.EndPopup();
            }
            //StringEditor
            if (stringConfirm)
            {
                ImGui.OpenPopup("Confirm?##stringedit" + Unique);
                stringConfirm = false;
            }
            if (ImGui.BeginPopupModal("Confirm?##stringedit" + Unique, WindowFlags.AlwaysAutoResize))
            {
                ImGui.Text("Data is >255 bytes, string will be truncated. Continue?");
                if (ImGui.Button("Yes"))
                {
                    text.SetBytes(selectedNode.Data, 255);
                    stringEditor = true;
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button("No")) ImGui.CloseCurrentPopup();
                ImGui.EndPopup();
            }
            if (stringEditor)
            {
                ImGui.OpenPopup("String Editor##" + Unique);
                stringEditor = false;
            }
            if (ImGui.BeginPopupModal("String Editor##" + Unique, WindowFlags.AlwaysAutoResize))
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
                ImGui.EndPopup();
            }
            //Hex Editor
            if (hexEditor)
            {
                ImGui.OpenPopup("HexEditor##" + Unique);
                hexEditor = false;
            }
            if (ImGui.BeginPopupModal("HexEditor##" + Unique))
            {
                ImGui.PushFont(ImGuiHelper.Default);
                int res;
                if ((res = mem.Draw("Hex", hexdata, hexdata.Length, 0)) != 0)
                {
                    if (res == 1) selectedNode.Data = hexdata;
                    ImGui.CloseCurrentPopup();
                }
                ImGui.PopFont();
                ImGui.EndPopup();
            }
            //Color Picker
            if (colorPicker)
            {
                ImGui.OpenPopup("Color Picker##" + Unique);
                colorPicker = false;
            }
            if (ImGui.BeginPopupModal("Color Picker##" + Unique, WindowFlags.AlwaysAutoResize))
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
                ImGui.EndPopup();
            }
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
            //Rename dialog
            if (doRename)
            {
                ImGui.OpenPopup("Rename##" + Unique);
                doRename = false;
            }
            if (ImGui.BeginPopupModal("Rename##" + Unique, WindowFlags.AlwaysAutoResize))
            {
                ImGui.Text("Name: ");
                ImGui.SameLine();
                bool entered = ImGui.InputText("", text.Pointer, text.Size, InputTextFlags.EnterReturnsTrue, text.Callback);
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
                ImGui.EndPopup();
            }
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
            //Add
            if (doAdd)
            {
                ImGui.OpenPopup("New Node##" + Unique);
                doAdd = false;
            }
            if (ImGui.BeginPopupModal("New Node##" + Unique, WindowFlags.AlwaysAutoResize))
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

        LUtfNode pasteInto;
        LUtfNode clearNode;

        bool doRename = false;
        LUtfNode renameNode;

        LUtfNode deleteNode;
        LUtfNode deleteParent;

        bool doAdd = false;
        int addOffset = 0;
        LUtfNode addNode;
        LUtfNode addParent;

        void DoNodeMenu(string id, LUtfNode node, LUtfNode parent)
        {
            if (ImGui.BeginPopupContextItem(id))
            {
                ImGui.MenuItem(node.Name, false);
                ImGui.MenuItem(string.Format("CRC: 0x{0:X}", CrcTool.FLModelCrc(node.Name)), false);
                ImGui.Separator();
                if (ImGui.MenuItem("Rename", node != Utf.Root))
                {
                    text.SetText(node.Name);
                    renameNode = node;
                    doRename = true;
                }
                if (ImGui.MenuItem("Delete", node != Utf.Root))
                {
                    deleteParent = parent;
                    deleteNode = node;
                    Confirm("Are you sure you want to delete: '" + node.Name + "'?", () =>
                    {
                        if (selectedNode == deleteNode)
                        {
                            selectedNode = null;
                        }
                        deleteParent.Children.Remove(deleteNode);
                    });
                }
                if (ImGui.MenuItem("Clear", node.Children != null || node.Data != null))
                {
                    clearNode = node;
                    Confirm("Clearing this node will delete all data and children. Continue?", () =>
                    {
                        clearNode.Data = null;
                        if (clearNode == Utf.Root)
                            clearNode.Children = new List<LUtfNode>();
                        else
                            clearNode.Children = null;
                    });
                }
                ImGui.Separator();
                if (ImGui.BeginMenu("Add"))
                {
                    if (ImGui.MenuItem("Child"))
                    {
                        text.SetText("");
                        addParent = null;
                        addNode = node;
                        if (selectedNode.Data != null)
                        {
                            Confirm("Adding a node will clear data. Continue?", () =>
                            {
                                doAdd = true;
                            });
                        }
                        else
                            doAdd = true;
                    }
                    if (ImGui.MenuItem("Before", node != Utf.Root))
                    {
                        text.SetText("");
                        addParent = parent;
                        addNode = node;
                        addOffset = 0;
                        doAdd = true;
                    }
                    if (ImGui.MenuItem("After", node != Utf.Root))
                    {
                        text.SetText("");
                        addParent = parent;
                        addNode = node;
                        addOffset = 1;
                        doAdd = true;
                    }
                    ImGui.EndMenu();
                }
                ImGui.Separator();
                if (ImGui.MenuItem("Cut", node != Utf.Root))
                {
                    parent.Children.Remove(node);
                    main.ClipboardCopy = false;
                    main.Clipboard = node;
                }
                if (ImGui.MenuItem("Copy", node != Utf.Root))
                {
                    main.ClipboardCopy = true;
                    main.Clipboard = node.MakeCopy();
                }
                if (main.Clipboard != null)
                {
                    if (ImGui.BeginMenu("Paste"))
                    {
                        if (ImGui.MenuItem("Before", node != Utf.Root))
                        {
                            if (main.ClipboardCopy)
                            {
                                var cpy = main.Clipboard.MakeCopy();
                                cpy.Parent = parent;
                                parent.Children.Insert(parent.Children.IndexOf(node), cpy);
                            }
                            else
                            {
                                main.Clipboard.Parent = parent;
                                parent.Children.Insert(parent.Children.IndexOf(node), main.Clipboard);
                                main.Clipboard = null;
                            }
                        }
                        if (ImGui.MenuItem("After", node != Utf.Root))
                        {
                            if (main.ClipboardCopy)
                            {
                                var cpy = main.Clipboard.MakeCopy();
                                cpy.Parent = parent;
                                parent.Children.Insert(parent.Children.IndexOf(node) + 1, cpy);
                            }
                            else
                            {
                                main.Clipboard.Parent = parent;
                                parent.Children.Insert(parent.Children.IndexOf(node) + 1, main.Clipboard);
                                main.Clipboard = null;
                            }
                        }
                        if (ImGui.MenuItem("Into"))
                        {
                            if (node.Data == null)
                            {
                                if (node.Children == null) node.Children = new List<LUtfNode>();
                                if (main.ClipboardCopy)
                                {
                                    var cpy = main.Clipboard.MakeCopy();
                                    cpy.Parent = node;
                                    node.Children.Add(cpy);
                                }
                                else
                                {
                                    main.Clipboard.Parent = node;
                                    node.Children.Add(main.Clipboard);
                                    main.Clipboard = null;
                                }
                            }
                            else
                            {
                                pasteInto = node;
                                Confirm("Adding children will delete this node's data. Continue?", () =>
                                {
                                    pasteInto.Data = null;
                                    pasteInto.Children = new List<LUtfNode>();
                                    if (main.ClipboardCopy)
                                    {
                                        var cpy = main.Clipboard.MakeCopy();
                                        cpy.Parent = pasteInto;
                                        pasteInto.Children.Add(cpy);
                                    }
                                    else
                                    {
                                        main.Clipboard.Parent = pasteInto;
                                        pasteInto.Children.Add(main.Clipboard);
                                        main.Clipboard = null;
                                    }
                                });
                            }
                        }
                        ImGui.EndMenu();
                    }
                }
                else
                {
                    ImGui.MenuItem("Paste", false);
                }
                ImGui.EndPopup();
            }
        }

        void DoNode(LUtfNode node, LUtfNode parent, int idx)
        {
            string id = node.Name + "##" + parent.Name + idx;
            if (node.Children != null)
            {
                var flags = selectedNode == node ? TreeNodeFlags.Selected | tflags : tflags;
                var isOpen = ImGui.TreeNodeEx(id, flags);
                if (ImGuiNative.igIsItemClicked(0))
                {
                    selectedNode = node;
                }
                ImGui.PushID(id);
                DoNodeMenu(id, node, parent);
                ImGui.PopID();
                //int i = 0;
                if (isOpen)
                {
                    for (int i = 0; i < node.Children.Count; i++)
                    {
                        DoNode(node.Children[i], node, (idx * 1024) + i);
                    }
                    ImGui.TreePop();
                }
            }
            else
            {
                if (node.Data != null)
                {
                    ImGui.Bullet();
                }
                else
                {
                    Theme.Icon("node_empty", Color4.White);
                    ImGui.SameLine();
                }
                bool selected = selectedNode == node;
                if (ImGui.SelectableEx(id, ref selected))
                {
                    selectedNode = node;
                }
                DoNodeMenu(id, node, parent);
            }

        }
    }
}