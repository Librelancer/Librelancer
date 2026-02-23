// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Globalization;
using System.IO;
using System.Xml.Serialization;
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
        [Entry("msaa")]
        public int MSAA = 0;
        [Entry("lod_multiplier")]
        public float LodMultiplier = 1.3f;
        [Entry("debug")]
        public bool Debug = false;

        float IRendererSettings.LodMultiplier => LodMultiplier;

        int IRendererSettings.SelectedAnisotropy => Anisotropy;
        TextureFiltering IRendererSettings.SelectedFiltering =>
            Anisotropy == 0 ? TextureFiltering.Trilinear : TextureFiltering.Anisotropic;
        int IRendererSettings.SelectedMSAA => MSAA;

        public int[] AnisotropyLevels() => RenderContext.GetAnisotropyLevels();
        public int MaxMSAA() => RenderContext.MaxSamples;

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
            writer.WriteLine($"msaa = {MSAA}");
            writer.WriteLine($"lod_multiplier = {Fmt(LodMultiplier)}");
            writer.WriteLine($"debug = {(Debug ? "true" : "false")}");
        }

        [WattleScriptHidden]
        public RenderContext RenderContext;

        [WattleScriptHidden]
        public GameSettings MakeCopy()
        {
            var gs = new GameSettings();
            gs.MasterVolume = MasterVolume;
            gs.SfxVolume = SfxVolume;
            gs.InterfaceVolume = InterfaceVolume;
            gs.VoiceVolume = VoiceVolume;
            gs.MusicVolume = MusicVolume;

            gs.FullScreen = FullScreen;

            gs.VSync = VSync;
            gs.Anisotropy = Anisotropy;
            gs.MSAA = MSAA;
            gs.LodMultiplier = LodMultiplier;
            gs.RenderContext = RenderContext;
            gs.Debug = Debug;
            return gs;
        }

        public void Validate()
        {
            if (MSAA > RenderContext.MaxSamples)
            {
                FLLog.Info("Config", $"{MSAA}x MSAA not supported, disabling.");
                MSAA = 0;
            }
            if (Anisotropy > RenderContext.MaxAnisotropy)
            {
                FLLog.Info("Config", $"{Anisotropy}x anisotropy not supported, disabling.");
                Anisotropy = 0;
            }
        }
    }
}
