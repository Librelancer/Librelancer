using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
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
        public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.Modal;
        public override Vector2 InitSize => new Vector2(450, 550);

        static readonly float LABEL_WIDTH = 125f;
        static readonly float BUTTON_WIDTH = 110f;
        static readonly float SQAURE_BUTTON_WIDTH = 30f;
        static readonly int MAX_LODS = 9;

        // TXM state
        string filename = "";
        string nodeName = "";
        string[] lodPaths = new string[MAX_LODS];
        byte[][] lodData = new byte[MAX_LODS][];
        int selectedIndex = -1;

        int texWidth = 256;
        int texHeight = 256;
        int gridSizeX = 4;
        int gridSizeY = 4;
        int textureCount = 1;
        int frameCount = 16;
        int fps = 30;

        Queue<(string path, int mipIndex)> pendingImports = new();

        // Texture import popup state
        bool showTextureImportPopup = false;
        bool importProcessing = false;

        AnalyzedTexture importSource;
        ImTextureRef importTextureId;
        int importMipIndex = -1;

        bool importFlip = false;
        MipmapMethod importMipmaps = MipmapMethod.Lanczos4;
        DDSFormat importFormat = DDSFormat.Uncompressed;

        bool rememberImportSettings = false;
        bool hasCapturedImportSettings = false;
        bool requestOpenTextureImportPopup = false;

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
            if (requestOpenTextureImportPopup)
            {
                ImGui.OpenPopup("Import Texture");
                requestOpenTextureImportPopup = false;
            }
            DrawTextureImportPopup();
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
                                if (Path.Exists(path))
                                {
                                    lodPaths[i] = path;
                                    pendingImports.Enqueue((path, i));
                                    TryStartNextImport();
                                }
                                break;
                            }
                        }
                    }
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
        void DrawTextureImportPopup()
        {
            if (!showTextureImportPopup)
                return;

            if (ImGui.BeginPopupModal(
                "Import Texture",
                ref showTextureImportPopup,
                ImGuiWindowFlags.AlwaysAutoResize))
            {
                if (importProcessing)
                {
                    ImGuiExt.Spinner(
                        "##spinner",
                        10,
                        2,
                        ImGui.GetColorU32(ImGuiCol.ButtonHovered, 1));
                    ImGui.SameLine();
                    ImGui.Text("Processing...");
                }
                else
                {
                    DrawTextureImportSettings();
                }

                ImGui.EndPopup();
            }
        }
        void DrawTextureImportSettings()
        {
            var sz = 128 * ImGuiHelper.Scale;
            var wsz = ImGui.GetWindowWidth();
            if (wsz > sz)
                ImGui.SameLine((wsz - sz) / 2);

            // Preview
            ImGui.Image(importTextureId, new Vector2(sz), new Vector2(0, 1), new Vector2(1, 0));
            ImGui.Text($"Size: {importSource.Texture.Width}x{importSource.Texture.Height}");

            // Source type info
            if (importSource.Type == TexLoadType.Opaque)
                ImGui.Text("Source is RGB");
            else if (importSource.Type == TexLoadType.Alpha)
                ImGui.Text(importSource.OneBitAlpha
                    ? "Source is RGBA (1-bit alpha)"
                    : "Source is RGBA (8-bit alpha)");

            ImGui.Separator();

            // -------- Format combo --------
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Format");
            ImGui.SameLine();

            if (ImGui.BeginCombo("##format", FormatName(importFormat)))
            {
                if (ImGui.Selectable(FormatName(DDSFormat.Uncompressed), importFormat == DDSFormat.Uncompressed))
                    importFormat = DDSFormat.Uncompressed;

                if (ImGui.Selectable(FormatName(DDSFormat.DXT1), importFormat == DDSFormat.DXT1))
                    importFormat = DDSFormat.DXT1;

                if (importSource.OneBitAlpha &&
                    ImGui.Selectable(FormatName(DDSFormat.DXT1a), importFormat == DDSFormat.DXT1a))
                    importFormat = DDSFormat.DXT1a;

                if (!importSource.OneBitAlpha &&
                    importSource.Type == TexLoadType.Alpha &&
                    ImGui.Selectable(FormatName(DDSFormat.DXT5), importFormat == DDSFormat.DXT5))
                    importFormat = DDSFormat.DXT5;

                if (ImGui.Selectable(FormatName(DDSFormat.RGTC2), importFormat == DDSFormat.RGTC2))
                    importFormat = DDSFormat.RGTC2;

                if (ImGui.Selectable(FormatName(DDSFormat.MetallicRGTC1), importFormat == DDSFormat.MetallicRGTC1))
                    importFormat = DDSFormat.MetallicRGTC1;

                if (ImGui.Selectable(FormatName(DDSFormat.RoughnessRGTC1), importFormat == DDSFormat.RoughnessRGTC1))
                    importFormat = DDSFormat.RoughnessRGTC1;

                ImGui.EndCombo();
            }

            // -------- Mipmaps combo --------
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Mipmaps");
            ImGui.SameLine();

            if (ImGui.BeginCombo("##mipmaps", importMipmaps.ToString()))
            {
                if (ImGui.Selectable(nameof(MipmapMethod.None), importMipmaps == MipmapMethod.None))
                    importMipmaps = MipmapMethod.None;
                if (ImGui.Selectable(nameof(MipmapMethod.Box), importMipmaps == MipmapMethod.Box))
                    importMipmaps = MipmapMethod.Box;
                if (ImGui.Selectable(nameof(MipmapMethod.Tent), importMipmaps == MipmapMethod.Tent))
                    importMipmaps = MipmapMethod.Tent;
                if (ImGui.Selectable(nameof(MipmapMethod.Lanczos4), importMipmaps == MipmapMethod.Lanczos4))
                    importMipmaps = MipmapMethod.Lanczos4;
                if (ImGui.Selectable(nameof(MipmapMethod.Mitchell), importMipmaps == MipmapMethod.Mitchell))
                    importMipmaps = MipmapMethod.Mitchell;
                if (ImGui.Selectable(nameof(MipmapMethod.Kaiser), importMipmaps == MipmapMethod.Kaiser))
                    importMipmaps = MipmapMethod.Kaiser;

                ImGui.EndCombo();
            }

            ImGui.Separator();

            // -------- Flags --------
            ImGui.Checkbox("Flip Vertically", ref importFlip);
            ImGui.Checkbox("Remember these settings", ref rememberImportSettings);

            ImGui.Spacing();

            // -------- Actions --------
            if (ImGui.Button("Import"))
            {
                importProcessing = true;

                Task.Run(() =>
                {
                    byte[] data;

                    if (importMipmaps == MipmapMethod.None &&
                        importFormat == DDSFormat.Uncompressed)
                    {
                        data = TextureImport.TGANoMipmap(importSource.Source, importFlip);
                    }
                    else
                    {
                        data = TextureImport.CreateDDS(
                            importSource.Source,
                            importFormat,
                            importMipmaps,
                            true,
                            importFlip);
                    }

                    win.QueueUIThread(() =>
                    {
                        lodData[importMipIndex] = data;
                        hasCapturedImportSettings |= rememberImportSettings;

                        ImGuiHelper.DeregisterTexture(importSource.Texture);
                        importSource.Texture.Dispose();

                        CloseTextureImportPopup();
                        ImGui.CloseCurrentPopup();
                        TryStartNextImport();
                    });
                });
            }

            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                ImGuiHelper.DeregisterTexture(importSource.Texture);
                importSource.Texture.Dispose();

                CloseTextureImportPopup();
                ImGui.CloseCurrentPopup();
                TryStartNextImport();
            }
        }

        EditableUtf GenerateUtfFileTemplate()
        {
            var rv = new EditableUtf();
            var textureLibraryNode = new LUtfNode() { Name = "Texture LIbrary", Children = new List<LUtfNode>() };

            var _nodeName = String.IsNullOrWhiteSpace(nodeName)
                    ? "UntitledAnim"
                    : nodeName;

            var mipsNode = new LUtfNode() { Name = $"{_nodeName}_0", Children = new List<LUtfNode>() };
            for (int i = MAX_LODS - 1; i >= 0; i--)
            {
                var node = new LUtfNode() { Name = $"MIP{i.ToString()}" };
                if (lodData[i] != null)
                {
                    node.Data = lodData[i];
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

        void ImportTexture(string path, int mipIndex)
        {
            var src = TextureImport.OpenBuffer(File.ReadAllBytes(path), win.RenderContext);
            if (src.IsError)
            {
                win.ResultMessages(src);
                return;
            }

            if (src.Data.Type == TexLoadType.DDS)
            {
                src.Data.Texture.Dispose();
                lodData[mipIndex] = File.ReadAllBytes(path);
                return;
            }

            if (rememberImportSettings && hasCapturedImportSettings)
            {
                AutoImportTexture(src.Data, mipIndex);
                return;
            }

            importSource = src.Data;
            importMipIndex = mipIndex;

            if (!hasCapturedImportSettings)
            {
                importFlip = false;
                importMipmaps = MipmapMethod.Lanczos4;
                importFormat = importSource.OneBitAlpha ? DDSFormat.DXT1a :
                               importSource.Type == TexLoadType.Alpha ? DDSFormat.DXT5 :
                               DDSFormat.DXT1;
            }

            importTextureId = ImGuiHelper.RegisterTexture(importSource.Texture);
            showTextureImportPopup = true;
            requestOpenTextureImportPopup = true;
        }

        void AutoImportTexture(AnalyzedTexture src, int mip)
        {
            Task.Run(() =>
            {
                var data = TextureImport.CreateDDS(
                    src.Source,
                    importFormat,
                    importMipmaps,
                    true,
                    importFlip);

                win.QueueUIThread(() =>
                {
                    lodData[mip] = data;
                    src.Texture.Dispose();
                    TryStartNextImport();
                });
            });
        }

        void CloseTextureImportPopup()
        {
            showTextureImportPopup = false;
            requestOpenTextureImportPopup = false;
            importProcessing = false;

            importSource = null;
            importMipIndex = -1;
        }

        void TryStartNextImport()
        {
            // Already importing or popup open → wait
            if (showTextureImportPopup || importProcessing)
                return;

            if (pendingImports.Count == 0)
                return;

            var (path, mipIndex) = pendingImports.Dequeue();
            ImportTexture(path, mipIndex);
        }

        static string FormatName(DDSFormat fmt) => fmt switch
        {
            DDSFormat.Uncompressed => "Uncompressed",
            DDSFormat.DXT1 => "DXT1 (Opaque)",
            DDSFormat.DXT1a => "DXT1A (1-bit Alpha)",
            DDSFormat.DXT3 => "DXT3",
            DDSFormat.DXT5 => "DXT5 (8-bit Alpha)",
            DDSFormat.RGTC2 => "Normal Map",
            DDSFormat.MetallicRGTC1 => "Metallic Map (B channel)",
            DDSFormat.RoughnessRGTC1 => "Roughness Map (G channel)",
            _ => fmt.ToString()
        };

    }
}
