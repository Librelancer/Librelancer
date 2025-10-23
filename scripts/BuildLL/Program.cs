using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using static Bullseye.Targets;
using static BuildLL.Runtime;

namespace BuildLL
{
    class Program
    {
        private static string versionSetting = "git";
        private static string prefix = "/usr/local/";
        private static int parallel = -1;
        private static string glslangValidatorPath = null;
        private static bool buildDebug = false;
        private static bool buildO0 = false;
        private static bool withWin32 = false;
        private static bool withWin64 = false;
        private static bool withUpdates = true;
        private static string updateChannel = "daily";

        private static DateTime Invoked = DateTime.UtcNow;
        public static void Options()
        {
            StringArg("--assemblyversion", x => versionSetting = x, "Set generated version");
            StringArg("--prefix", x => prefix = x, "Set cmake install prefix");
            IntArg("-j|--jobs", x => parallel = x, "Parallelism for native build step");
            StringArg("--glslangValidator", x => glslangValidatorPath = x);
            FlagArg("--debug", () => buildDebug = true, "Build natives with debug info");
            FlagArg("--O0", () => buildO0 = true, "Build natives with -O0 debug");
            FlagArg("--with-win32", () => withWin32 = true, "Also build for 32-bit windows");
            FlagArg("--with-win64", () => withWin64 = true, "(Linux only) Also build for 64-bit windows");
            FlagArg("--no-updates", () => withUpdates = false, "Disables built in updater (SDK only)");
            StringArg("--update-channel", v => updateChannel = v, "Sets update channel for this build (SDK only)");
        }

        static readonly string[] sdkProjects = {
            "src/lancer/lancer.csproj",
            "src/LLServer/LLServer.csproj",
            "src/LLServerGui/LLServerGui.csproj",
            "src/Editor/InterfaceEdit/InterfaceEdit.csproj",
            "src/Editor/LancerEdit/LancerEdit.csproj",
            "src/Editor/lleditscript/lleditscript.csproj",
            "src/Launcher/Launcher.csproj"
        };

        static readonly string[] engineProjects = {
            "src/lancer/lancer.csproj",
            "src/Launcher/Launcher.csproj",
            "src/LLServer/LLServer.csproj",
            "src/LLServerGui/LLServerGui.csproj",
        };

        static void Clean()
        {
            Dotnet.Clean("LibreLancer.sln");
            RmDir("./obj/");
            RmDir("./bin/");
        }

       static  List<string> publishedProjects = new List<string>();

        static async Task FullBuild(string rid, bool sdk)
        {
            Dotnet.Restore("LibreLancer.sln", rid);

            var projs = sdk ? sdkProjects : engineProjects;
            var objDir = "./obj/projs-";
            var binDir = sdk ? "./bin/librelancer-sdk-" : "./bin/librelancer-";
            var outdir = binDir + rid;
            foreach (var proj in projs)
            {
                var name = Path.GetFileName(proj);
                if (!publishedProjects.Contains(rid + ":" + proj)) {
                    CustomPublish.PatchedPublish(proj, objDir + rid + "/" + name, rid);
                    publishedProjects.Add(rid + ":" + proj);
                }
            }
            await CustomPublish.Merge(objDir + rid, binDir + rid, rid,
                projs.Select(x => Path.GetFileNameWithoutExtension(x)).ToArray());
            if (sdk)
            {
                var docsdir = Path.Combine(outdir, "lib/Docs");
                Directory.CreateDirectory(docsdir);
                CustomPublish.CopyFilesRecursively(new DirectoryInfo("./bin/docs"),
                    new DirectoryInfo(docsdir));
            }
            CopyFile("Credits.txt", outdir);
            if (IsWindows) {
                CopyFile("deps/openal-soft-license.txt", outdir);
                CopyFile("deps/openal-soft-sourceurl.txt", outdir);
            }
            CopyFile("LICENSE", outdir);
            var unixTime = (long)((Invoked - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds);
            if (sdk && withUpdates) {
                File.WriteAllText(Path.Combine(outdir, "lib", "updates.ini"), $"[Updates]\nserver=https://librelancer.net/builds/\nchannel={updateChannel}");
                File.WriteAllText(Path.Combine(outdir,"lib","build.txt"), $"{rid};{unixTime}");
                if(rid.StartsWith("win"))
                    CopyFile("src/updater.win32/updater.exe", Path.Combine(outdir,"lib/"));
                var manifest =
                    Directory.EnumerateFiles(outdir, "*.*", SearchOption.AllDirectories)
                        .Select(x => Path.GetRelativePath(outdir, x).Replace('\\', '/')).ToArray();
                File.WriteAllLines(Path.Combine(outdir, "lib", "manifest.txt"), manifest);
            }

            if (sdk)
            {
                Directory.CreateDirectory(Path.Combine(outdir, "blender"));
                await using var blAddon = File.Create(Path.Combine(outdir, "blender", "librelancer_addon.zip"));
                ZipFile.CreateFromDirectory("src/Editor/librelancer_blender_addon", blAddon, CompressionLevel.Optimal,
                    true);
            }
        }

        static string GetLinuxRid()
        {
            var uname = Bash("uname -m", false).Trim().ToLowerInvariant();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                if (uname.StartsWith("arm64"))
                    return "osx-arm64";
                return "osx-x64";
            }
            else
            {
                if(uname.StartsWith("aarch64"))
                    return "linux-arm64";
                if(uname.StartsWith("armv"))
                    return "linux-arm";
                if(uname.StartsWith("x86_64"))
                    return "linux-x64";
                return "linux-x86";
            }
        }

