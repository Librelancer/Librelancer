bool CheckCommand_Unix(string cmd) => (StartProcess("/bin/sh", new ProcessSettings() { Arguments = string.Format("-c \"command -v {0}\"",cmd) }) == 0);

string PathCombine(string a, string b) => System.IO.Path.Combine(a,b);

string GitLogTip_Shell()
{
	if(IsRunningOnWindows()) return GitLogTip(".").Sha;
	//Linux: Cake.Git seems to intermittently fail with method body errors
	//call git from shell first
	if(!CheckCommand_Unix("git")) {
		Warning("BUG: Git not found in PATH, GenerateVersion may fail!");
		return GitLogTip(".").Sha;
	}
	IEnumerable<string> gitOutput;
	StartProcess("git", new ProcessSettings { Arguments = "rev-parse HEAD", RedirectStandardOutput = true }, out gitOutput);
	return gitOutput.FirstOrDefault() ?? "invalid";
}

void EnsureDirDeleted(string directory)
{
    if(DirectoryExists(directory)) {
        var settings = new DeleteDirectorySettings();
        settings.Recursive = true;
		settings.Force = true;
        DeleteDirectory(directory, settings);
    }
}

string GetLinuxRid()
{
    IEnumerable<string> output;
	StartProcess("uname", new ProcessSettings { Arguments = "-m", RedirectStandardOutput = true }, out output);
	string uname = output.FirstOrDefault();
	if(string.IsNullOrEmpty(uname)) return "linux-x64";
	uname = uname.Trim().ToLowerInvariant();
	if(uname.StartsWith("aarch64"))
        return "linux-arm64";
    if(uname.StartsWith("armv"))
        return "linux-arm";
    if(uname.StartsWith("x86_64"))
        return "linux-x64";
    return "linux-x86";
}
