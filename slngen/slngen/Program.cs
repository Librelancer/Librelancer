using System;
using System.Collections.Generic;
using System.IO;

namespace slngen
{
    class Program
    {
        class ProjectDef
        {
            public string Path;
            public string Folder;
            public BPlatforms Platforms;
            public ProjectInformation Info;
        }
        class SlnItem
        {
            public string Path;
            public string Folder;
        }
		[Flags]
        enum BPlatforms
        {
            None = 0,
            Windows = 2,
            Linux = 4,
            Mac = 8,
            All = (2 | 4 | 8)
        }
        const string FOLDER_GUID = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";
        static void Main(string[] args)
        {
            Console.WriteLine("SLNGEN v1.0");

            BPlatforms currentPlatform = BPlatforms.Windows;
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Unix:
                    if (Directory.Exists("/Applications") &&
                        Directory.Exists("/Users") &&
                        Directory.Exists("/System") &&
                        Directory.Exists("/Volumes"))
                        currentPlatform = BPlatforms.Mac;
                    else
                        currentPlatform = BPlatforms.Linux;
                    break;
                case PlatformID.MacOSX:
                    currentPlatform = BPlatforms.Mac;
                    break;
            }
            Console.WriteLine("Platform: {0}", currentPlatform);
            string basedir = null;
            string slnname = null;
            List<string> configs = new List<string>();
            List<SlnItem> solutionItems = new List<SlnItem>();
            List<string> slnItemFolders = new List<string>();
            List<ProjectDef> projects = new List<ProjectDef>();
            Dictionary<string, string> slnFolders = new Dictionary<string, string>();
            bool hasFolders = false;

