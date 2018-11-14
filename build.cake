#addin nuget:?package=Cake.Git
#addin nuget:?package=Cake.CMake
#addin nuget:?package=Cake.FileHelpers
#addin nuget:?package=SharpZipLib
#addin nuget:?package=Cake.Compression

var target = Argument("target", "Build");
var versionSetting = Argument("assemblyversion","git");
var configuration = Argument("configuration","Release");

string GitLogTip_Shell()
{
	if(IsRunningOnWindows()) return GitLogTip(".").Sha;
	//Linux: Cake.Git seems to intermittently fail with method body errors
	//call git from shell first
	using(var process = StartAndReturnProcess("/bin/sh", new ProcessSettings() { Arguments = "-c 'command -v git'" })) {
		process.WaitForExit();
		if(process.GetExitCode() != 0) {
			Warning("BUG: Git not found in PATH, GenerateVersion may fail!");
			return GitLogTip(".").Sha;
		}
	}
	IEnumerable<string> gitOutput;
	StartProcess("git", new ProcessSettings { Arguments = "rev-parse HEAD", RedirectStandardOutput = true }, out gitOutput);
	return gitOutput.FirstOrDefault() ?? "badversion";
}

Task("GenerateVersion")
	.Does(() =>
{
	var version = versionSetting;
	if(version == "git") {
		var lastSha = GitLogTip_Shell();
		version = lastSha.Substring(0,7) + "-git";
	}
	CreateAssemblyInfo("./src/CommonVersion.cs", new AssemblyInfoSettings {
		InformationalVersion = version
	});
	Information("Version: {0}",version);
});

Task("BuildNatives")
    .Does(() =>
{
	//Ensure Directories Exist
	if(!DirectoryExists("obj")) CreateDirectory("obj");
	if(!DirectoryExists("bin")) CreateDirectory("bin");
	if(!DirectoryExists("bin/Debug")) CreateDirectory("bin/Debug");
	if(!DirectoryExists("bin/Release")) CreateDirectory("bin/Release");
	//Build CMake
	if(IsRunningOnWindows())
	{
		//More directories! (this build is involved af)
		if(!DirectoryExists("obj/x86")) CreateDirectory("obj/x86");
		if(!DirectoryExists("obj/x64")) CreateDirectory("obj/x64");
		if(!DirectoryExists("bin/Debug/x86")) CreateDirectory("bin/Debug/x86");
		if(!DirectoryExists("bin/Debug/x64")) CreateDirectory("bin/Debug/x64");
		if(!DirectoryExists("bin/Release/x86")) CreateDirectory("bin/Release/x86");
		if(!DirectoryExists("bin/Release/x64")) CreateDirectory("bin/Release/x64");
		//x86 build first!
		CMake(".", new CMakeSettings() {
			OutputPath = "obj/x86",
			Generator = "Visual Studio 15 2017"
		});
		MSBuild("./obj/x86/librelancernatives.sln", new MSBuildSettings() {
			MaxCpuCount = 0, Configuration = "Release", ToolVersion = MSBuildToolVersion.VS2017
		});
		CopyFiles("./obj/x86/binaries/*.dll", "./bin/Debug/x86/");
		CopyFiles("./obj/x86/binaries/*.dll", "./bin/Release/x86/");
		//Then x64
		CMake(".", new CMakeSettings() {
			OutputPath = "obj/x64",
			Generator = "Visual Studio 15 2017 Win64"
		});
		MSBuild("./obj/x64/librelancernatives.sln", new MSBuildSettings() {
			MaxCpuCount = 0, Configuration = "Release", ToolVersion = MSBuildToolVersion.VS2017
		});
		CopyFiles("./obj/x64/binaries/*.dll", "./bin/Debug/x64/");
		CopyFiles("./obj/x64/binaries/*.dll", "./bin/Release/x64/");
	}
	else
	{
		CMake(".",new CMakeSettings() {
			OutputPath = "obj"
		});
		using(var process = StartAndReturnProcess("make", new ProcessSettings() { WorkingDirectory = "obj" })) {
			process.WaitForExit();
			var code = process.GetExitCode();
			if(code != 0) {
				throw new Exception("Make exited with error code " + code);
			}
		}
		CopyFiles("obj/binaries/*","./bin/Debug/");
		CopyFiles("obj/binaries/*","./bin/Release/");
	}

});

Task("Build")
	  .IsDependentOn("GenerateVersion")
      .IsDependentOn("BuildNatives")
	  .Does(() =>
{
	//Restore NuGet packages
	NuGetRestore("./src/LibreLancer.sln");
	//Build C#
	if(IsRunningOnWindows())
	{
		MSBuild("./src/LibreLancer.sln", settings => settings.SetConfiguration(configuration));
	}
	else
	{
		//Check for real MSBuild on Unix
		bool useMSBuild = false;
		using(var process = StartAndReturnProcess("/bin/sh", new ProcessSettings() { Arguments = "-c 'command -v msbuild'" })) {
			process.WaitForExit();
			useMSBuild = (process.GetExitCode() == 0);
		}
		//XBuild is also fine
		if(useMSBuild)
			MSBuild("./src/LibreLancer.sln", settings => settings.SetConfiguration(configuration));
		else
			XBuild("./src/LibreLancer.sln", settings => settings.SetConfiguration(configuration));
	}
});

Task("LinuxDaily")
    .IsDependentOn("Build")
    .Does(() =>
{
	if(!DirectoryExists("packaging/packages")) CreateDirectory("packaging/packages");
	var lastCommit = GitLogTip(".");
	var name = "librelancer-" + lastCommit.Sha.Substring(0,7) + "-ubuntu-amd64";
	if(DirectoryExists("packaging/packages/" + name))
		CleanDirectories("packaging/packages/" + name);
	else
		CreateDirectory("packaging/packages/" + name);
	CopyFiles("bin/Release/*","packaging/packages/" + name);
	DeleteFiles("packaging/packages/" + name + "/*.pdb");
	DeleteFiles("packaging/packages/" + name + "/*.xml");
	GZipCompress("packaging/packages/",
				"packaging/packages/" + name + ".tar.gz", 
				GetFiles("packaging/packages/" + name + "/*")
	);
	DeleteDirectory("packaging/packages/" + name, recursive:true);
	var unixTime = (long)((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds);
	FileWriteText("packaging/packages/timestamp",unixTime.ToString());
});

RunTarget(target);
