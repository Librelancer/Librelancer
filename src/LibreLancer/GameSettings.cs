// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Globalization;
using System.IO;
using LibreLancer.Data.Ini;
using LibreLancer.Graphics;
using LibreLancer.Render;
using WattleScript.Interpreter;

namespace LibreLancer
{
    [WattleScriptUserData]
    [ParsedSection]
    public partial class GameSettings : IRendererSettings
    {
        [Entry("master_volume")]
        public float MasterVolume = 1.0f;
        [Entry("sfx_volume")]
        public float SfxVolume = 1.0f;
        [Entry("voice_volume")]
        public float VoiceVolume = 1.0f;
        [Entry("interface_volume")]
        public float InterfaceVolume = 1.0f;
        [Entry("music_volume")]
        public float MusicVolume = 1.0f;

        [Entry("fullscreen")]
        public bool FullScreen = true;

        [Entry("vsync")]
        public bool VSync = true;
        [Entry("anisotropy")]
        public int Anisotropy = 0;
        [Entry("antialias")]
        public int Antialias = 0;
        [Entry("lod_multiplier")]
        public float LodMultiplier = 1.3f;
        [Entry("debug")]
        public bool Debug = false;
        [Entry("per_pixel_lighting")]
        public bool PerPixelLighting = true;

        float IRendererSettings.LodMultiplier => LodMultiplier;
        bool IRendererSettings.PerPixelLighting => PerPixelLighting;

        int IRendererSettings.SelectedAnisotropy => Anisotropy;
        TextureFiltering IRendererSettings.SelectedFiltering =>
            Anisotropy == 0 ? TextureFiltering.Trilinear : TextureFiltering.Anisotropic;

        AntialiasMode IRendererSettings.SelectedAA => (AntialiasMode)Antialias;

        public int[]? AnisotropyLevels() => RenderContext.GetAnisotropyLevels();
        public int MaxAALevel() => (int)RenderContext.MaxAntialias;

        [WattleScriptHidden]
        public void Write(TextWriter writer)
        {
            static string Fmt(float f) => f.ToString("F3", CultureInfo.InvariantCulture);
            writer.WriteLine("[Settings]");
            writer.WriteLine($"master_volume = {Fmt(MasterVolume)}");
            writer.WriteLine($"sfx_volume = {Fmt(SfxVolume)}");
            writer.WriteLine($"voice_volume = {Fmt(VoiceVolume)}");
            writer.WriteLine($"interface_volume = {Fmt(InterfaceVolume)}");
            writer.WriteLine($"music_volume = {Fmt(MusicVolume)}");

            writer.WriteLine($"fullscreen = {(FullScreen ? "true" : "false")}");

            writer.WriteLine($"vsync = {(VSync ? "true" : "false")}");
            writer.WriteLine($"anisotropy = {Anisotropy}");
            writer.WriteLine($"antialias = {Antialias}");
            writer.WriteLine($"lod_multiplier = {Fmt(LodMultiplier)}");
            writer.WriteLine($"debug = {(Debug ? "true" : "false")}");
            writer.WriteLine($"per_pixel_lighting = {(PerPixelLighting ? "true" : "false")}");
        }

        [WattleScriptHidden]
        public RenderContext RenderContext = null!;

        [WattleScriptHidden]
        public GameSettings MakeCopy()
        {
            var gs = new GameSettings
            {
                MasterVolume = MasterVolume,
                SfxVolume = SfxVolume,
                InterfaceVolume = InterfaceVolume,
                VoiceVolume = VoiceVolume,
                MusicVolume = MusicVolume,
                FullScreen = FullScreen,
                VSync = VSync,
                Anisotropy = Anisotropy,
                Antialias = Antialias,
                LodMultiplier = LodMultiplier,
                RenderContext = RenderContext,
                Debug = Debug,
                PerPixelLighting = PerPixelLighting
            };

            return gs;
        }

        public void Validate()
        {
            var mode = (AntialiasMode)Antialias;
            if (mode > RenderContext.MaxAntialias)
            {
                FLLog.Info("Config", $"{mode} not supported, disabling.");
                Antialias = 0;
            }
            if (Anisotropy > RenderContext.MaxAnisotropy)
            {
                FLLog.Info("Config", $"{Anisotropy}x anisotropy not supported, disabling.");
                Anisotropy = 0;
            }
        }
    }
}
