using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using LibreLancer;
using LibreLancer.Data;
using LibreLancer.Infocards;
using LibreLancer.Interface;

namespace InterfaceEdit
{
    public class Project
    {
        public UiData UiData;
        public UiXmlLoader XmlLoader;
        public string XmlFolder;
        public string FlFolder;
        public string ProjectFile;

        public Infocard TestingInfocard;
        private MainWindow window;
        public Project(MainWindow window)
        {
            this.window = window;
           
        }

        void Load()
        {
            UiData.FlDirectory = FlFolder;
            UiData.ResourceManager = new GameResourceManager(window);
            UiData.FileSystem = FileSystem.FromFolder(FlFolder);
            UiData.Fonts = window.Fonts;
            UiData.OpenFolder(XmlFolder);
            
            var flIni = new FreelancerIni(UiData.FileSystem);
            var dataPath = UiData.FileSystem.Resolve(flIni.DataPath); 
           
            try
            {
                var navbarIni = new LibreLancer.Data.BaseNavBarIni(UiData.FileSystem);
                UiData.NavbarIcons = navbarIni.Navbar;
            }
            catch (Exception)
            {
                UiData.NavbarIcons = null;
            }

            try
            {
                var hud = new HudIni();
                hud.AddIni(flIni.HudPath, UiData.FileSystem);
                var maneuvers = new List<Maneuver>();
                var p = flIni.DataPath.Replace('\\', Path.DirectorySeparatorChar);
                foreach (var m in hud.Maneuvers)
                {
                    maneuvers.Add(new Maneuver()
                    {
                        Action = m.Action,
                        ActiveModel = Path.Combine(p,m.ActiveModel),
                        InactiveModel = Path.Combine(p,m.InactiveModel)
                    });
                }
                window.TestApi.ManeuverData = maneuvers.ToArray();
            }
            catch (Exception)
            {
                window.TestApi.ManeuverData = null;
            }
            if (flIni.JsonResources != null)
                UiData.Infocards = new InfocardManager(flIni.JsonResources.Item1, flIni.JsonResources.Item2);
            else if (flIni.Resources != null)
                UiData.Infocards = new InfocardManager(flIni.Resources);
            UiData.DataPath = flIni.DataPath;
            XmlLoader = new UiXmlLoader(UiData.Resources);
            try
            {
                UiData.Stylesheet = (Stylesheet)XmlLoader.FromString(UiData.ReadAllText("stylesheet.xml"), null);
            }
            catch (Exception)
            {
            }
            window.Fonts.LoadFontsFromIni(flIni, UiData.FileSystem);
            //unioners infocard
            var im = new InfocardManager(flIni.Resources);
            TestingInfocard = RDLParse.Parse(im.GetXmlResource(65546), window.Fonts);
        }

        public void Open(string projectpath)
        {
            UiData = new UiData();
            XmlFolder = Path.GetDirectoryName(projectpath);
            ProjectFile = projectpath;
            using (var reader = new StreamReader(File.OpenRead(projectpath)))
            {
                var x = (ProjectXml) _xml.Deserialize(reader);
                FlFolder = x.DataFolder;
            }
            Load();
        }
        
        public void Create(string folder, string projectpath)
        {
            UiData = new UiData();
            FlFolder = folder;
            XmlFolder = Path.GetDirectoryName(projectpath);
            WriteBlankFiles();
            ProjectFile = projectpath;

            using (var writer = new StreamWriter(File.Create(projectpath)))
            {
                _xml.Serialize(writer, new ProjectXml() { DataFolder = folder });
            }
            Load();
        }
        
        static XmlSerializer _xml = new XmlSerializer(typeof(ProjectXml));
        public class ProjectXml
        {
            public string DataFolder;
        }
        
        public void WriteResources() => File.WriteAllText(Path.Combine(XmlFolder, "resources.xml"), UiData.Resources.ToXml());

        void WriteBlankFiles()
        {
            var resources = new InterfaceResources();
            File.WriteAllText(Path.Combine(XmlFolder, "resources.xml"), resources.ToXml());
            File.WriteAllText(Path.Combine(XmlFolder, "stylesheet.xml"), "<Stylesheet></Stylesheet>");
        }
    }
}