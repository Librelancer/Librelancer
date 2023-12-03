// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using LibreLancer;
using LibreLancer.Data;
using LibreLancer.Ini;
using LibreLancer.Render;
using static System.FormattableString;
namespace LancerEdit
{
    public enum CameraModes
    {
        Arcball,
        Walkthrough,
        Starsphere,
        Cockpit
    }

    [SelfSection("Config")]
    public class EditorConfiguration : IniFile, IRendererSettings
    {
        [Entry("msaa")]
        public int MSAA;
        [Entry("texture_filter")]
        public int TextureFilter;
        [Entry("view_buttons")]
        public bool ViewButtons;
        [Entry("pause_when_unfocused")]
        public bool PauseWhenUnfocused = true;
        [Entry("background_top")]
        public Color4 Background = Color4.CornflowerBlue * new Color4(0.3f, 0.3f, 0.3f, 1f);
        [Entry("background_bottom")]
        public Color4 Background2 = Color4.Black;
        [Entry("background_gradient")]
        public bool BackgroundGradient = false;
        [Entry("grid_color")]
        public Color4 GridColor = Color4.CornflowerBlue * new Color4(0.5f, 0.5f, 0.5f, 1f);
        [Entry("default_camera_mode")]
        public int DefaultCameraMode = 0;
        [Entry("default_render_mode")]
        public int DefaultRenderMode = 0;
        [Entry("last_export_path")]
        private string lastExportPath;
        [Entry("blender_path")]
        private string blenderPath;
        [Entry("lod_multiplier")]
        public float LodMultiplier = 1.3f;
        [Entry("log_visible")]
        public bool LogVisible;
        [Entry("files_visible")]
        public bool FilesVisible;

        public int SelectedAnisotropy => TextureFilter > 2 ? (int)Math.Pow(2, TextureFilter - 2) : 0;

        public TextureFiltering SelectedFiltering => TextureFilter switch
        {
            0  => TextureFiltering.Linear,
            1 => TextureFiltering.Bilinear,
            2 => TextureFiltering.Trilinear,
            _ => TextureFiltering.Anisotropic
        };

        public List<BrowserFavorite> Favorites = new List<BrowserFavorite>();

        [EntryHandler("favorite", Multiline = true, MinComponents = 2)]
        void HandleFavorite(Entry e) => Favorites.Add(new BrowserFavorite(Decode(e[0].ToString()), Decode(e[1].ToString())));

        int IRendererSettings.SelectedMSAA => MSAA;

        float IRendererSettings.LodMultiplier => LodMultiplier;

        public string LastExportPath
        {
            get => Decode(lastExportPath);
            set => lastExportPath = Encode(value);
        }

        public string BlenderPath
        {
            get => Decode(blenderPath);
            set => blenderPath = Encode(value);
        }

        static string Decode(string encoded)
            => !string.IsNullOrWhiteSpace(encoded)
                ? Encoding.UTF8.GetString(Convert.FromBase64String(encoded.Replace('_', '=')))
                : "";

        static string Encode(string src) => Convert.ToBase64String(Encoding.UTF8.GetBytes(src)).Replace('=','_');


        [Entry("ui_scale")] public float UiScale = 1f;
        static string FormatColor(Color4 c)
        {
            static string Fmt(float f) => ((int) (f * 255f)).ToString();
            return $"{Fmt(c.R)}, {Fmt(c.G)}, {Fmt(c.B)}, {Fmt(c.A)}";
        }
        public void Save()
        {
            using(var writer = new StreamWriter(configPath))
            {
                writer.WriteLine("[Config]");
                writer.WriteLine($"msaa = {MSAA}");
                writer.WriteLine($"texture_filter = {TextureFilter}");
                writer.WriteLine($"view_buttons = {(ViewButtons ? "true" : "false")}");
                writer.WriteLine($"pause_when_unfocused = {(PauseWhenUnfocused ? "true" : "false")}");
                writer.WriteLine($"background_top = {FormatColor(Background)}");
                writer.WriteLine($"background_bottom = {FormatColor(Background2)}");
                writer.WriteLine($"background_gradient = {(BackgroundGradient ? "true" : "false")}");
                writer.WriteLine($"grid_color = {FormatColor(GridColor)}");
                writer.WriteLine($"default_camera_mode = {DefaultCameraMode}");
                writer.WriteLine($"default_render_mode = {DefaultRenderMode}");
                writer.WriteLine(Invariant($"ui_scale = {UiScale:F4}"));
                if(!string.IsNullOrWhiteSpace(lastExportPath))
                    writer.WriteLine($"last_export_path = {lastExportPath}");
                if(!string.IsNullOrWhiteSpace(blenderPath))
                    writer.WriteLine($"blender_path = {blenderPath}");
                writer.WriteLine($"lod_multiplier = {LodMultiplier.ToStringInvariant()}");
                writer.WriteLine($"log_visible = {(LogVisible ? "true": "false" )}");
                writer.WriteLine($"files_visible = {(FilesVisible ? "true": "false" )}");
                foreach(var fav in Favorites)
                    writer.WriteLine($"favorite = {Encode(fav.Name)}, {Encode(fav.FullPath)}");
            }
        }

        public void Validate(RenderContext context)
        {
            if (SelectedAnisotropy > context.MaxSamples)
            {
                FLLog.Info("Config", $"{SelectedAnisotropy}x anisotropy not supported, disabling.");
                TextureFilter = 2;
            }
            if (MSAA > context.MaxSamples)
            {
                FLLog.Info("Config", $"{MSAA}x MSAA not supported, disabling.");
                MSAA = 0;
            }
        }

        public static EditorConfiguration Load()
        {
            try
            {
                if (File.Exists(configPath))
                {
                    var ec = new EditorConfiguration();
                    ec.ParseAndFill(configPath, null);
                    if (ec.UiScale < 1 || ec.UiScale > 2.5f)
                        ec.UiScale = 1;
                    return ec;
                }
                else
                    return new EditorConfiguration();
            }
            catch (Exception)
            {
                FLLog.Error("Config", "Error loading lanceredit.ini");
                return new EditorConfiguration();
            }
        }

        static string configPath;
        static EditorConfiguration()
        {
            SetConfigPath();
            FLLog.Info("Config", "Path: " + configPath);
        }

        static void SetConfigPath()
        {
            configPath = Path.Combine(Platform.GetLocalConfigFolder(), "lanceredit.ini");
        }
    }
}
