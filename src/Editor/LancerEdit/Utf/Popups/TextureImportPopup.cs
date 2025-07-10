using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using ImGuiNET;
using LibreLancer.ContentEdit;
using LibreLancer.ImUI;

namespace LancerEdit.Utf.Popups;

public class TextureImportPopup : PopupWindow
{
    private Action<List<LUtfNode>> callback;
    private AnalyzedTexture source;
    private int textureId;

    private int step = 0;

    private bool flip = false;
    private MipmapMethod mipmaps = MipmapMethod.Lanczos4;
    private DDSFormat importFormat = DDSFormat.Uncompressed;
    public override bool NoClose => step > 0;
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize;


    private MainWindow win;

    public TextureImportPopup(AnalyzedTexture source, Action<List<LUtfNode>> onImported, MainWindow win)
    {
        this.source = source;
        this.callback = onImported;
        this.win = win;
        if (source.OneBitAlpha) {
            importFormat = DDSFormat.DXT1a;
        }
        else if (source.Type == TexLoadType.Alpha) {
            importFormat = DDSFormat.DXT5;
        }
        else {
            importFormat = DDSFormat.DXT1;
        }
        textureId = ImGuiHelper.RegisterTexture(source.Texture);
    }

    public override string Title { get; set; } = "Texture Import";

    public static string FormatName(DDSFormat fmt) => fmt switch
    {
        DDSFormat.Uncompressed => "Uncompressed",
        DDSFormat.DXT1 => "DXT1 (Opaque)",
        DDSFormat.DXT1a => "DXT1A (1-bit Alpha)",
        DDSFormat.DXT3 => "DXT3",
        DDSFormat.DXT5 => "DXT5 (8-bit Alpha)",
        DDSFormat.RGTC2 => "Normal Map",
        DDSFormat.MetallicRGTC1 => "Metallic Map (B channel)",
        DDSFormat.RoughnessRGTC1 => "Roughness Map (G channel)",
    };

    List<LUtfNode> Import()
    {
        if (mipmaps == MipmapMethod.None && importFormat == DDSFormat.Uncompressed)
        {
            var l = new List<LUtfNode>();
            l.Add(new LUtfNode() { Name = "MIP0", Data = TextureImport.TGANoMipmap(source.Source, flip)});
            return l;
        }
        if (importFormat == DDSFormat.Uncompressed)
        {
            return TextureImport.TGAMipmaps(source.Source, mipmaps, flip);
        }
        var dds = new List<LUtfNode>();
        dds.Add(new LUtfNode() { Name = "MIPS", Data = TextureImport.CreateDDS(source.Source, importFormat, mipmaps, true, flip)});
        return dds;
    }

    void ImportSettings()
    {
        var sz = 128 * ImGuiHelper.Scale;
        var wsz = ImGui.GetWindowWidth();
        if (wsz > sz) {
            ImGui.SameLine((wsz - sz) / 2);
        }
        ImGui.Image((IntPtr)textureId, new Vector2(sz),
            new Vector2(0, 1), new Vector2(1, 0), Vector4.One, Vector4.Zero);
        ImGui.Text(string.Format("Dimensions: {0}x{1}", source.Texture.Width, source.Texture.Height));
        if (source.Type == TexLoadType.Opaque)
        {
            ImGui.Text("Source is RGB");
        }
        if (source.Type == TexLoadType.Alpha)
        {
            ImGui.Text(source.OneBitAlpha ? "Source is RGBA (1-bit alpha)" : "Source is RGBA (8-bit alpha)");
        }
        // Formats, filtering by type
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Format");
        ImGui.SameLine();
        if (ImGui.BeginCombo("##formats", FormatName(importFormat)))
        {
            if (ImGui.Selectable(FormatName(DDSFormat.Uncompressed), importFormat == DDSFormat.Uncompressed)) {
                importFormat = DDSFormat.Uncompressed;
            }
            if (ImGui.Selectable(FormatName(DDSFormat.DXT1), importFormat == DDSFormat.DXT1)) {
                importFormat = DDSFormat.DXT1;
            }
            if (source.OneBitAlpha &&
                ImGui.Selectable(FormatName(DDSFormat.DXT1a), importFormat == DDSFormat.DXT1a))
            {
                importFormat = DDSFormat.DXT1a;
            }
            if (!source.OneBitAlpha &&
                source.Type == TexLoadType.Alpha &&
                ImGui.Selectable(FormatName(DDSFormat.DXT5), importFormat == DDSFormat.DXT5))
            {
                importFormat = DDSFormat.DXT5;
            }
            if (ImGui.Selectable(FormatName(DDSFormat.RGTC2), importFormat == DDSFormat.RGTC2)) {
                importFormat = DDSFormat.RGTC2;
            }
            if (ImGui.Selectable(FormatName(DDSFormat.MetallicRGTC1), importFormat == DDSFormat.MetallicRGTC1)) {
                importFormat = DDSFormat.MetallicRGTC1;
            }
            if (ImGui.Selectable(FormatName(DDSFormat.RoughnessRGTC1), importFormat == DDSFormat.RoughnessRGTC1)) {
                importFormat = DDSFormat.RoughnessRGTC1;
            }
            ImGui.EndCombo();
        }
        //Mipmaps
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Mipmaps");
        ImGui.SameLine();
        if (ImGui.BeginCombo("##mipmaps", mipmaps.ToString()))
        {
            if(ImGui.Selectable(nameof(MipmapMethod.None), mipmaps == MipmapMethod.None))
                mipmaps = MipmapMethod.None;
            if(ImGui.Selectable(nameof(MipmapMethod.Box), mipmaps == MipmapMethod.Box))
                mipmaps = MipmapMethod.Box;
            if(ImGui.Selectable(nameof(MipmapMethod.Tent), mipmaps == MipmapMethod.Tent))
                mipmaps = MipmapMethod.Tent;
            if(ImGui.Selectable(nameof(MipmapMethod.Lanczos4), mipmaps == MipmapMethod.Lanczos4))
                mipmaps = MipmapMethod.Lanczos4;
            if(ImGui.Selectable(nameof(MipmapMethod.Mitchell), mipmaps == MipmapMethod.Mitchell))
                mipmaps = MipmapMethod.Mitchell;
            if(ImGui.Selectable(nameof(MipmapMethod.Kaiser), mipmaps == MipmapMethod.Kaiser))
                mipmaps = MipmapMethod.Kaiser;
            ImGui.EndCombo();
        }

        ImGui.Checkbox("Flip Vertically", ref flip);
        if (ImGui.Button("Import"))
        {
            Task.Run(() =>
            {
                var result = Import();
                win.QueueUIThread(() =>
                {
                    callback(result);
                    step = 2;
                });
            });
            step = 1;
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
        {
            ImGui.CloseCurrentPopup();
        }
    }

    public override void Draw()
    {
        if (step == 2)
        {
            ImGui.CloseCurrentPopup();
        }
        else if (step == 1)
        {
            ImGuiExt.Spinner("##spinner", 10, 2, ImGui.GetColorU32(ImGuiCol.ButtonHovered, 1));
            ImGui.SameLine();
            ImGui.Text("Processing...");
        }
        else
        {
            ImportSettings();
        }
    }

    public override void OnClosed()
    {
        ImGuiHelper.DeregisterTexture(source.Texture);
        source.Texture.Dispose();
    }
}
