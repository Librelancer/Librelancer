// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.IO;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ImUI;
using LibreLancer.Utf.Ale;
using LibreLancer.ContentEdit;
using LibreLancer.ContentEdit.Model;
using LibreLancer.ImageLib;
using LibreLancer.Utf.Cmp;

namespace LancerEdit
{
    public partial class UtfTab : EditorTab
    {
        public string FilePath = null;
        public int DirtyCountHp = 0;
        public int DirtyCountPart = 0;
        public EditableUtf Utf;
        LUtfNode selectedNode = null;
        MainWindow main;
        PopupManager popups = new PopupManager();
        List<JointMapView> jointViews = new List<JointMapView>();
        //generated parameter is used for utf generated internally, like from the collada exporter
        //saves a bunch of copies when opening a large UTF from disk
        public UtfTab(MainWindow main, EditableUtf utf, string title, bool generated = false)
        {
            this.main = main;
            Utf = utf;
            DocumentName = title;
            Title = title;
            text = new TextBuffer();
            if(generated) utf.Source = utf.Export();
            if (utf.Source != null)
            {
                main.Resources.AddResources(utf.Source, Unique.ToString());
                utf.Source = null;
            }
            SaveStrategy = new UtfSaveStrategy(main, this);
            RegisterPopups();
        }
        public void UpdateTitle()
        {
            Title = DocumentName;
        }

        ImGuiTreeNodeFlags tflags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;
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


        public override void Draw(double elapsed)
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
            ImGui.BeginChild("##scroll");
            var flags = selectedNode == Utf.Root ? ImGuiTreeNodeFlags.Selected | tflags : tflags;
            var isOpen = ImGui.TreeNodeEx("/", flags);
            if (ImGui.IsItemClicked(0))
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
            //if (ImGui.Button("Actions"))
                //ImGui.OpenPopup("actions");
            using (var tb = Toolbar.Begin("##actions", false))
            {
                if (tb.ButtonItem("View Model"))
                {
                    IDrawable drawable = null;
                    ModelNodes hpn = new ModelNodes();
                    try
                    {
                        drawable = LibreLancer.Utf.UtfLoader.GetDrawable(Utf.Export(), main.Resources);
                        if(Utf.Root.Children.Any((x) => x.Name.Equals("cmpnd",StringComparison.OrdinalIgnoreCase))) {
                            foreach(var child in Utf.Root.Children.Where((x) => x.Name.EndsWith(".3db", StringComparison.OrdinalIgnoreCase))) {
                                var n = new ModelHpNode();
                                n.Name = child.Name;
                                n.Node = child;
                                n.HardpointsNode = child.Children.FirstOrDefault((x) => x.Name.Equals("hardpoints", StringComparison.OrdinalIgnoreCase));
                                hpn.Nodes.Add(n);
                            }
                            var cmpnd = Utf.Root.Children.First((x) => x.Name.Equals("cmpnd", StringComparison.OrdinalIgnoreCase));
                            hpn.Cons = cmpnd.Children.FirstOrDefault((x) => x.Name.Equals("cons", StringComparison.OrdinalIgnoreCase));
                        } else {
                            var n = new ModelHpNode();
                            n.Name = "ROOT";
                            n.Node = Utf.Root;
                            n.HardpointsNode = Utf.Root.Children.FirstOrDefault((x) => x.Name.Equals("hardpoints", StringComparison.OrdinalIgnoreCase));
                            hpn.Nodes.Add(n);
                        }
                    }
                    catch (Exception ex) { ErrorPopup("Could not open as model\n" + ex.Message + "\n" + ex.StackTrace); drawable = null; }
                    if (drawable != null)
                    {
                        main.AddTab(new ModelViewer(DocumentName, drawable, main, this,hpn));
                    }
                }

                if (tb.ButtonItem("View Ale"))
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
                        main.AddTab(new AleViewer(Title, ale, main));
                }

