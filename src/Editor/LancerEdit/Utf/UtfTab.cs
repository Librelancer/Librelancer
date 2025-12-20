// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LancerEdit.Audio;
using LancerEdit.Utf.Popups;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.ContentEdit.Model;
using LibreLancer.Data;
using LibreLancer.Dialogs;
using LibreLancer.Graphics;
using LibreLancer.ImageLib;
using LibreLancer.ImUI;
using LibreLancer.Resources;
using LibreLancer.Utf.Ale;

namespace LancerEdit
{
    public partial class UtfTab : EditorTab
    {
        public string FilePath = null;
        public int DirtyCountHp = 0;
        public int DirtyCountPart = 0;
        public int DirtyCountAnm = 0;
        public EditableUtf Utf;
        LUtfNode selectedNode = null;
        MainWindow main;
        PopupManager popups = new PopupManager();
        List<JointMapView> jointViews = new List<JointMapView>();

        public GameResourceManager DetachedResources;
        public int DetachedResourceCount;

        // Track ImGui expanded/collapsed state
        Dictionary<LUtfNode, bool> nodeOpenState = new();

        public override string Tooltip => FilePath;

        public void ReferenceDetached()
        {
            DetachedResourceCount++;
        }

        public void DereferenceDetached()
        {
            DetachedResourceCount--;
            if (DetachedResourceCount == 0)
                DetachedResources?.Dispose();
        }
        //generated parameter is used for utf generated internally, like from the collada exporter
        //saves a bunch of copies when opening a large UTF from disk
        public UtfTab(MainWindow main, EditableUtf utf, string title, bool generated = false)
        {
            this.main = main;
            Utf = utf;
            DocumentName = title;
            Title = title;
            if (generated) utf.Source = utf.Export();
            if (utf.Source != null)
            {
                main.Resources.AddResources(utf.Source, Unique.ToString());
                utf.Source = null;
            }
            SaveStrategy = new UtfSaveStrategy(main, this);
            ReferenceDetached();
        }
        public void UpdateTitle()
        {
            Title = DocumentName;
        }

        ImGuiTreeNodeFlags tflags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;

        public override void Dispose()
        {
            DereferenceDetached();
            main.Resources.RemoveResourcesForId(Unique.ToString());
        }

        public string GetUtfPath()
        {
            if (selectedNode == null) return "None";
            List<string> strings = new List<string>();
            LUtfNode node = selectedNode;
            while (node != null)
            {
                strings.Add(node.Name);
                node = node.Parent;
            }
            strings.Reverse();
            var path = string.Join("/", strings);
            return path;
        }

        public void GenerateTangents()
        {
            try
            {
                LibreLancer.Utf.UtfLoader.GetDrawable(Utf.Export(), main.Resources);
                Confirm("This action will overwrite any existing tangent data. Continue?", () =>
                {
                    var result = Tangents.GenerateForUtf(Utf);
                    main.ResultMessages(result);
                    ReloadResources();
                });
            }
            catch (Exception ex)
            {
                ErrorPopup("Could not open as model\n" + ex.Message + "\n" + ex.StackTrace);
            }
        }

        public void ReloadResources()
        {
            var res = DetachedResources ?? main.Resources;
            res.RemoveResourcesForId(Unique.ToString());
            res.AddResources(Utf.Export(), Unique.ToString());
        }

        public override void OnHotkey(Hotkeys hk, bool shiftPressed)
        {
            if (selectedNode == null)
                return;
            if (hk == Hotkeys.Copy && selectedNode != Utf.Root)
            {
                main.SetClipboardArray(UtfClipboard.ToBytes(selectedNode));
            }
            if (hk == Hotkeys.Cut && selectedNode != Utf.Root)
            {
                selectedNode.Parent.Children.Remove(selectedNode);
                main.SetClipboardArray(UtfClipboard.ToBytes(selectedNode));
            }

            if (hk == Hotkeys.Paste &&
                main.ClipboardStatus() == ClipboardContents.Array)
            {
                var cpy = UtfClipboard.FromBytes(main.GetClipboardArray());
                if (cpy == null) return;
                ConfirmIf(selectedNode.Data != null, "Adding children will delete this node's data. Continue?", () =>
                {
                    selectedNode.Data = null;
                    selectedNode.Children ??= new List<LUtfNode>();
                    cpy.Parent = selectedNode;
                    selectedNode.Children.Add(cpy);
                });
            }
        }

