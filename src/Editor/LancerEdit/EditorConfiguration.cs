// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Globalization;
using System.IO;
using System.Xml.Serialization;
using LibreLancer;
using LibreLancer.Ini;

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
    public class EditorConfiguration : IniFile
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
        public Color4 GridColor = Color4.CornflowerBlue;
        [Entry("default_camera_mode")]
        public int DefaultCameraMode = 0;

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
            if (Platform.RunningOS == OS.Windows)
                configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "lanceredit.ini");
            else
            {
                string osConfigDir = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
                if (String.IsNullOrEmpty(osConfigDir))
                {
                    osConfigDir = Environment.GetEnvironmentVariable("HOME");
                    if (String.IsNullOrEmpty(osConfigDir))
                    {
                        configPath = "lanceredit.ini";
                        return;
                    }
                    osConfigDir += "/.config/";
                }
                configPath = Path.Combine(osConfigDir, "lanceredit.ini");
            }
        }
    }
}