                if (tb.ButtonItem("Resolve Audio Hashes"))
                {
                    FileDialog.ChooseFolder(folder =>
                    {
                        var idtable = new IDTable(folder);
                        foreach (var n in Utf.Root.IterateAll())
                        {
                            if (n.Name.StartsWith("0x"))
                            {
                                uint v;
                                if (uint.TryParse(n.Name.Substring(2), NumberStyles.HexNumber,
                                        CultureInfo.InvariantCulture, out v))
                                {
                                    idtable.UtfNicknameTable.TryGetValue(v, out n.ResolvedName);
                                }
                            }
                            else
                                n.ResolvedName = null;
                        }
                    });
                }
                if(tb.ButtonItem("Reload Resources"))
                {
                    main.Resources.RemoveResourcesForId(Unique.ToString());
                    main.Resources.AddResources(Utf.Export(), Unique.ToString());
                }
            }

            Popups();
        }

        unsafe int DummyCallback(ImGuiInputTextCallbackData* data)
        {
            return 0;
        }

        unsafe void NodeInformation()
        {
            ImGui.BeginChild("##scrollnode");
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
                                FileDialog.Open(path =>
                                {
                                    selectedNode.Children = null;
                                    selectedNode.Data = File.ReadAllBytes(path);
                                });
                            });
                        }
                        if (ImGui.MenuItem("Texture"))
                            Confirm("Importing data will delete this node's children. Continue?", ImportTexture);
                        ImGui.EndPopup();
                    }

                    if (selectedNode.Name.StartsWith("joint map", StringComparison.OrdinalIgnoreCase))
                    {
                        if (ImGui.Button("View Joint Map"))
                        {
                            JointMapView jmv;
                            if((jmv = JointMapView.Create(selectedNode)) != null)
                                jointViews.Add(jmv);
                        };
                    }

                    if (selectedNode.Name.StartsWith("object map", StringComparison.OrdinalIgnoreCase))
                    {
                        if (ImGui.Button("View Object Map"))
                        {
                            JointMapView jmv;
                            if((jmv = JointMapView.Create(selectedNode)) != null)
                                jointViews.Add(jmv);
                        };
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
                    ImGui.InputText("##strpreview", selectedNode.Data, (uint)Math.Min(selectedNode.Data.Length, 32), ImGuiInputTextFlags.ReadOnly, DummyCallback);
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
                            color4 = Color4.Black;
                        }
                        else if (len == 3)
                        {
                            pickcolor4 = false;
                            color3 = new Vector3(
                                BitConverter.ToSingle(selectedNode.Data, 0),
                                BitConverter.ToSingle(selectedNode.Data, 4),
                                BitConverter.ToSingle(selectedNode.Data, 8));
                        }
                        else if (len > 3)
                        {
                            pickcolor4 = true;
                            color4 = new Vector4(
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
                    Texture tex = null;
                    try
                    {
                        using (var stream = new MemoryStream(selectedNode.Data))
                        {
                            if (DDS.StreamIsDDS(stream))
                                tex = DDS.FromStream(stream);
                            else
                                tex = TGA.FromStream(stream);
                        }
                        var title = string.Format("{0} ({1})", selectedNode.Name, Title);
                        if (tex is Texture2D tex2d)
                        {
                            var tab = new TextureViewer(title, tex2d, null);
                            main.AddTab(tab);
                        }
                        else if (tex is TextureCube texcube)
                        {
                            var tab = new CubemapViewer(title, texcube, main);
                            main.AddTab(tab);
                        }
                    }
                    catch (Exception ex)
                    {
                        #if DEBUG
                        throw;
                        #else
                        ErrorPopup("Node data couldn't be opened as texture:\n" + ex.Message);
                        #endif
                    }
                }
                if (ImGui.Button("Play Audio"))
                {
                    try
                    {
                        var data = main.Audio.AllocateData();
                        using (var stream = new MemoryStream(selectedNode.Data))
                        {
                            main.Audio.PlayStream(stream);
                        }
                    }
                    catch (Exception ex)
                    {
                        FLLog.Error("Audio", ex.ToString());
                        ErrorPopup("Error:\n" + ex.Message);
                    }
                }
                if (ImGui.Button("Import Data"))
                    ImGui.OpenPopup("importactions");
                if (ImGui.BeginPopup("importactions"))
                {
                    if (ImGui.MenuItem("File"))
                    {
                        FileDialog.Open(path => selectedNode.Data = File.ReadAllBytes(path));
                    }
                    if (ImGui.MenuItem("Texture"))
                        ImportTexture();
                    ImGui.EndPopup();
                }
                if (ImGui.Button("Export Data"))
                {
                    FileDialog.Save(path =>  File.WriteAllBytes(path, selectedNode.Data));
                }
                if (selectedNode.Name.ToLowerInvariant() == "vmeshdata" &&
                    ImGui.Button("View VMeshData"))
                {
                    LibreLancer.Utf.Vms.VMeshData dat = null;
                    try
                    {
                        dat = new LibreLancer.Utf.Vms.VMeshData(new ArraySegment<byte>(selectedNode.Data),  "");
                    }
                    catch (Exception ex)
                    {
                        ErrorPopup(string.Format("Not a valid VMeshData node\n{0}\n{1}", ex.Message, ex.StackTrace));
                    }

                    if (dat != null)
                    {
                        main.TextWindows.Add(new TextDisplayWindow(DumpObject.DumpVmeshData(dat), selectedNode.Name + ".txt"));
                    }
                }

                if (selectedNode.Name.ToLowerInvariant() == "vmeshref" &&
                    ImGui.Button("View VMeshRef"))
                {
                    LibreLancer.Utf.Cmp.VMeshRef dat = null;
                    try
                    {
                        dat = new LibreLancer.Utf.Cmp.VMeshRef(new ArraySegment<byte>(selectedNode.Data));
                    }
                    catch (Exception ex)
                    {
                        ErrorPopup(string.Format("Not a valid VMeshRef node\n{0}\n{1}", ex.Message, ex.StackTrace));
                    }

                    if (dat != null)
                    {
                        main.TextWindows.Add(new TextDisplayWindow(DumpObject.DumpVmeshRef(dat), selectedNode.Name + ".txt"));
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
                        FileDialog.Open(path => selectedNode.Data = File.ReadAllBytes(path));
                    }
                    if(ImGui.MenuItem("Texture"))
                        ImportTexture();
                    ImGui.EndPopup();
                }
            }

            var removeJmv = new List<JointMapView>();
            foreach (var jm in jointViews)
            {
                if(!jm.Draw()) removeJmv.Add(jm);
            }
            foreach (var jmv in removeJmv) jointViews.Remove(jmv);
            ImGui.EndChild();
        }

        void ImportTexture()
        {
            FileDialog.Open(path =>
            {
                var src = TextureImport.OpenFile(path);
                if (src.Type == TexLoadType.ErrorLoad ||
                    src.Type == TexLoadType.ErrorNonSquare ||
                    src.Type == TexLoadType.ErrorNonPowerOfTwo)
                {
                    main.ErrorDialog(TextureImport.LoadErrorString(src.Type, path));
                }
                else if (src.Type == TexLoadType.DDS)
                {
                    src.Texture.Dispose();
                    selectedNode.Children = null;
                    selectedNode.Data = File.ReadAllBytes(path);
                }
                else
                {
                    teximportprev = src.Texture;
                    teximportpath = path;
                    teximportid = ImGuiHelper.RegisterTexture(teximportprev);
                    popups.OpenPopup("Texture Import");
                }
            });
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
                if(Theme.IconMenuItem(Icons.Edit, "Rename", node != Utf.Root))
                {
                    text.SetText(node.Name);
                    renameNode = node;
                    popups.OpenPopup("Rename Node");
                }
                if (Theme.IconMenuItem(Icons.TrashAlt, "Delete", node != Utf.Root))
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
                if (Theme.IconMenuItem(Icons.Eraser, "Clear", node.Children != null || node.Data != null))
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
                if (Theme.BeginIconMenu(Icons.PlusCircle, "Add"))
                {
                    if (ImGui.MenuItem("Child"))
                    {
                        text.SetText("");
                        addParent = null;
                        addNode = node;
                        if (node.Data != null)
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
                if (Theme.IconMenuItem(Icons.Cut, "Cut", node != Utf.Root))
                {
                    parent.Children.Remove(node);
                    main.ClipboardCopy = false;
                    main.Clipboard = node;
                }
                if (Theme.IconMenuItem(Icons.Copy, "Copy", node != Utf.Root))
                {
                    main.ClipboardCopy = true;
                    main.Clipboard = node.MakeCopy();
                }
                if (main.Clipboard is LUtfNode utfNode)
                {
                    if (Theme.BeginIconMenu(Icons.Paste, "Paste"))
                    {
                        if (ImGui.MenuItem("Before", node != Utf.Root))
                        {
                            if (main.ClipboardCopy)
                            {
                                var cpy = utfNode.MakeCopy();
                                cpy.Parent = parent;
                                parent.Children.Insert(parent.Children.IndexOf(node), cpy);
                            }
                            else
                            {
                                utfNode.Parent = parent;
                                parent.Children.Insert(parent.Children.IndexOf(node), utfNode);
                                main.Clipboard = null;
                            }
                        }
                        if (ImGui.MenuItem("After", node != Utf.Root))
                        {
                            if (main.ClipboardCopy)
                            {
                                var cpy = utfNode.MakeCopy();
                                cpy.Parent = parent;
                                parent.Children.Insert(parent.Children.IndexOf(node) + 1, cpy);
                            }
                            else
                            {
                                utfNode.Parent = parent;
                                parent.Children.Insert(parent.Children.IndexOf(node) + 1, utfNode);
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
                                    var cpy = utfNode.MakeCopy();
                                    cpy.Parent = node;
                                    node.Children.Add(cpy);
                                }
                                else
                                {
                                    utfNode.Parent = node;
                                    node.Children.Add(utfNode);
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
                                        var cpy = utfNode.MakeCopy();
                                        cpy.Parent = pasteInto;
                                        pasteInto.Children.Add(cpy);
                                    }
                                    else
                                    {
                                        utfNode.Parent = pasteInto;
                                        pasteInto.Children.Add(utfNode);
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
                    Theme.IconMenuItem(Icons.Paste, "Paste", false);
                }
                ImGui.EndPopup();
            }
        }

        void DoNode(LUtfNode node, LUtfNode parent, int idx)
        {
            string id = ImGuiExt.IDWithExtra(node.Name, parent.Name + idx);
            if (node.Children != null)
            {
                var flags = selectedNode == node ? ImGuiTreeNodeFlags.Selected | tflags : tflags;
                var isOpen = ImGui.TreeNodeEx(id, flags);
                if (ImGui.IsItemClicked(0))
                {
                    selectedNode = node;
                }
                if (node.ResolvedName != null)
                {
                    ImGui.SameLine();
                    ImGui.TextDisabled("(" + ImGuiExt.IDSafe(node.ResolvedName) + ")");
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
                    ImGui.Text($"  {Icons.BulletEmpty}");
                    ImGui.SameLine();
                }
                bool selected = selectedNode == node;
                if (ImGui.Selectable(id, ref selected))
                {
                    selectedNode = node;
                }
                if (node.ResolvedName != null)
                {
                    ImGui.SameLine();
                    ImGui.TextDisabled("(" + ImGuiExt.IDSafe(node.ResolvedName) + ")");
                }
                DoNodeMenu(id, node, parent);
            }

        }
    }
}
