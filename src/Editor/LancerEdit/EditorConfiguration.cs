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
using LibreLancer.Data.Ini;
using LibreLancer.Graphics;
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

    public class CameraPreset(string name, string preset)
    {
        public string Name = name;
        public string Preset = preset;
    }

    [ParsedSection]
    public partial class EditorConfiguration : IRendererSettings
    {
        [Entry("tab_style")]
        public int TabStyle;
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
        [Entry("default_sysedit_camera_mode")]
        public int DefaultSysEditCameraMode = 0;
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
        [Entry("status_bar_visible")]
        public bool StatusBarVisible = true;
        [Entry("collada_visible")]
        public bool ColladaVisible;
        [Entry("update_channel")]
        public string UpdateChannel;

        public string AutoLoadPath = "";

        public List<CameraPreset> CameraPresets = [];

        [EntryHandler("camera_preset", MinComponents = 2)]
        void HandleCameraPreset(Entry entry)
        {
            CameraPresets.Add(new(CommentEscaping.Unescape(entry[0].ToString()!), entry[1].ToString()));
        }

        [EntryHandler("auto_load_path", MinComponents = 1)]
        void HandleAutoLoadPath(Entry entry)
        {
            var pathBase64 = entry[0].ToString()!;
            AutoLoadPath = Decode(pathBase64);
        }

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

        public void Save()
        {
            if (!canSave)
                return;
            var b = new IniBuilder();
            var c = b.Section("Config")
                .Entry("tab_style", TabStyle)
                .Entry("msaa", MSAA)
                .Entry("texture_filter", TextureFilter)
                .Entry("view_buttons", ViewButtons)
                .Entry("pause_when_unfocused", PauseWhenUnfocused)
                .Entry("background_top", Background, true)
                .Entry("background_bottom", Background2, true)
                .Entry("background_gradient", BackgroundGradient)
                .Entry("grid_color", GridColor, true)
                .Entry("default_camera_mode", DefaultCameraMode)
                .Entry("default_sysedit_camera_mode", DefaultSysEditCameraMode)
                .Entry("default_render_mode", DefaultRenderMode)
                .Entry("ui_scale", UiScale)
                .OptionalEntry("last_export_path", lastExportPath)
                .OptionalEntry("blender_path", blenderPath)
                .OptionalEntry("update_channel", UpdateChannel)
                .Entry("lod_multiplier", LodMultiplier)
                .Entry("log_visible", LogVisible)
                .Entry("files_visible", FilesVisible)
                .Entry("status_bar_visible", StatusBarVisible)
                .Entry("collada_visible", ColladaVisible);
            foreach (var fav in Favorites)
                c.Entry("favorite", Encode(fav.Name), Encode(fav.FullPath));
            if (!string.IsNullOrWhiteSpace(AutoLoadPath))
                c.Entry("auto_load_path", Encode(AutoLoadPath));
            foreach (var e in CameraPresets)
                c.Entry("camera_preset", CommentEscaping.Escape(e.Name), e.Preset);
            using(var file = File.Create(configPath))
                IniWriter.WriteIni(file, b.Sections);
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

        private bool canSave;
        private EditorConfiguration(bool isFile)
        {
            canSave = isFile;
        }

        private EditorConfiguration() : this(true)
        {

        }

        public static EditorConfiguration Load(bool isFile)
        {
            if (!isFile)
            {
                FLLog.Info("Config", "TESTING MODE");
                return new EditorConfiguration(false);
            }

            try
            {
                if (File.Exists(configPath))
                {
                    using var strm = File.OpenRead(configPath);
                    var section = IniFile.ParseFile(configPath, strm).FirstOrDefault();
                    EditorConfiguration ec = null;
                    if (section != null)
                    {
                        TryParse(section, out ec);
                    }
                    ec ??= new EditorConfiguration(true);
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
