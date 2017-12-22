using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace slngen
{
    public class ProjectInformation
    {
        public string ProjectGuid;
        public string TypeGuid;
        public List<string> Dependencies = new List<string>();
    }
    class MSBuild
    {
        DynamicLoader ms;
        bool legacy;

        private MSBuild() { }

        const string DEFAULT_TYPE = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";

        public ProjectInformation GetInformation(string path)
        {
            var info = new ProjectInformation();
            if (legacy)
            {
                var buildEngine = ms.New("Engine", Environment.CurrentDirectory);
                var project = ms.New("Project", buildEngine);
                project.Load(path);
                string guid = project.GetEvaluatedProperty("ProjectGuid");
                info.ProjectGuid = guid;
                string projtype = project.GetEvaluatedProperty("ProjectTypeGuids");
				//TODO: Big hack
				if (!string.IsNullOrEmpty(projtype) && projtype.Contains(DEFAULT_TYPE)) projtype = DEFAULT_TYPE;
                info.TypeGuid = string.IsNullOrEmpty(projtype) ? DEFAULT_TYPE : projtype;
            }
            else
            {
                var collection = ms.New("ProjectCollection");
                var project = ms.New("Project", path, null, null, collection);
                string guid = project.GetPropertyValue("ProjectGuid");
                info.ProjectGuid = guid;
                string projtype = project.GetPropertyValue("ProjectTypeGuids");
				//TODO: Big hack
				if (!string.IsNullOrEmpty(projtype) && projtype.Contains(DEFAULT_TYPE)) projtype = DEFAULT_TYPE;
                info.TypeGuid = string.IsNullOrEmpty(projtype) ? DEFAULT_TYPE : projtype;
                foreach (var item in project.GetItems("ProjectReference"))
                {
                    info.Dependencies.Add(item.GetMetadataValue("Project"));
                }
                collection.Dispose();
            }
            return info;
        }

        public static MSBuild Create(string testproject)
        {
            var windows = (Environment.OSVersion.Platform != PlatformID.Unix && Environment.OSVersion.Platform != PlatformID.MacOSX);
            var instance = new MSBuild();
            var msbuilddir = "";
            if (windows)
            {
                var msbuildver = 0.0m;
                var searchPath = Path.Combine(Environment.GetFolderPath(
                    IntPtr.Size == 8 ? Environment.SpecialFolder.ProgramFilesX86 : Environment.SpecialFolder.ProgramFiles), "MSBuild");
                foreach (var directory in Directory.GetDirectories(searchPath))
                {
                    var split = directory.Split('\\');
                    var ver = split[split.Length - 1];
                    decimal d;
                    if (decimal.TryParse(ver, out d) && Directory.Exists(Path.Combine(directory, "Bin")))
                    {
                        msbuildver = Math.Max(d, msbuildver);
                    }
                }
                if (msbuildver > 0)
                {
                    msbuilddir = Path.Combine(Path.Combine(searchPath, msbuildver.ToString(System.Globalization.CultureInfo.InvariantCulture)), "Bin\\");
                    Console.WriteLine("MSBuild Auto-detection: " + msbuilddir);
                }
            }
            var eval = new DynamicLoader(msbuilddir + "Microsoft.Build.dll") { Namespace = "Microsoft.Build.Evaluation" };
            var col = eval.New("ProjectCollection");
            var testprj = eval.New("Project", testproject, null, null, col);
            if (string.IsNullOrEmpty(testprj.GetPropertyValue("ProjectGuid")))
            {
                Console.WriteLine("Using Legacy MSBuild API");
                instance.legacy = true;
                instance.ms = new DynamicLoader(msbuilddir + "Microsoft.Build.Engine.dll") { Namespace = "Microsoft.Build.BuildEngine" };
            }
            else
            {
                instance.ms = eval;
            }
            return instance;
        }
    }

    class DynamicLoader
    {
        Assembly asm;
        public string Namespace;
        public DynamicLoader(string name)
        {
            if (name.IndexOfAny(new[] { '/', '\\' }) != -1)
            {
                asm = Assembly.LoadFrom(name);
            }
            else
            {
                try
                {
                    var ln = name;
                    if (name.EndsWith(".dll") || name.EndsWith(".exe"))
                    {
                        ln = name.Substring(0, name.Length - 4);
                    }
                    asm = Assembly.LoadFrom(ln);
                }
                catch (Exception)
                {
                    asm = Assembly.LoadFile(Path.Combine(GetSystemDir(), name));
                }
            }
        }

        static string GetSystemDir()
        {
            return Path.GetDirectoryName(typeof(object).Assembly.Location);
        }

        public dynamic New(string name, params object[] p)
        {
            var t = asm.GetType(Namespace == "" ? Namespace : (Namespace + ".") + name);
            try
            {
                return Activator.CreateInstance(t, p);
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null) throw ex.InnerException;
                throw;
            }

        }
    }

}
