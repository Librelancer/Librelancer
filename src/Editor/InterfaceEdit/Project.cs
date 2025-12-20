using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using LibreLancer;
using LibreLancer.Data;
using LibreLancer.Data.IO;
using LibreLancer.Data.Schema;
using LibreLancer.Infocards;
using LibreLancer.Interface;
using LibreLancer.Resources;

namespace InterfaceEdit
{
    public class Project
    {
        public UiData UiData;
        public UiXmlLoader XmlLoader;

        public ProjectConfiguration Configuration { get; set; }
        public string XmlFolder { get; private set; }
        public string FlFolder => ProjectVariable.Substitute(Configuration.DataFolder, ProjectVariables());

        public string OutputFilename => ProjectVariable.Substitute(
            Configuration.OutputFilename,
            ProjectVariables(),
            Path.Combine(XmlFolder, "out", "interface.json")
        );


        public string ProjectFile;

        public Infocard TestingInfocard;
        public Infocard ShipInfocard;
        private MainWindow window;
        public Project(MainWindow window)
        {
            this.window = window;
        }

        IDictionary<string, string> ProjectVariables()
        {
            var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var v in window.Variables)
                variables[v.Key] = v.Value;
            variables["ProjectFolder"] = XmlFolder;
            return variables;
        }

        public string ResolvedDataDir { get; private set; }


        void Load()
        {
            UiData.FlDirectory = FlFolder;
            UiData.FileSystem = FileSystem.FromPath(FlFolder);
            UiData.ResourceManager = new GameResourceManager(window, UiData.FileSystem);
            UiData.Fonts = window.Fonts;
            var flIni = new FreelancerIni(UiData.FileSystem);
            var dataPath = "/";
            ResolvedDataDir = dataPath;
            UiData.DataPath = flIni.DataPath;
            //TODO: Fix to work with custom game
            UiData.NavmapIcons = new NavmapIcons();
            UiData.OpenFolder(XmlFolder);

            try
            {
                var navbarIni = new BaseNavBarIni(dataPath, UiData.FileSystem);
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
                foreach (var m in hud.Maneuvers)
                {
                    maneuvers.Add(new Maneuver()
                    {
                        Action = m.Action,
                        ActiveModel = m.ActiveModel,
                        InactiveModel = m.InactiveModel
                    });
                }
                window.TestApi.ManeuverData = maneuvers.ToArray();
            }
            catch (Exception)
            {
                window.TestApi.ManeuverData = null;
            }

            UiData.Infocards = new InfocardManager(flIni.Resources);
            XmlLoader = new UiXmlLoader(UiData.Resources);
            try
            {
                UiData.Stylesheet = (Stylesheet)XmlLoader.FromString(UiData.ReadAllText("stylesheet.xml"), null);
            }
            catch (Exception)
            {
            }
            window.Fonts.LoadFontsFromIni(flIni, window.RenderContext, UiData.FileSystem);
            //unioners infocard
            var im = new InfocardManager(flIni.Resources);
            TestingInfocard = RDLParse.Parse(im.GetXmlResource(65546), window.Fonts);
            ShipInfocard = RDLParse.Parse(im.GetXmlResource(66598), window.Fonts);
        }

        public bool Open(string projectpath)
        {
            UiData = new UiData();
            XmlFolder = Path.GetDirectoryName(projectpath);
            ProjectFile = projectpath;
            Configuration = ProjectConfiguration.Read(projectpath);
            if (!Directory.Exists(FlFolder))
                return false;
            Load();
            return true;
        }

        public void Create(string folder, string projectpath)
        {
            UiData = new UiData();
            UiData.NavmapIcons = new NavmapIcons();
            XmlFolder = Path.GetDirectoryName(projectpath);
            WriteBlankFiles();
            ProjectFile = projectpath;
            Configuration = new ProjectConfiguration();
            Configuration.DataFolder = folder;
            Configuration.Write(projectpath);
            Load();
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
