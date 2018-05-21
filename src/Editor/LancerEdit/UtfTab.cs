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
    public partial class UtfTab : DockTab
    {
        bool open = true;
        public EditableUtf Utf;
        LUtfNode selectedNode = null;
        MainWindow main;
        PopupManager popups = new PopupManager();
        public UtfTab(MainWindow main, EditableUtf utf, string title)
        {
            this.main = main;
            Utf = utf;
            Title = string.Format("{0}##{1}",title,Unique);
            text = new TextBuffer();
            main.Resources.AddResources(utf.Export(), Unique.ToString());
            RegisterPopups();
        }

        public override void SetActiveTab(MainWindow win)
        {
            win.ActiveTab = this;
        }
      
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
                if(ImGui.MenuItem("Dump Model"))
                {
                    LibreLancer.Utf.Cmp.ModelFile model = null;
                    LibreLancer.Utf.Cmp.CmpFile cmp = null;
                    try
                    {
                        var drawable = LibreLancer.Utf.UtfLoader.GetDrawable(Utf.Export(), main.Resources);
                        model = (drawable as LibreLancer.Utf.Cmp.ModelFile);
                        cmp = (drawable as LibreLancer.Utf.Cmp.CmpFile);
                    }
                    catch (Exception) { ErrorPopup("Could not open as model"); model = null; }
                    if (model != null)
                    {
                        var output = FileDialog.Save();
                        if(output != null) {
                            DumpStatus(DumpObject.DumpObj(model, output));
                        }
                    }
                    if(cmp != null)
                    {
                        dumpcmp = cmp;
                        DoPickObject();
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
                            popups.OpenPopup("Confirm?##stringedit");
                        else
                        {
                            text.SetBytes(selectedNode.Data, selectedNode.Data.Length);
                            popups.OpenPopup("String Editor");
                        }
                    }
                    if (ImGui.MenuItem("Hex Editor"))
                    {
                        hexdata = new byte[selectedNode.Data.Length];
                        selectedNode.Data.CopyTo(hexdata, 0);
                        mem = new MemoryEditor();
                        popups.OpenPopup("Hex Editor");
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
                        popups.OpenPopup("Color Picker");
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
                    if(selectedNode.Name.ToLowerInvariant() == "vmeshdata")
                        ImGui.OpenPopup("exportactions");
                    else
                    {
                        string path;
                        if ((path = FileDialog.Save()) != null)
                        {
                            File.WriteAllBytes(path, selectedNode.Data);
                        }
                    }
                }
                if(ImGui.BeginPopup("exportactions"))
                {
                    if(ImGui.MenuItem("Raw"))
                    {
                        string path;
                        if ((path = FileDialog.Save()) != null)
                        {
                            File.WriteAllBytes(path, selectedNode.Data);
                        }
                    }
                    if(ImGui.MenuItem("VMeshData"))
                    {
                        string path;
                        if ((path = FileDialog.Save()) != null)
                        {
                            LibreLancer.Utf.Vms.VMeshData dat = null;
                            try
                            {
                                dat = new LibreLancer.Utf.Vms.VMeshData(selectedNode.Data, new EmptyLib(), "");
                            }
                            catch (Exception ex)
                            {
                                ErrorPopup(string.Format("Not a valid VMeshData node\n{0}\n{1}", ex.Message, ex.StackTrace));
                            }
                            if (dat != null) DumpObject.DumpVmeshData(path, dat);
                        }
                    }
                    ImGui.EndPopup();
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
                        popups.OpenPopup("Texture Import");
                    }
                    catch (Exception)
                    {
                        ErrorPopup("Could not open file as image");
                    }
                }
            }
        }

        LUtfNode pasteInto;
        LUtfNode clearNode;

        LUtfNode renameNode;

        LUtfNode deleteNode;
        LUtfNode deleteParent;

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
                    popups.OpenPopup("Rename Node");
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
                                popups.OpenPopup("New Node");
                            });
                        }
                        else
                            popups.OpenPopup("New Node");
                    }
                    if (ImGui.MenuItem("Before", node != Utf.Root))
                    {
                        text.SetText("");
                        addParent = parent;
                        addNode = node;
                        addOffset = 0;
                        popups.OpenPopup("New Node");
                    }
                    if (ImGui.MenuItem("After", node != Utf.Root))
                    {
                        text.SetText("");
                        addParent = parent;
                        addNode = node;
                        addOffset = 1;
                        popups.OpenPopup("New Node");
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