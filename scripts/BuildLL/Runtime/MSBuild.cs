using System;
using System.Collections.Generic;
using System.IO;
using static BuildLL.Runtime;

namespace BuildLL
{
    public enum VSVersion
    {
        VS2019,
        VS2022
    }
    public enum MSBuildPlatform
    {
        x86,
        x64
    }
    public class MSBuild
    {
        static readonly string[] _editions = {
            "Enterprise", "Professional", "Community", "BuildTools"
        };
        static readonly List<string> paths = new List<string>();

        static string VSPath(string ver, string[] editions, string msbuildVer, MSBuildPlatform platform)
        {
            // VS 2022 can exist in either Program Files or X86
            // Just check all combinations
            string[] checkFolders =
            [
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            ];
            foreach (var pg in checkFolders)
            {
                foreach (var e in editions)
                {
                    var bin = Path.Combine(pg, "Microsoft Visual Studio", ver, e, "MSBuild", msbuildVer, "Bin");
                    paths.Add(bin);
                    if (Directory.Exists(bin))
                    {
                        if (platform == MSBuildPlatform.x64)
                            return Path.Combine(bin, "amd64", "MSBuild.exe");
                        return Path.Combine(bin, "MSBuild.exe");
                    }
                }
            }
            return null;
        }

        public static void Run(string file, string args, VSVersion vs, MSBuildPlatform platform)
        {
            string msbuild = null;
            paths.Clear();
            if (vs == VSVersion.VS2019) {
                msbuild = VSPath("2019", _editions, "Current", platform);
                if (msbuild == null) msbuild = VSPath("2019", _editions, "16.0", platform);
            }
             if (vs == VSVersion.VS2022) {
                msbuild = VSPath("2022", _editions, "Current", platform);
                if (msbuild == null) msbuild = VSPath("2022", _editions, "17.0", platform);
            }

            if (msbuild == null)
            {
                foreach (var p in paths) Console.WriteLine("Tried: {0}", p);
                throw new Exception($"Could not find installed MSBuild for {vs} {platform}");
            }

            RunCommand(msbuild, $"{Quote(file)} {args}");
        }
    }
}
