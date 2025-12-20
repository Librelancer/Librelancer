using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.Dialogs;
using LibreLancer.ImUI;

namespace LancerEdit
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
        public override Vector2 InitSize => new Vector2(450, 555);

        static readonly float LABEL_WIDTH = 125f;
        static readonly float BUTTON_WIDTH = 110f;
        readonly Vector4 ERROR_TEXT_COLOUR = new Vector4(1f, 0.3f, 0.3f, 1f);
        readonly Vector4 SUCCESS_TEXT_COLOUR = new Vector4(0f, 0.8f, 0.2f, 1f);

        // TXM state
        string filename = "";
        string nodeName = "";
        List<LUtfNode> importedMipNodes;

        Point texSize = new Point(256, 256);
        Point gridSize = new Point(4,4);
        int textureCount = 1;
        int frameCount = 16;
        int fps = 30;

        // Texture import popup state
        bool showTextureImportPopup = false;
        bool importProcessing = false;

        AnalyzedTexture importSource;
        ImTextureRef importTextureId;

        bool importFlip = false;
        MipmapMethod importMipmaps = MipmapMethod.Lanczos4;
        DDSFormat importFormat = DDSFormat.Uncompressed;

        bool requestOpenTextureImportPopup = false;

        bool isError = false;
        string errorText = "";

        public override void Draw(bool appearing)
        {
            if (!ImGui.CollapsingHeader("File Metadata", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Leaf))
                return;
            DrawFileMetadataFields();

            if (!ImGui.CollapsingHeader("Import Texture", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Leaf))
                return;
            DrawMipsFileSelect();

            if (!ImGui.CollapsingHeader("Animation And Frame Rect Settings", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Leaf))
                return;
            DrawAnimFields();

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.Spacing();

            ImGui.SetCursorPosX((ImGui.GetContentRegionAvail().X / 2) - (BUTTON_WIDTH / 2));

            if (ImGuiExt.Button("Create", importedMipNodes != null, new Vector2(BUTTON_WIDTH, 0)))
            {
                var utf = GenerateUtfFileTemplate();
                action((filename, utf));
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
            ImGui.InputInt2("##texSize", ref texSize, ImGuiInputTextFlags.None);

            ImGui.AlignTextToFramePadding();
            ImGui.Text("Texture count"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
            ImGui.InputInt("##txtCount", ref textureCount, 1, 10, ImGuiInputTextFlags.CharsDecimal);

            ImGui.AlignTextToFramePadding();
            ImGui.Text("Frame count"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
            ImGui.InputInt("##frameCount", ref frameCount, 1, 10, ImGuiInputTextFlags.CharsDecimal);

            ImGui.AlignTextToFramePadding();
            ImGui.Text("FPS"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
            ImGui.InputInt("##FPS", ref fps, 1, 10, ImGuiInputTextFlags.CharsDecimal);

            ImGui.AlignTextToFramePadding();
            ImGui.Text("Grid size"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
            ImGui.InputInt2("##gridSize", ref gridSize, ImGuiInputTextFlags.None);
            
            ImGui.Spacing();
        }
        void DrawMipsFileSelect()
        {
            ImGui.Spacing();

            if (ImGui.Button("Select texture", new Vector2(LABEL_WIDTH-20*ImGuiHelper.Scale, 0)))
            {
                FileDialog.Open(path =>
                {
                    isError = false;
                    if (path == null || path.Length == 0)
                    {
                        isError = true;
                        errorText = "No file selected";
                        return;
                    }

                    if (Path.Exists(path))
                    {
                        importedMipNodes = null;

                        var src = TextureImport.OpenBuffer(
                            File.ReadAllBytes(path),
                            win.RenderContext);

                        if (src.IsError)
                        {
                            isError = true;
                            errorText = "Failed to import file";
                            win.ResultMessages(src);
                            return;
                        }

                        // DDS = immediate import (no popup)
                        if (src.Data.Type == TexLoadType.DDS)
                        {
                            importedMipNodes = new List<LUtfNode>
                            {
                                new LUtfNode
                                {
                                    Name = "MIPS",
                                    Data = File.ReadAllBytes(path)
                                }
                            };
                            src.Data.Texture.Dispose();
                            isError = false;
                            errorText = "File imported";
                            return;
                        }

                        // Non-DDS → open import popup
                        importSource = src.Data;
                        importTextureId = ImGuiHelper.RegisterTexture(importSource.Texture);

                        importFlip = false;
                        importMipmaps = MipmapMethod.Lanczos4;
                        importFormat =
                            importSource.OneBitAlpha ? DDSFormat.DXT1a :
                            importSource.Type == TexLoadType.Alpha ? DDSFormat.DXT5 :
                            DDSFormat.DXT1;

                        showTextureImportPopup = true;
                        requestOpenTextureImportPopup = true;
                        
                    }
                });
            }
            ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(isError ? ERROR_TEXT_COLOUR : SUCCESS_TEXT_COLOUR, errorText );
            ImGui.Spacing();
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

                        importedMipNodes = ImportTextureAsNodes(
                            importSource,
                            importFormat,
                            importMipmaps,
                            importFlip);

                        ImGuiHelper.DeregisterTexture(importSource.Texture);
                        importSource.Texture.Dispose();
                        isError = false;
                        errorText = "File imported";
                        CloseTextureImportPopup();
                        ImGui.CloseCurrentPopup();
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
            }
        }

        EditableUtf GenerateUtfFileTemplate()
        {
            var rv = new EditableUtf();
            var textureLibraryNode = new LUtfNode() { Name = "Texture LIbrary", Children = new List<LUtfNode>(), Parent=rv.Root };

            var _nodeName = string.IsNullOrWhiteSpace(nodeName)
                    ? "UntitledAnim"
                    : nodeName;

            var mipsNode = new LUtfNode() { Name = $"{_nodeName}_0", Children = new List<LUtfNode>(), Parent=textureLibraryNode };
            var mipNodeChildren = importedMipNodes;
            mipNodeChildren.ForEach(e => {
                e.Parent = mipsNode;
                mipsNode.Children.Add(e);
            });  
            textureLibraryNode.Children.Add(mipsNode);
            
            var TexRectNode = new LUtfNode() { Name = $"{_nodeName}", Children = new List<LUtfNode>(), Parent= textureLibraryNode };
            TexRectNode.Children.Add(LUtfNode.IntNode(TexRectNode, "Texture count", textureCount));
            TexRectNode.Children.Add(LUtfNode.IntNode(TexRectNode, "Frame count", frameCount));
            TexRectNode.Children.Add(LUtfNode.IntNode(TexRectNode, "FPS", fps));
            var framRects = FrameRectCalculator.GenerateFrameRects(gridSize.X, gridSize.Y, frameCount);
            TexRectNode.Children.Add(new LUtfNode()
            {
                Name = "Frame rects",
                Data = FrameRectCalculator.SerializeFrameRects(framRects),
                Parent = TexRectNode
            });
            textureLibraryNode.Children.Add(TexRectNode);

            rv.Root.Children.Add(textureLibraryNode);

            return rv;
        }
        void CloseTextureImportPopup()
        {
            showTextureImportPopup = false;
            requestOpenTextureImportPopup = false;
            importProcessing = false;

            importSource = null;
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
        static List<LUtfNode> ImportTextureAsNodes(
            AnalyzedTexture source,
            DDSFormat format,
            MipmapMethod mipmaps,
            bool flip)
        {
            if (mipmaps == MipmapMethod.None && format == DDSFormat.Uncompressed)
            {
                return new()
                {
                    new LUtfNode
                    {
                        Name = "MIP0",
                        Data = TextureImport.TGANoMipmap(source.Source, flip)
                    }
                };
            }

            if (format == DDSFormat.Uncompressed)
                return TextureImport.TGAMipmaps(source.Source, mipmaps, flip);

            return new()
            {
                new LUtfNode
                {
                    Name = "MIPS",
                    Data = TextureImport.CreateDDS(
                        source.Source,
                        format,
                        mipmaps,
                        true,
                        flip)
                }
            };
        }
    }
}
