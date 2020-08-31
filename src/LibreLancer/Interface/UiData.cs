// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibreLancer.Data;

namespace LibreLancer.Interface
{
    public class UiData
    {
        //Data
        public string DataPath;
        public GameResourceManager ResourceManager;
        public InfocardManager Infocards;
        public FontManager Fonts;
        public FileSystem FileSystem;
        public Dictionary<string,string> NavbarIcons;
        public SoundManager Sounds;
        //Ui
        public Stylesheet Stylesheet;
        public UiXmlLoader XmlLoader;
        public InterfaceResources Resources;
        //TODO: Make configurable
        public INavmapIcons NavmapIcons = new NavmapIcons();
        public string XInterfacePath;
        //Editor-only
        public string FlDirectory;
        public UiData()
        {
        }
        public UiData(FreelancerGame game)
        {
            ResourceManager = game.ResourceManager;
            FileSystem = game.GameData.VFS;
            Infocards = game.GameData.Ini.Infocards;
            Fonts = game.Fonts;
            NavbarIcons = game.GameData.GetBaseNavbarIcons();
            Sounds = game.Sound;
            DataPath = game.GameData.Ini.Freelancer.DataPath;
            if (!string.IsNullOrWhiteSpace(game.GameData.Ini.Freelancer.XInterfacePath))
                OpenFolder(game.GameData.Ini.Freelancer.XInterfacePath);
            else
                OpenDefault();
        }

        public string GetNavbarIconPath(string icon)
        {
            var p = DataPath.Replace('\\', Path.DirectorySeparatorChar);
            string ic;
            if (!NavbarIcons.TryGetValue(icon, out ic))
            {
                FLLog.Warning("Interface", $"Could not find icon {icon}");
                ic = NavbarIcons.Values.First();
            }

            return Path.Combine(p, ic);
        }
        public string GetFont(string fontName)
        {
            if (fontName[0] == '$') fontName = Fonts.ResolveNickname(fontName.Substring(1));
            return fontName;
        }

        public InterfaceColor GetColor(string color)
        {
            var clr = Resources.Colors.FirstOrDefault(x => x.Name.Equals(color, StringComparison.OrdinalIgnoreCase));
            return clr ?? new InterfaceColor() {
                Color = Parser.Color(color)
            };
        }
        
        Dictionary<string, Texture2D> loadedFiles = new Dictionary<string,Texture2D>();
        public Texture2D GetTextureFile(string filename)
        {
            try
            {
                var file = FileSystem.Resolve(filename);
                if (!loadedFiles.ContainsKey(file))
                {
                    loadedFiles.Add(file, LibreLancer.ImageLib.Generic.FromFile(file));
                }
                return loadedFiles[file];
            }
            catch (Exception)
            {
                return null;
            }
        }

        public RigidModel GetModel(string path)
        {
            if(string.IsNullOrEmpty(path)) return null;
            try
            {
                return ((IRigidModelFile) ResourceManager.GetDrawable(FileSystem.Resolve(path))).CreateRigidModel(true);
            }
            catch (Exception e)
            {
                FLLog.Error("UiContext",$"{e.Message}\n{e.StackTrace}");
                return null;
            }
        }
        public void OpenFolder(string xinterfacePath)
        {
            XInterfacePath = xinterfacePath;
            ReadResourcesAndStylesheet();
        }
        
        public void OpenDefault()
        {
            XInterfacePath = null;
            ReadResourcesAndStylesheet();
        }

        void ReadResourcesAndStylesheet()
        {
            Resources = InterfaceResources.FromXml(ReadAllText("resources.xml"));
            XmlLoader = new UiXmlLoader(Resources);
            Stylesheet = (Stylesheet) XmlLoader.FromString(ReadAllText("stylesheet.xml"), null);
            LoadLibraries();
        }
        public string ReadAllText(string file)
        {
            if (!string.IsNullOrEmpty(XInterfacePath))
            {
                var path = FileSystem.Resolve(Path.Combine(XInterfacePath, file));
                return File.ReadAllText(path);
            }
            else
            {
                using (var reader =
                    new StreamReader(
                        typeof(UiContext).Assembly.GetManifestResourceStream($"LibreLancer.Interface.Default.{file}")))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public UiWidget LoadXml(string file)
        {
            var widget = (UiWidget) XmlLoader.FromString(ReadAllText(file), null);
            widget.ApplyStylesheet(Stylesheet);
            return widget;
        }

        public void LoadLibraries()
        {
            foreach (var file in Resources.LibraryFiles)
            {
                ResourceManager.LoadResourceFile(FileSystem.Resolve(file));
            }
        }
    }
}