        static void FindDXC()
        {
            if (IsWindows)
            {
                if (Config.GetBool("USE_SYSTEM_DXC") && FindExeWin32("dxc.exe", []) != null)
                {
                    Console.WriteLine("dxc.exe located on PATH");
                    return;
                }
                if (File.Exists("bin/builddeps/bin/x64/dxc.exe"))
                {
                    Console.WriteLine("dxc.exe located");
                    return;
                }
                if (!File.Exists("obj/dxc.zip"))
                {
                    DownloadFile(Config["DXC_WINX64"], "obj/dxc.zip");
                }
                using var zip = File.OpenRead("obj/dxc.zip");
                ZipFile.ExtractToDirectory(zip, "bin/builddeps", true);
                Console.WriteLine("Pre-built dxc extracted");
                return;
            }
            if (Config.GetBool("USE_SYSTEM_DXC") && UnixHasCommand("dxc"))
            {
                Console.WriteLine("dxc located on PATH");
                return;
            }
            if (File.Exists("bin/builddeps/bin/dxc"))
            {
                Console.WriteLine("dxc located");
                return;
            }

            var rid = GetLinuxRid();
            if (rid == "linux-x64")
            {
                if (!File.Exists("obj/dxc.tar.gz"))
                {
                    DownloadFile(Config["DXC_LINUXX64"], "obj/dxc.tar.gz");
                }
                using var tar = new GZipStream(File.OpenRead("obj/dxc.tar.gz"), CompressionMode.Decompress);
                TarFile.ExtractToDirectory(tar, "bin/builddeps", true);
                Console.WriteLine("Pre-built dxc extracted");
                return;
            }
            throw new Exception(
                $"dxc not available, and platform is {rid}. Please install from source: https://github.com/microsoft/DirectXShaderCompiler");
        }

