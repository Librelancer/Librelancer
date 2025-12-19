using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using BepuPhysics.Constraints;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.Dialogs;
using LibreLancer.ImUI;

namespace LancerEdit.Utf.Popups
{
    public class NewTxmPopup : PopupWindow
    {
        Action<(string name, EditableUtf utf)> action;
        readonly MainWindow win;

        public NewTxmPopup(MainWindow win, Action<(string name, EditableUtf utf)> action)
        {
            this.action = action;
            this.win = win;
        }

        public override string Title { get; set; } = "Create new .txm Document";
        public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings;
        public override Vector2 InitSize => new Vector2(450, 550);

        static readonly float LABEL_WIDTH = 125f;
        static readonly float BUTTON_WIDTH = 110f;
        static readonly float SQAURE_BUTTON_WIDTH = 30f;
        static readonly int MAX_LODS = 9;

        // ui state vars
        string filename = "";
        string nodeName = "";
        string[] lodPaths = new string[MAX_LODS];
        int selectedIndex = -1;
        int texWidth = 256;
        int texHeight = 256;
        int gridSizeX = 4;
        int gridSizeY = 4;
        int textureCount = 1;
        int frameCount = 16;
        int fps = 30;

        bool isError = false;

        public override void Draw(bool appearing)
        {
            if (!ImGui.CollapsingHeader("File Metadata", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Leaf))
                return;
            DrawFileMetadataFields();

            if (!ImGui.CollapsingHeader("Import MIPS", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Leaf))
                return;
            DrawMipsFileSelect();

            if (!ImGui.CollapsingHeader("Animation And Frame Rect Settings", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Leaf))
                return;
            DrawAnimFields();

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.Spacing();

            if (ImGui.Button("Create", new Vector2(BUTTON_WIDTH, 0)))
            {
                var utf = GenerateUtfFileTemplate();

                var _filename = String.IsNullOrWhiteSpace(filename)
                    ? "Untitled"
                    : filename;

                action((_filename, utf));
                ImGui.CloseCurrentPopup();
            }
        }

        private EditableUtf GenerateUtfFileTemplate()
        {
            var rv = new EditableUtf();
            var textureLibraryNode = new LUtfNode() { Name = "Texture LIbrary", Children = new List<LUtfNode>() };

            var _nodeName = String.IsNullOrWhiteSpace(nodeName)
                    ? "UntitledAnim"
                    : nodeName;

            var mipsNode = new LUtfNode() { Name = $"{_nodeName}_0", Children = new List<LUtfNode>() };
            for (int i = lodPaths.Count() - 1; i >= 0; i--)
            {
                var node = new LUtfNode() { Name = $"MIP{i.ToString()}", Data = new byte[0] };
                if (lodPaths[i] != null)
                {
                    // TODO Write import login - review how this is done in TextureImportPopup - we will
                    // likely need to tie into this / probably calling for each file to import 
                }
                mipsNode.Children.Add(node);
            }
            textureLibraryNode.Children.Add(mipsNode);

            var TexRectNode = new LUtfNode() { Name = $"{_nodeName}", Children = new List<LUtfNode>() };
            TexRectNode.Children.Add(LUtfNode.IntNode(TexRectNode, "Texture count", textureCount));
            TexRectNode.Children.Add(LUtfNode.IntNode(TexRectNode, "Frame count", frameCount));
            TexRectNode.Children.Add(LUtfNode.IntNode(TexRectNode, "FPS", fps));

            TexRectNode.Children.Add(new LUtfNode()
            {
                Name = "Frame rects",
                Data = BuildFrameRects(texWidth, texHeight, gridSizeX, gridSizeY, frameCount)
            });
            textureLibraryNode.Children.Add(TexRectNode);

            rv.Root.Children.Add(textureLibraryNode);

            return rv;

        }