        private Action dropAction = null;
        public override void Draw(double elapsed)
        {
            //Child Window
            var size = ImGui.GetWindowSize();
            var actionBarHeight = ImGui.GetFrameHeightWithSpacing();
            ImGui.BeginChild("##utfchild", new Vector2(0, size.Y - actionBarHeight * 1.2f));
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
            DoNode(Utf.Root, null, isRoot: true);
            ImGui.EndChild();

            if (dropAction != null)
            {
                dropAction();
                dropAction = null;
            }
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

            using (var tb = Toolbar.Begin("##actions", false))
            {
                const string TooltipExpandAll = "Expands all nodes in the node tree.\n This action also exapands all children nodes recursively";
                const string TooltipCollapseAll = "Collapses all nodes in the node tree.\n This action also collapses all children nodes recursively";
                if (tb.ButtonItem("Expand All", true, TooltipExpandAll))
                {
                    ExpandRecursive(Utf.Root);
                }
                if (tb.ButtonItem("Collapse All", true, TooltipCollapseAll))
                {
                    CollapseRecursive(Utf.Root);
                }
                if (tb.ButtonItem("View Model"))
                {
                    IDrawable drawable = null;
                    ModelNodes hpn = new ModelNodes();
                    hpn.RootNode = Utf.Root;
                    try
                    {
                        drawable = LibreLancer.Utf.UtfLoader.GetDrawable(Utf.Export(), main.Resources);
                        if (Utf.Root.Children.Any((x) => x.Name.Equals("cmpnd", StringComparison.OrdinalIgnoreCase)))
                        {
                            foreach (var child in Utf.Root.Children.Where((x) => x.Name.EndsWith(".3db", StringComparison.OrdinalIgnoreCase)))
                            {
                                var n = new ModelHpNode();
                                n.Name = child.Name;
                                n.Node = child;
                                n.HardpointsNode = child.Children.FirstOrDefault((x) => x.Name.Equals("hardpoints", StringComparison.OrdinalIgnoreCase));
                                hpn.Nodes.Add(n);
                            }
                            var cmpnd = Utf.Root.Children.First((x) => x.Name.Equals("cmpnd", StringComparison.OrdinalIgnoreCase));
                            hpn.Cons = cmpnd.Children.FirstOrDefault((x) => x.Name.Equals("cons", StringComparison.OrdinalIgnoreCase));
                        }
                        else
                        {
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
                        ReferenceDetached();
                        main.AddTab(new ModelViewer(DocumentName, drawable, main, this, hpn));
                    }
                }

                if (tb.ButtonItem("Generate Tangents"))
                {
                    GenerateTangents();
                }

                if (tb.ButtonItem("View Ale"))
                {
                    AleFile ale = null;
                    try
                    {
                        ale = new AleFile(Utf.Export());
                    }
                    catch (Exception e)
                    {
                        ErrorPopup($"Could not open as ale\n{e.ToString()}");
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
                if (tb.ButtonItem("Reload Resources"))
                {
                    ReloadResources();
                }

                const string TooltipDetached = "Resources already detached";
                const string TooltipToDetach =
                    "Detaches tab from shared resources\nViewers opened will have textures+materials unique to this file.";
                string tooltip = DetachedResources == null ? TooltipToDetach : TooltipDetached;
                if (tb.ButtonItem("Detach Resources", DetachedResources == null, tooltip))
                {
                    TabColor = TabColor.Alternate;
                    main.Resources.RemoveResourcesForId(Unique.ToString());
                    DetachedResources = new GameResourceManager(main.Resources);
                    DetachedResources.AddResources(Utf.Export(), Unique.ToString());
                }
            }

            popups.Run();
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
                        if (ImGui.MenuItem("Audio", main.EnableAudioConversion))
                        {
                            Confirm("Importing data will delete this node's data. Continue?", () =>
                                AudioImportPopup.Run(main, popups, b => selectedNode.Data = b)
                                );
                        }
                        if (ImGui.MenuItem("Bulk Audio", main.EnableAudioConversion))
                        {
                            Confirm("Importing data will delete this node's children/data. Continue?", () =>
                            {
                                main.QueueUIThread(() =>
                                {
                                    BulkAudioTool.Open(main, popups, b =>
                                    {
                                        selectedNode.Data = null;
                                        selectedNode.Children = new List<LUtfNode>();
                                        foreach (var item in b)
                                        {
                                            selectedNode.Children.Add(new LUtfNode
                                            {
                                                Name = item.NodeName,
                                                Data = item.Data
                                            });

                                        }
                                    });
                                });
                            });
                        }
                        ImGui.EndPopup();
                    }

                    if (selectedNode.Name.StartsWith("joint map", StringComparison.OrdinalIgnoreCase))
                    {
                        if (ImGui.Button("View Joint Map"))
                        {
                            var jmv = JointMapView.Create(selectedNode);
                            main.ResultMessages(jmv);
                            if (jmv.IsSuccess)
                                jointViews.Add(jmv.Data);
                        }
                    }

                    if (selectedNode.Name.StartsWith("object map", StringComparison.OrdinalIgnoreCase))
                    {
                        if (ImGui.Button("View Object Map"))
                        {
                            var jmv = JointMapView.Create(selectedNode);
                            main.ResultMessages(jmv);
                            if (jmv.IsSuccess)
                                jointViews.Add(jmv.Data);
                        }
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
                    ImGui.InputText("##strpreview", selectedNode.Data, (uint)Math.Min(selectedNode.Data.Length, 32), ImGuiInputTextFlags.ReadOnly);
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
                        EditString(selectedNode);
                    }
                    if (ImGui.MenuItem("Hex Editor"))
                    {
                        popups.OpenPopup(new HexEditorPopup(selectedNode));
                    }
                    if (ImGui.MenuItem("Float Editor"))
                    {
                        popups.OpenPopup(new FloatEditorPopup(selectedNode));
                    }
                    if (ImGui.MenuItem("Int Editor"))
                    {
                        popups.OpenPopup(new IntEditorPopup(selectedNode));
                    }
                    if (ImGui.MenuItem("Color Picker"))
                    {
                        popups.OpenPopup(new ColorPickerPopup(selectedNode));
                    }
                    if (ImGui.MenuItem("Frame Rect Editor"))
                    {
                        popups.OpenPopup(new FrameRectEditorPopup(selectedNode));
                    }
                    ImGui.EndPopup();
                }
                ImGui.NextColumn();
                if (ImGui.Button("Texture Viewer"))
                {
                    Texture tex = null;
#if !DEBUG
                    try
                    {
#endif
                    using (var stream = new MemoryStream(selectedNode.Data))
                    {
                        if (DDS.StreamIsDDS(stream))
                            tex = DDS.FromStream(main.RenderContext, stream);
                        else
                            tex = TGA.TextureFromStream(main.RenderContext, stream);
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
#if !DEBUG
                    }
                    catch (Exception ex)
                    {
                        ErrorPopup("Node data couldn't be opened as texture:\n" + ex.Message);
                    }
#endif
                }

                if (main.PlayingBuffer)
                {
                    if (ImGui.Button("Stop Audio"))
                        main.StopBuffer();
                }
                else if (ImGui.Button("Play Audio"))
                {
                    main.PlayBuffer(selectedNode.Data);
                }

                if (!main.PlayingBuffer && ImGui.BeginPopupContextItem("loopmenu"))
                {
                    if (ImGui.MenuItem("Play Looped"))
                        main.PlayBuffer(selectedNode.Data, true);
                    ImGui.EndPopup();
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
                    if (ImGui.MenuItem("Audio", main.EnableAudioConversion))
                    {
                        AudioImportPopup.Run(main, popups, b => selectedNode.Data = b);
                    }
                    ImGui.EndPopup();
                }
                if (ImGui.Button("Export Data"))
                {
                    FileDialog.Save(path => File.WriteAllBytes(path, selectedNode.Data));
                }
                if (selectedNode.Name.ToLowerInvariant() == "vmeshdata" &&
                    ImGui.Button("View VMeshData"))
                {
                    LibreLancer.Utf.Vms.VMeshData dat = null;
                    try
                    {
                        dat = new LibreLancer.Utf.Vms.VMeshData(new ArraySegment<byte>(selectedNode.Data), "");
                    }
                    catch (Exception ex)
                    {
                        ErrorPopup(string.Format("Not a valid VMeshData node\n{0}\n{1}", ex.Message, ex.StackTrace));
                    }

                    if (dat != null)
                    {
                        main.TextWindows.Add(new TextDisplayWindow(DumpObject.DumpVmeshData(dat), selectedNode.Name + ".txt", main));
                    }
                }

                if (selectedNode.Name.ToLowerInvariant() == "vmeshdata" &&
                    ImGui.Button("Edit Materials"))
                {
                    VmsMaterialEditor dat = null;
                    try
                    {
                        dat = new VmsMaterialEditor(selectedNode, main.Resources, this);
                    }
                    catch (Exception ex)
                    {
                        ErrorPopup(string.Format("Not a valid VMeshData node\n{0}\n{1}", ex.Message, ex.StackTrace));
                    }
                    if (dat != null)
                        popups.OpenPopup(dat);
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
                        main.TextWindows.Add(new TextDisplayWindow(DumpObject.DumpVmeshRef(dat), selectedNode.Name + ".txt", main));
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
                if (ImGui.BeginPopup("importactions"))
                {
                    if (ImGui.MenuItem("File"))
                    {
                        FileDialog.Open(path => selectedNode.Data = File.ReadAllBytes(path));
                    }
                    if (ImGui.MenuItem("Texture"))
                        ImportTexture();
                    if (ImGui.MenuItem("Audio", main.EnableAudioConversion))
                        AudioImportPopup.Run(main, popups, b => selectedNode.Data = b);
                    if (ImGui.MenuItem("Bulk Audio", main.EnableAudioConversion))
                        BulkAudioTool.Open(main, popups, b =>
                        {
                            selectedNode.Children = new List<LUtfNode>();
                            foreach (var item in b)
                            {
                                selectedNode.Children.Add(new LUtfNode
                                {
                                    Name = item.NodeName,
                                    Data = item.Data
                                });

                            }
                        });
                    ImGui.EndPopup();
                }
            }

            var removeJmv = new List<JointMapView>();
            foreach (var jm in jointViews)
            {
                if (!jm.Draw()) removeJmv.Add(jm);
            }
            foreach (var jmv in removeJmv) jointViews.Remove(jmv);
            ImGui.EndChild();
        }

        void ImportTexture()
        {
            FileDialog.Open(path =>
            {
                var src = TextureImport.OpenBuffer(File.ReadAllBytes(path), main.RenderContext);
                if (src.IsError)
                {
                    main.ResultMessages(src);
                }
                else if (src.Data.Type == TexLoadType.DDS)
                {
                    src.Data.Texture.Dispose();
                    selectedNode.Data = null;
                    selectedNode.Children = new List<LUtfNode>()
                    {
                        new () { Parent = selectedNode, Name = "MIPS", Data = File.ReadAllBytes(path) }
                    };
                }
                else
                {
                    var target = selectedNode;
                    popups.OpenPopup(new TextureImportPopup(src.Data, ch =>
                    {
                        foreach (var child in ch)
                            child.Parent = target;
                        target.Data = null;
                        target.Children = ch;
                    }, main));
                }
            });
        }

        LUtfNode clearNode;

        LUtfNode deleteNode;
        LUtfNode deleteParent;

        private bool canPaste = false;

        void DoNodeMenu(string id, LUtfNode node, LUtfNode parent)
        {
            if (ImGui.IsItemHovered() && ImGui.IsMouseReleased(ImGuiMouseButton.Right))
            {
                canPaste = main.ClipboardStatus() == ClipboardContents.Array;
            }
            if (ImGui.BeginPopupContextItem(id))
            {
                ImGui.MenuItem(node.Name, false);
                ImGui.MenuItem(string.Format("CRC: 0x{0:X}", CrcTool.FLModelCrc(node.Name)), false);
                ImGui.Separator();
                if (Theme.IconMenuItem(Icons.Edit, "Rename", node != Utf.Root))
                {
                    RenameNode(node);
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
                        ConfirmIf(node.Data != null, "Adding a node will clear data. Continue?", () =>
                        {
                            AddNode(null, node, 0);
                        });
                    }
                    if (ImGui.MenuItem("Before", node != Utf.Root))
                    {
                        AddNode(parent, node, 0);
                    }
                    if (ImGui.MenuItem("After", node != Utf.Root))
                    {
                        AddNode(parent, node, 1);
                    }
                    ImGui.EndMenu();
                }
                ImGui.Separator();
                if (Theme.IconMenuItem(Icons.Cut, "Cut", node != Utf.Root))
                {
                    parent.Children.Remove(node);
                    main.SetClipboardArray(UtfClipboard.ToBytes(node));
                }
                if (Theme.IconMenuItem(Icons.Copy, "Copy", node != Utf.Root))
                {
                    main.SetClipboardArray(UtfClipboard.ToBytes(node));
                }
                if (canPaste)
                {
                    if (Theme.BeginIconMenu(Icons.Paste, "Paste"))
                    {
                        if (ImGui.MenuItem("Before", node != Utf.Root))
                        {
                            var cpy = UtfClipboard.FromBytes(main.GetClipboardArray());
                            if (cpy != null)
                            {
                                cpy.Parent = parent;
                                parent.Children.Insert(parent.Children.IndexOf(node), cpy);
                            }
                        }
                        if (ImGui.MenuItem("After", node != Utf.Root))
                        {
                            var cpy = UtfClipboard.FromBytes(main.GetClipboardArray());
                            if (cpy != null)
                            {
                                cpy.Parent = parent;
                                parent.Children.Insert(parent.Children.IndexOf(node) + 1, cpy);
                            }
                        }
                        if (ImGui.MenuItem("Into"))
                        {
                            ConfirmIf(node.Data != null, "Adding children will delete this node's data. Continue?", () =>
                            {
                                node.Data = null;
                                if (node.Children == null) node.Children = new List<LUtfNode>();
                                var cpy = UtfClipboard.FromBytes(main.GetClipboardArray());
                                if (cpy != null)
                                {
                                    cpy.Parent = node;
                                    node.Children.Add(cpy);
                                }
                            });
                        }
                        ImGui.EndMenu();
                    }
                }
                else
                {
                    Theme.IconMenuItem(Icons.Paste, "Paste", false);
                }
                ImGui.Separator();

                if (Theme.IconMenuItem(Icons.List, "Sort Children", node.Children is { Count: > 1 }))
                {
                    node.Children.Sort((x, y) => x.Name.CompareTo(y.Name));
                }
                ImGui.Separator();

                if (Theme.IconMenuItem(Icons.SquareCorners, "Expand Children", node.Children is { Count: > 1 }))
                {
                    nodeOpenState[node] = true;
                    foreach (var c in node.Children)
                        nodeOpenState[c] = true;
                }
                if (Theme.IconMenuItem(Icons.SquareCornersInverted, "Collapse Children", node.Children is { Count: > 1 }))
                {
                    foreach (var c in node.Children)
                        nodeOpenState[c] = false;
                }
                ImGui.EndPopup();
            }
        }

        static unsafe bool AcceptDragDropPayload(string type, ImGuiDragDropFlags flags, out ImGuiPayloadPtr ptr)
        {
            return (ptr = ImGui.AcceptDragDropPayload(type, flags)) != null;
        }

        private int[] dragDropBuffer = new int[256];
        Span<int> GetDragDropPath(LUtfNode node)
        {
            LUtfNode n = node;
            int idx = 0;
            dragDropBuffer[idx++] = (int)(Unique & uint.MaxValue);
            dragDropBuffer[idx++] = (int)(Unique >> 32);
            while (n != Utf.Root)
            {
                var x = n.Parent.Children.IndexOf(n);
                if (x == -1)
                    throw new Exception("Parent not set correctly, invalid internal state");
                dragDropBuffer[idx++] = x;
                n = n.Parent;
            }
            dragDropBuffer.AsSpan().Slice(2, idx - 2).Reverse();
            return dragDropBuffer.AsSpan().Slice(0, idx);
        }

        unsafe (UtfTab, LUtfNode) GetDragDropNode(IntPtr data, int dataLen)
        {
            var payload = new Span<int>((void*)data, dataLen / sizeof(int));
            long tabId = payload[1];
            tabId <<= 32;
            tabId |= (uint)payload[0];
            var tab = main.TabControl.Tabs.OfType<UtfTab>().FirstOrDefault(x => x.Unique == tabId);
            if (tab == null)
                throw new Exception("Dragged from closed tab?");
            var n = tab.Utf.Root;
            for (int i = 2; i < payload.Length; i++)
                n = n.Children[payload[i]];
            return (tab, n);
        }

        bool DragDropAllowed(LUtfNode sourceNode, LUtfNode targetNode)
        {
            LUtfNode n = targetNode;
            while (n.Parent != null)
            {
                if (n.Parent == sourceNode)
                    return false;
                n = n.Parent;
            }
            return true;
        }

        void DropTarget(LUtfNode parent, LUtfNode sibling, string id)
        {
            if (main.DrawDragTargets)
            {
                ImGui.PushID($"{id};dropTarget;{sibling?.Name ?? "///NULL"}");
                ImGui.Separator();
                ImGui.SeparatorEx(ImGuiSeparatorFlags.Horizontal | ImGuiSeparatorFlags.SpanAllColumns, 3);
                if (ImGui.BeginDragDropTarget())
                {
                    if (AcceptDragDropPayload("_UTFNODE", ImGuiDragDropFlags.None, out var ptr))
                    {
                        var (sourceTab, sourceNode) = GetDragDropNode(ptr.Data, ptr.DataSize);
                        if (DragDropAllowed(sourceNode, parent))
                        {
                            if (sourceNode != sibling)
                            {
                                dropAction = () =>
                                {
                                    sourceNode.Parent.Children.Remove(sourceNode);
                                    sourceNode.Parent = parent;
                                    if (sibling == null)
                                        parent.Children.Add(sourceNode);
                                    else
                                        parent.Children.Insert(parent.Children.IndexOf(sibling), sourceNode);
                                    sourceTab.selectedNode = null;
                                };
                            }
                        }
                    }
                    if (AcceptDragDropPayload("_UTFNODE",
                            ImGuiDragDropFlags.AcceptBeforeDelivery | ImGuiDragDropFlags.AcceptNoPreviewTooltip,
                            out ptr))
                    {
                        var (_, sourceNode) = GetDragDropNode(ptr.Data, ptr.DataSize);
                        if (!DragDropAllowed(sourceNode, parent))
                        {
                            ImGui.SetTooltip("Cannot move parent to child");
                            ImGui.SetMouseCursor(ImGuiMouseCursor.NotAllowed);
                        }
                    }
                    ImGui.EndDragDropTarget();
                }
                ImGui.PopID();
            }
        }

        unsafe void DoNode(LUtfNode node, LUtfNode parent, bool isRoot = false)
        {
            string id = ImGuiExt.IDWithExtra(node.Name, node.InterfaceID);

            // Root has special drop behaviour — do NOT use normal parent/child DropTarget for root
            if (!isRoot)
                DropTarget(parent, node, id);

            bool empty = node.Data == null && node.Children == null;

            ImGuiTreeNodeFlags flags =
                (node == selectedNode ? ImGuiTreeNodeFlags.Selected : 0) |
                (node.Children == null ? (ImGuiTreeNodeFlags.Bullet | ImGuiTreeNodeFlags.Leaf) : 0) |
                tflags;

            // Apply pending expand/collapse request (one-frame only)
            if (nodeOpenState.TryGetValue(node, out bool forcedOpen))
            {
                ImGui.SetNextItemOpen(forcedOpen, ImGuiCond.Always);
                nodeOpenState.Remove(node); // consume command
            }

            // Render node
            if (empty)
                ImGui.PushStyleColor(ImGuiCol.Text, Color4.Orange);

            bool isOpen = ImGui.TreeNodeEx(id, flags);

            if (empty)
            {
                ImGui.PopStyleColor();
                ImGui.SetItemTooltip("Node is empty and cannot be saved. Add data or children");
            }

            // Selection
            if (ImGui.IsItemClicked(0))
                selectedNode = node;

            // Drag-drop handling
            // ROOT: cannot be dragged; children can be dropped ONTO root
            if (isRoot)
            {
                HandleRootDropTarget(id);
            }
            else
            {
                // --- Normal drop target (node accepts children) ---
                if (ImGui.BeginDragDropTarget())
                {
                    ImGuiPayloadPtr ptr;

                    // Accept move-into
                    if (AcceptDragDropPayload("_UTFNODE", ImGuiDragDropFlags.None, out ptr))
                    {
                        var (sourceTab, sourceNode) = GetDragDropNode(ptr.Data, ptr.DataSize);

                        if (DragDropAllowed(sourceNode, node))
                        {
                            Action act = () =>
                            {
                                sourceNode.Parent.Children.Remove(sourceNode);
                                sourceNode.Parent = node;

                                node.Data = null;
                                node.Children ??= new List<LUtfNode>();
                                node.Children.Insert(0, sourceNode);

                                sourceTab.selectedNode = null;
                            };

                            if (node.Data != null)
                                Confirm("Adding children will delete this node's data. Continue?",
                                    () => dropAction = act);
                            else
                                dropAction = act;
                        }
                    }

                    // Previews for invalid move
                    if (AcceptDragDropPayload("_UTFNODE",
                            ImGuiDragDropFlags.AcceptBeforeDelivery | ImGuiDragDropFlags.AcceptNoPreviewTooltip,
                            out ptr))
                    {
                        var (_, sourceNode) = GetDragDropNode(ptr.Data, ptr.DataSize);

                        if (!DragDropAllowed(sourceNode, node))
                        {
                            ImGui.SetTooltip("Can't move parent into child");
                            ImGui.SetMouseCursor(ImGuiMouseCursor.NotAllowed);
                        }
                    }

                    ImGui.EndDragDropTarget();
                }

                // --- Drag source (root cannot be dragged) ---
                if (ImGui.BeginDragDropSource())
                {
                    var path = GetDragDropPath(node);
                    fixed (int* buffer = &path.GetPinnableReference())
                        ImGui.SetDragDropPayload("_UTFNODE", (IntPtr)buffer, (IntPtr)(path.Length * sizeof(int)));

                    ImGui.Text(node.Name);
                    ImGui.EndDragDropSource();
                }
            }

            if (node.ResolvedName != null)
            {
                ImGui.SameLine();
                ImGui.TextDisabled("(" + ImGuiExt.IDSafe(node.ResolvedName) + ")");
            }

            // Context menu
            ImGui.PushID(id);
            DoNodeMenu(id, node, parent);
            ImGui.PopID();

            if (isOpen && node.Children != null)
            {
                foreach (var child in node.Children)
                    DoNode(child, node);

                if (!isRoot)
                    DropTarget(node, null, id);
            }

            if (isOpen)
                ImGui.TreePop();
        }
        unsafe void HandleRootDropTarget(string id)
        {
            if (ImGui.BeginDragDropTarget())
            {
                if (AcceptDragDropPayload("_UTFNODE", ImGuiDragDropFlags.None, out var ptr))
                {
                    var (sourceTab, sourceNode) = GetDragDropNode(ptr.Data, ptr.DataSize);

                    // Convert into a root-level node
                    dropAction = () =>
                    {
                        sourceNode.Parent.Children.Remove(sourceNode);
                        sourceNode.Parent = Utf.Root;
                        Utf.Root.Children.Insert(0, sourceNode);

                        sourceTab.selectedNode = null;
                    };
                }
                ImGui.EndDragDropTarget();
            }
        }

        void ExpandRecursive(LUtfNode node)
        {
            nodeOpenState[node] = true;

            if (node.Children == null) return;

            foreach (var c in node.Children)
                ExpandRecursive(c);
        }

        void CollapseRecursive(LUtfNode node)
        {
            nodeOpenState[node] = false;

            if (node.Children == null) return;

            foreach (var c in node.Children)
                CollapseRecursive(c);
        }

    }


}