        private static string VersionString;
        public static void Targets()
        {
            if(parallel > 0) Dotnet.CPUCount = parallel;
            /* webhook things */
            Target("default", DependsOn("BuildAll"));
            Target("BuildAll", DependsOn("BuildEngine", "BuildSdk"));

            Target("GenerateVersion", () =>
            {
                var version = versionSetting;
                if (version == "git")
                {
                    var lastSha = Git.ShaTip(".");
                    version = string.Format("{0}-git ({1})", lastSha.Substring(0, 7),
                        DateTime.Now.ToString("yyyyMMdd"));
                }

                string msvcDefine = IsWindows ? "<DefineConstants>MSVC_BUILD</DefineConstants>" : "";
                string commonVersion =
                    $"<!-- This file is AutoGenerated -->\n<Project><PropertyGroup><IncludeSourceRevisionInInformationalVersion>false</IncludeSourceRevisionInInformationalVersion><InformationalVersion>{version}</InformationalVersion>{msvcDefine}</PropertyGroup></Project>";
                Console.WriteLine($"Version: {version}");
                VersionString = version;
                if (!File.Exists("./src/CommonVersion.props") ||
                    File.ReadAllText("./src/CommonVersion.props") != commonVersion)
                {
                    File.WriteAllText("./src/CommonVersion.props", commonVersion);
                    Console.WriteLine("Updated version file ./src/CommonVersion.props");
                }
            });

            static string GetFileArgs(string dir, string glob)
            {
                return string.Join(" ", Directory.GetFiles(dir, glob).Select(x => Quote(x)));
            }
            Target("BuildShaders", () =>
            {
                Directory.CreateDirectory("shaders/natives/bin");
                if (IsWindows) {
                    CMake.Run("shaders/natives", new CMakeSettings() {
                        OutputPath = "shaders/natives/bin",
                        Generator = "Visual Studio 17 2022",
                        Platform = "x64",
                        BuildType = "MinSizeRel"
                    });
                    MSBuild.Run("./shaders/natives/bin/shadercompiler.sln", "/m /p:Configuration=MinSizeRel", VSVersion.VS2022, MSBuildPlatform.x86);
                } else {
                    CMake.Run("shaders/natives", new CMakeSettings()
                    {
                        OutputPath = "shaders/natives/bin",
                        Options = new[] { "-DCMAKE_INSTALL_PREFIX=" + prefix },
                        BuildType = "MinSizeRel"
                    });
                    string pl = "";
                    if (parallel > 0) pl = "-j" + parallel;
                    RunCommand("make", pl, "shaders/natives/bin");
                }
                var args =  $"-d LibreLancer.Graphics.RenderContext -b -t ShaderVariables -c ShaderVariables.Compile -x ShaderVariables.Log -n LibreLancer.Shaders -o ./src/LibreLancer/Shaders {GetFileArgs("./shaders/","*.glsl")}";
                Dotnet.Run("./shaders/ShaderProcessor/ShaderProcessor.csproj", args);
            });

            Target("ShaderDependencies", () =>
            {
                Directory.CreateDirectory("bin/builddeps");
                Directory.CreateDirectory("obj/spirvcross");
                if (IsWindows) {
                    CMake.Run("extern/SPIRV-Cross", new CMakeSettings() {
                        OutputPath = "obj/spirvcross",
                        Generator = "Visual Studio 17 2022",
                        Platform = "x64",
                        BuildType = "MinSizeRel",
                        Options = new[] { "-DSPIRV_CROSS_SHARED=ON", "-DSPIRV_CROSS_STATIC=OFF", "-DSPIRV_CROSS_CLI=OFF", "-DSPIRV_CROSS_ENABLE_TESTS=OFF"}
                    });
                    MSBuild.Run("./obj/spirvcross/SPIRV-Cross.sln", "/m /p:Configuration=MinSizeRel", VSVersion.VS2022, MSBuildPlatform.x86);
                } else {
                    CMake.Run("extern/SPIRV-Cross", new CMakeSettings()
                    {
                        OutputPath = "obj/spirvcross",
                        BuildType = "MinSizeRel",
                        Options = new[] { "-DSPIRV_CROSS_SHARED=ON", "-DSPIRV_CROSS_STATIC=OFF", "-DSPIRV_CROSS_CLI=OFF", "-DSPIRV_CROSS_ENABLE_TESTS=OFF"}
                    });
                    string pl = "";
                    if (parallel > 0) pl = "-j" + parallel;
                    RunCommand("make", pl, "obj/spirvcross");
                }
                CopyDirContents("obj/spirvcross", "bin/builddeps", false, "*.so");
                CopyDirContents("obj/spirvcross", "bin/builddeps", false, "*.dll");
                if(IsWindows)
                {
                    CopyDirContents("obj/spirvcross/MinSizeRel", "bin/builddeps", false, "*.dll");
                }
                FindDXC();
                Dotnet.BuildDebug("src/LLShaderCompiler/LLShaderCompiler.csproj");
            });

            Target("BuildNatives", () =>
            {
                if (buildDebug) Console.WriteLine("Building natives with debug info");
                if (buildO0) Console.WriteLine("Building natives with O0 level debug");
                Directory.CreateDirectory("obj");
                Directory.CreateDirectory("bin/natives/x86");
                Directory.CreateDirectory("bin/natives/x64");
                string config = buildDebug ? "RelWithDebInfo" : "MinSizeRel";
                if(buildO0)
                    config = "Debug";
                if (IsWindows)
                {
                    Directory.CreateDirectory("obj/x86");
                    Directory.CreateDirectory("obj/x64");
                    CopyDirContents("./deps/x64/", "./bin/natives/x64", false, "*.dll");
                    if (withWin32)
                    {
                        CopyDirContents("./deps/x86/", "./bin/natives/x86", false, "*.dll");
                        //build 32-bit
                        CMake.Run(".", new CMakeSettings()
                        {
                            OutputPath = "obj/x86",
                            Generator = "Visual Studio 17 2022",
                            Platform = "Win32",
                            BuildType = config
                        });
                        MSBuild.Run("./obj/x86/librelancernatives.sln", $"/m /p:Configuration={config}",
                            VSVersion.VS2022, MSBuildPlatform.x86);
                        CopyDirContents("./obj/x86/binaries/", "./bin/natives/x86", false, "*.dll");
                        if (buildDebug || buildO0) CopyDirContents("./obj/x86/binaries/", "./bin/natives/x86", false, "*.pdb");
                    }
                    //build 64-bit
                    CMake.Run(".", new CMakeSettings() {
                        OutputPath = "obj/x64",
                        Generator = "Visual Studio 17 2022",
                        Platform = "x64",
                        BuildType = config
                    });
                    MSBuild.Run("./obj/x64/librelancernatives.sln", $"/m /p:Configuration={config}", VSVersion.VS2022, MSBuildPlatform.x64);
                    CopyDirContents("./obj/x64/binaries/", "./bin/natives/x64", false, "*.dll");
                    if (buildDebug || buildO0) CopyDirContents("./obj/x64/binaries/", "./bin/natives/x64", false, "*.pdb");

                }
                else
                {
                    string args = "";
                    if (parallel > 0) args = "-j" + parallel;

                    if (withWin32) {
                        Directory.CreateDirectory("obj/x86-mingw");
                        CopyDirContents("./deps/x86/", "./bin/natives/x86", false, "*.dll");
                        CMake.Run(".", new CMakeSettings()
                        {
                            OutputPath = "obj/x86-mingw",
                            Options = new[] { "-DCMAKE_INSTALL_PREFIX=" + prefix, "-DCMAKE_TOOLCHAIN_FILE=./scripts/mingw-w64-i686.cmake" },
                            BuildType = config
                        });
                        RunCommand("make", args, "obj/x86-mingw");
                        CopyDirContents("obj/x86-mingw/binaries/", "./bin/natives/x86/", false, "*.dll");
                        MingwDeps.CopyMingwDependencies("i686-w64-mingw32","./bin/natives/x86/");
                    }
                    if (withWin64)
                    {
                        Directory.CreateDirectory("obj/x64-mingw");
                        CopyDirContents("./deps/x64/", "./bin/natives/x64", false, "*.dll");
                        CMake.Run(".", new CMakeSettings()
                        {
                            OutputPath = "obj/x64-mingw",
                            Options = new[] { "-DCMAKE_INSTALL_PREFIX=" + prefix, "-DCMAKE_TOOLCHAIN_FILE=./scripts/mingw-w64-x86_64.cmake" },
                            BuildType = config
                        });
                        RunCommand("make", args, "obj/x64-mingw");
                        CopyDirContents("obj/x64-mingw/binaries/", "./bin/natives/x64");
                        MingwDeps.CopyMingwDependencies("x86_64-w64-mingw32","./bin/natives/x64/");
                    }
                    //Build linux
                    CMake.Run(".", new CMakeSettings()
                    {
                        OutputPath = "obj",
                        Options = new[] { "-DCMAKE_INSTALL_PREFIX=" + prefix },
                        BuildType = config
                    });
                    RunCommand("make", args, "obj");
                    CopyDirContents("obj/binaries/", "./bin/natives/");
                }
            });

            Target("Clean", () =>
            {
                Clean();
            });


            Target("BuildEngine", DependsOn("GenerateVersion", "BuildNatives", "ShaderDependencies"), async () =>
            {
                if(withWin32)
                    await FullBuild("win-x86", false);
                if(IsWindows || withWin64)
                    await FullBuild("win-x64", false);
                if(!IsWindows)
                    await FullBuild(GetLinuxRid(), false);
            });
            Target("BuildDocumentation", DependsOn("GenerateVersion", "ShaderDependencies"), () =>
            {
                string[] apiDlls = new string[]
                {
                    "LibreLancer.ContentEdit.dll"
                };
                Dotnet.BuildRelease("./src/Editor/LancerEdit/LancerEdit.csproj");

                DocumentationBuilder.BuildDocs("./docs/", "./bin/docs/", VersionString,
                    apiDlls.Select(x => Path.Combine("./src/Editor/LancerEdit/bin/Release/net8.0", x)));
            });
            Target("BuildSdk", DependsOn("GenerateVersion", "BuildDocumentation", "BuildNatives", "ShaderDependencies"), async () =>
            {
                if(withWin32)
                    await FullBuild("win-x86", true);
                if(IsWindows || withWin64)
                    await FullBuild("win-x64", true);
                if(!IsWindows)
                    await FullBuild(GetLinuxRid(), true);
            });
            Target("Test", DependsOn("GenerateVersion", "ShaderDependencies"), () => {
                Dotnet.Test("./src/LibreLancer.Tests/LibreLancer.Tests.csproj");
                Console.WriteLine("Testing compile of editor scripts");
                var files = Directory.GetFiles("./src/Editor/LancerEdit/editorscripts", "*.cs-script").Select(Quote);
                var args = $"--test-compile {string.Join(' ', files)}";
                Dotnet.Run("./src/Editor/lleditscript/lleditscript.csproj", args);
            });

            static void TarDirectory(string file, string dir)
            {
                Bash($"tar -I 'gzip -9' -cf {Quote(file)} -C {Quote(dir)} --xform s:'./':: .", true);
            }

            static void ZipDirectory(string file, string dir)
            {
                Bash($"cd {Quote(dir)} && zip -qq -r -9 - . > {Quote(Path.GetFullPath(file))}", true);
            }

            Target("BuildAndTest", DependsOn("BuildAll", "Test"));

            Target("LinuxDaily", DependsOn("BuildEngine", "BuildSdk", "Test"), async () =>
            {
                RmDir("packaging/packages/a");
                RmDir("packaging/packages/b");
                Directory.CreateDirectory("packaging/packages/a");
                Directory.CreateDirectory("packaging/packages/b");
                var lastCommit = Git.ShaTip(".");
                Console.WriteLine("Packaging Linux");
                string engineNamePrefix = "librelancer-" + (versionSetting == "git"
                    ? lastCommit.Substring(0, 7)
                    : versionSetting);
                string sdkNamePrefix = "librelancer-" + (versionSetting == "git"
                    ? lastCommit.Substring(0, 7)
                    : versionSetting);
                //Engine
                var name = $"{engineNamePrefix}-ubuntu-amd64";
                var linuxEngine = Task.Run(() =>
                {
                    CopyDirContents("bin/librelancer-" + GetLinuxRid(), "packaging/packages/a/" + name, true);
                    TarDirectory("packaging/packages/librelancer-daily-ubuntu-amd64.tar.gz", "packaging/packages/a");
                    RmDir("packaging/packages/a");
                });
                //Sdk
                name = $"{sdkNamePrefix}-ubuntu-amd64";
                var linuxSdk = Task.Run(() =>
                {
                    CopyDirContents("bin/librelancer-sdk-" + GetLinuxRid(), "packaging/packages/b/" + name, true);
                    TarDirectory("packaging/packages/librelancer-sdk-daily-ubuntu-amd64.tar.gz",
                        "packaging/packages/b");
                    RmDir("packaging/packages/b");
                });
                await linuxEngine;
                await linuxSdk;
                if (withWin64)
                {
                    Console.WriteLine("Packaging win64");
                    RmDir("packaging/packages/c");
                    RmDir("packaging/packages/d");
                    Directory.CreateDirectory("packaging/packages/c");
                    Directory.CreateDirectory("packaging/packages/d");
                    //Engine
                    name = $"{engineNamePrefix}-win-x64";
                    var winEngine = Task.Run(() =>
                    {
                        CopyDirContents("bin/librelancer-win-x64", "packaging/packages/c/" + name, true);
                        ZipDirectory("packaging/packages/librelancer-daily-win64.zip", "packaging/packages/c");
                        RmDir("packaging/packages/c");
                    });
                    //Sdk
                    name = $"{sdkNamePrefix}-win-x64";
                    var winSdk = Task.Run(() =>
                    {
                        CopyDirContents("bin/librelancer-sdk-win-x64", "packaging/packages/d/" + name, true);
                        ZipDirectory("packaging/packages/librelancer-sdk-daily-win64.zip", "packaging/packages/d");
                        RmDir("packaging/packages/d");
                    });
                    await winEngine;
                    await winSdk;
                }
                //Timestamp
                var unixTime = (long)((Invoked - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds);
                File.WriteAllText("packaging/packages/timestamp", unixTime.ToString());
            });
        }
    }
}