        void DrawAnimFields()
        {
            ImGui.Spacing();
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Texture size"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
            unsafe
            {
                fixed (int* v = &texWidth)
                {
                    // v[0] -> texWidth
                    // v[1] -> texHeight
                    v[1] = texHeight;

                    if (ImGui.InputInt2("##TextureSize", v, ImGuiInputTextFlags.CharsDecimal))
                    {
                        texWidth = v[0];
                        texHeight = v[1];
                    }
                }
            }
            ImGui.Text("Texture count"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
            ImGui.InputInt("##txtCount", ref textureCount, 1, 10, ImGuiInputTextFlags.CharsDecimal);

            ImGui.Text("Frame count"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
            ImGui.InputInt("##frameCount", ref frameCount, 1, 10, ImGuiInputTextFlags.CharsDecimal);

            ImGui.Text("FPS"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
            ImGui.InputInt("##FPS", ref fps, 1, 10, ImGuiInputTextFlags.CharsDecimal);

            ImGui.Text("Grid size"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
            unsafe
            {
                fixed (int* v = &gridSizeX)
                {
                    // v[0] -> texWidth
                    // v[1] -> texHeight
                    v[1] = gridSizeY;

                    if (ImGui.InputInt2("##gridSize", v, ImGuiInputTextFlags.CharsDecimal))
                    {
                        texWidth = v[0];
                        texHeight = v[1];
                    }
                }
            }
            ImGui.Spacing();
        }

        void DrawMipsFileSelect()
        {
            ImGui.Spacing();
            if (ImGui.BeginTable(
                "##LODTable",
                2,
                ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg,
                new Vector2(ImGui.GetContentRegionAvail().X - 40 * ImGuiHelper.Scale, 200)))

            {
                ImGui.TableSetupColumn("MIP", ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableSetupColumn("File", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableHeadersRow();

                for (int i = 0; i < MAX_LODS; i++)
                {
                    ImGui.TableNextRow();
                    ImGui.PushID(i);

                    bool selected = (selectedIndex == i);

                    // ---- Column 0: MIPS index ----
                    ImGui.TableSetColumnIndex(0);
                    if (ImGui.Selectable($"MIPS{i}", selected, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.NoAutoClosePopups))
                    {
                        selectedIndex = i;
                    }

                    // ---- Column 1: filename or empty ----
                    ImGui.TableSetColumnIndex(1);
                    if (lodPaths[i] != null)
                    {
                        ImGui.Text(Path.GetFileName(lodPaths[i]));
                    }
                    else
                    {
                        ImGui.TextDisabled("<empty>");
                    }

                    ImGui.PopID();
                }

                ImGui.EndTable();
            }

            ImGui.SameLine();
            ImGui.BeginChild(
                "##buttons",
                new Vector2(SQAURE_BUTTON_WIDTH + 16, 200),
                ImGuiChildFlags.None);

            ImGui.BeginDisabled(selectedIndex < 0);

            if (ImGui.Button(Icons.ArrowUp.ToString(), new Vector2(SQAURE_BUTTON_WIDTH)))
            {
                if (selectedIndex > 0)
                {
                    (lodPaths[selectedIndex - 1], lodPaths[selectedIndex]) =
                        (lodPaths[selectedIndex], lodPaths[selectedIndex - 1]);

                    selectedIndex--;
                }
            }

            ImGui.Spacing();

            if (ImGui.Button(Icons.ArrowDown.ToString(), new Vector2(SQAURE_BUTTON_WIDTH)))
            {
                if (selectedIndex < MAX_LODS - 1)
                {
                    (lodPaths[selectedIndex + 1], lodPaths[selectedIndex]) =
                        (lodPaths[selectedIndex], lodPaths[selectedIndex + 1]);

                    selectedIndex++;
                }
            }
            ImGui.EndDisabled();

            ImGui.BeginDisabled(!lodPaths.Any(p => p == null));
            ImGui.Dummy(new Vector2(ImGui.GetFrameHeightWithSpacing() * 2));
            if (ImGui.Button(Icons.SquarePlus.ToString(), new Vector2(SQAURE_BUTTON_WIDTH)))
            {
                win.QueueUIThread(() =>
                {
                    FileDialog.OpenMultiple(paths =>
                    {
                        if (paths == null || paths.Length == 0)
                        {
                            return;
                        }

                        foreach (var path in paths)
                        {
                            // Skip duplicates
                            if (lodPaths.Contains(path))
                                continue;

                            // Find first empty slot
                            for (int i = 0; i < MAX_LODS; i++)
                            {
                                if (lodPaths[i] == null)
                                {
                                    lodPaths[i] = path;
                                    break;
                                }
                            }
                        }
                    });
                });
            }
            ImGui.EndDisabled();

            ImGui.Spacing();

            ImGui.BeginDisabled(selectedIndex < 0);
            if (ImGui.Button(Icons.TrashAlt.ToString(), new Vector2(SQAURE_BUTTON_WIDTH)))
            {
                lodPaths[selectedIndex] = null;
                selectedIndex = -1;
            }
            ImGui.EndDisabled();
            ImGui.EndChild();
            ImGui.Spacing();
        }

        void DrawFileMetadataFields()
        {
            ImGui.Spacing();
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Filename"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);

            ImGui.AlignTextToFramePadding();
            ImGui.PushItemWidth(-1);
            ImGui.InputTextWithHint("##filename", "Untitled", ref filename, 4068);
            ImGui.PopItemWidth();

            ImGui.AlignTextToFramePadding();
            ImGui.Text("Node name"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
            ImGui.PushItemWidth(-1);
            ImGui.InputTextWithHint("##nodeName", "UntitledAnim", ref nodeName, 4068);
            ImGui.PopItemWidth();
            ImGui.Spacing();
        }

        
        byte[] BuildFrameRects(int texWidth, int texHeight, int gridSizeX, int gridSizeY, int frameCount)
        {
            byte[] data = new byte[frameCount * 20];

            float cellU = 1f / gridSizeX;
            float cellV = 1f / gridSizeY;

            int frame = 0;

            for (int y = 0; y < gridSizeY; y++)
            {
                for (int x = 0; x < gridSizeX; x++)
                {
                    if (frame >= frameCount)
                        break;

                    float u0 = x * cellU;
                    float v0 = y * cellV;
                    float u1 = u0 + cellU;
                    float v1 = v0 + cellV;

                    WriteFrameRect(
                        data,
                        frame * 20,
                        u0, v0,
                        u1, v1
                    );

                    frame++;
                }
            }

            return data;
        }
        static void WriteFrameRect(Span<byte> buffer, int offset, float u0, float v0, float u1, float v1)
        {
            BitConverter.GetBytes(0u).CopyTo(buffer[offset..]);       // index
            BitConverter.GetBytes(u0).CopyTo(buffer[(offset + 4)..]);
            BitConverter.GetBytes(v0).CopyTo(buffer[(offset + 8)..]);
            BitConverter.GetBytes(u1).CopyTo(buffer[(offset + 12)..]);
            BitConverter.GetBytes(v1).CopyTo(buffer[(offset + 16)..]);
        }
    }
}