            foreach (var section in ParseConfig("slngen.conf"))
            {
                switch (section.Name.ToLowerInvariant())
                {
                    case "solution":
                        basedir = section.Entries["basedir"];
                        slnname = section.Entries["slnname"];
                        break;
                    case "configuration":
                        configs.Add(section.Entries["config"]);
                        break;
                    case "project":
                        string v;
                        var proj = new ProjectDef() { Path = section.Entries["path"] };
                        if (section.Entries.TryGetValue("folder", out v)) proj.Folder = v;
                        if (section.Entries.TryGetValue("platforms", out v))
                        {
                            var platforms = BPlatforms.None;
                            foreach (var pl in v.Split(','))
                            {
                                var platform = pl.Trim().ToLowerInvariant();
                                switch (platform)
                                {
                                    case "linux": platforms |= BPlatforms.Linux; break;
                                    case "windows": platforms |= BPlatforms.Windows; break;
                                    case "mac": platforms |= BPlatforms.Mac; break;
                                }
                            }
                            proj.Platforms = platforms;
                        }
                        else
                        {
                            proj.Platforms = BPlatforms.All;
                        }
                        projects.Add(proj);
                        break;
                    case "solutionitem":
                        var item = new SlnItem() { Path = section.Entries["path"] };
                        string s; if (!section.Entries.TryGetValue("folder", out s)) s = "Solution Items";
                        item.Folder = s;
                        solutionItems.Add(item);
                        break;
                    default:
                        throw new Exception("Invalid config section " + section.Name);
                }
            }
            slnname = string.Format(slnname, currentPlatform);
            string testpath = null;
            foreach (var p in projects)
            {
                if ((p.Platforms & currentPlatform) != currentPlatform) continue;
                testpath = Path.Combine(basedir, p.Path);
                break;
            }
            if (testpath == null)
            {
                Console.WriteLine("No projects configured to build for this platform");
                return;
            }
            var ms = MSBuild.Create(testpath);
            Console.WriteLine("Enumerating Projects");
            foreach (var p in projects)
            {
				if ((p.Platforms & currentPlatform) != currentPlatform) continue;
                var projpath = Path.Combine(basedir, p.Path);
                p.Info = ms.GetInformation(projpath);
                if (p.Folder != null && !slnFolders.ContainsKey(p.Folder))
                {
                    var folderGuid = Guid.NewGuid().ToString("B").ToUpperInvariant();
                    slnFolders.Add(p.Folder, folderGuid);
                    hasFolders = true;
                }
            }
            foreach (var itm in solutionItems)
            {
                if (!slnItemFolders.Contains(itm.Folder)) slnItemFolders.Add(itm.Folder);
            }
            Console.WriteLine("Outputting to: {0}", Path.Combine(basedir, slnname));
            using (var writer = new StreamWriter(File.Create(Path.Combine(basedir, slnname))))
            {
                writer.NewLine = "\n";
                writer.WriteLine("Microsoft Visual Studio Solution File, Format Version 12.00");
                writer.WriteLine("# Visual Studio 14");
                writer.WriteLine("VisualStudioVersion = 14.0.25420.1");
                writer.WriteLine("MinimumVisualStudioVersion = 10.0.40219.1");
                foreach (var p in projects)
                {
                    if ((p.Platforms & currentPlatform) != currentPlatform) continue;
                    writer.WriteLine("Project(\"{0}\") = \"{1}\", \"{2}\", \"{3}\"",
                                     p.Info.TypeGuid,
                                     Path.GetFileNameWithoutExtension(p.Path),
                                     p.Path.Replace("/", "\\"),
                                     p.Info.ProjectGuid);
                    if (p.Info.Dependencies.Count > 0)
                    {
                        writer.WriteLine("\tProjectSection(ProjectDependencies) = postProject");
                        foreach (var dep in p.Info.Dependencies)
                        {
                            writer.WriteLine("\t\t{0} = {0}", dep);
                        }
                        writer.WriteLine("\tEndProjectSection");
                    }
                    writer.WriteLine("EndProject");
                }
                foreach (var kv in slnFolders)
                {
                    writer.WriteLine("Project(\"{0}\") = \"{1}\", \"{1}\", \"{2}\"",
                                    FOLDER_GUID,
                                    kv.Key,
                                    kv.Value
                                    );
                    writer.WriteLine("EndProject");
                }
                foreach (var fld in slnItemFolders)
                {
                    writer.WriteLine("Project(\"{0}\") = \"{1}\", \"{1}\", \"{2}\"",
                                    FOLDER_GUID,
                                    fld,
                                    Guid.NewGuid().ToString("B").ToUpperInvariant()
                                    );
                    writer.WriteLine("\tProjectSection(SolutionItems) = preProject");
                    foreach (var itm in solutionItems)
                    {
                        if (itm.Folder == fld) writer.WriteLine("\t\t{0} = {0}", itm.Path.Replace("/", "\\"));
                    }
                    writer.WriteLine("\tEndProjectSection");
                    writer.WriteLine("EndProject");
                }
                writer.WriteLine();
                writer.WriteLine("Global");
                writer.WriteLine("\tGlobalSection(SolutionProperties) = preSolution");
                writer.WriteLine("\t\tHideSolutionNode = FALSE");
                writer.WriteLine("\tEndGlobalSection");
                writer.WriteLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
                foreach (var conf in configs)
                {
                    writer.WriteLine("\t\t{0} = {0}", conf);
                }
                writer.WriteLine("\tEndGlobalSection");
                writer.WriteLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");
                foreach (var p in projects)
                {
                    if ((p.Platforms & currentPlatform) != currentPlatform) continue;
                    foreach (var conf in configs)
                    {
                        writer.WriteLine("\t\t{0}.{1}.ActiveCfg = {1}", p.Info.ProjectGuid, conf);
                        writer.WriteLine("\t\t{0}.{1}.Build.0 = {1}", p.Info.ProjectGuid, conf);
                    }
                }
                writer.WriteLine("\tEndGlobalSection");
                if (hasFolders)
                {
                    writer.WriteLine("\tGlobalSection(NestedProjects) = preSolution");
                    foreach (var p in projects)
                    {
                        if ((p.Platforms & currentPlatform) != currentPlatform) continue;
                        if (p.Folder == null) continue;
                        writer.WriteLine("\t\t{0} = {1}", p.Info.ProjectGuid, slnFolders[p.Folder]);
                    }
                    writer.WriteLine("\tEndGlobalSection");
                }
                writer.WriteLine("EndGlobal");
            }

        }

        static IEnumerable<ConfigSection> ParseConfig(string file)
        {
            using (var reader = new StreamReader(File.OpenRead(file)))
            {
                ConfigSection currentSection = null;
                while (reader.Peek() != -1)
                {
                    var ln = reader.ReadLine().Trim();
                    if (string.IsNullOrEmpty(ln) || ln[0] == ';') continue;
                    if (ln.StartsWith("["))
                    {
                        if (currentSection != null) yield return currentSection;
                        currentSection = new ConfigSection();
                        currentSection.Name = ln.TrimStart('[').TrimEnd(']').Trim();
                    }
                    else
                    {
                        if (!ln.Contains("=")) throw new Exception(string.Format("Invalid line '{0}' in config", ln));
                        var split = ln.Split('=');
                        currentSection.Entries.Add(split[0].Trim(), split[1].Trim());
                    }
                }
                yield return currentSection;
            }
        }
        class ConfigSection
        {
            public string Name;
            public Dictionary<string, string> Entries = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        }
    }
}
