using System;
using System.IO;

namespace BuildLL;

public class WindowsNatives
{
    public static void Build(
        string srcDir,
        string outputDir,
        string slnname,
        string buildType,
        VSVersion version,
        MSBuildPlatform platform,
        params string[] options)
    {
        options ??= [];
        version = MSBuild.SelectVersion(version, platform);

        string generator = version switch
        {
            VSVersion.VS2026 => "Visual Studio 18 2026",
            VSVersion.VS2022 => "Visual Studio 17 2022",
            VSVersion.VS2019 => "Visual Studio 16 2019",
            _ => throw new InvalidOperationException() // unreachable
        };
        string cmakePlatform = platform == MSBuildPlatform.x64 ? "x64" : "Win32";

        CMake.Run(srcDir, new()
        {
            OutputPath = outputDir,
            Generator = generator,
            Platform = cmakePlatform,
            BuildType = buildType,
            Options = options
        });

        var ext = version >= VSVersion.VS2026 ? ".slnx" : ".sln";
        string sln = Path.Combine(outputDir, slnname) + ext;
        MSBuild.Run(sln, $"/m /p:Configuration={buildType}", version, platform);
    }
}
