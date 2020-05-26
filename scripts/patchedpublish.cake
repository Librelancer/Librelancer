using IO = System.IO;
using System.Text;

void DeleteFilesGlob(string dir, params string[] globs)
{
	var inf = new DirectoryInfo(dir);
	foreach(var glob in globs) {
		foreach(var f in inf.GetFiles(glob)) {
			f.Delete();
		}
	}
}

void PatchedPublish(string proj, string outputDirectory, string rid)
{
	if(!DirectoryExists(outputDirectory)) CreateDirectory(outputDirectory);
    string publishDir = IO.Path.Combine(outputDirectory,"lib");
    //Publish
    var publishSettings = new DotNetCorePublishSettings
    {
        Configuration = "Release",
        OutputDirectory = publishDir,
        SelfContained = true,
        Runtime = rid
    };
    DotNetCorePublish(proj, publishSettings);
    //Delete junk files
    DeleteFilesGlob(publishDir,
        "*.pdb",
        "*.json",
        "createdump",
        "SOS_README.md"
    );
    //TODO: Fix this msbuild side
    if(rid == "win7-x86") {
        IO.Directory.Delete(IO.Path.Combine(publishDir,"x64"));
    }
    if(rid == "win7-x64") {
        IO.Directory.Delete(IO.Path.Combine(publishDir,"x86"));
    }
    //Move the AppHost
    var apphostName = IO.Path.GetFileNameWithoutExtension(proj);
    var origName = apphostName + ".dll";
    if(IsRunningOnWindows()) apphostName += ".exe";
    string appHostPath = IO.Path.Combine(outputDirectory, apphostName);
    if(IO.File.Exists(appHostPath)) IO.File.Delete(appHostPath);
    IO.File.Move(IO.Path.Combine(publishDir, apphostName), appHostPath);
    //Patch the AppHost
    var origPathBytes = Encoding.UTF8.GetBytes(origName + "\0");
    var libDir = IsRunningOnWindows() ? "lib\\" : "lib/";
    var newPath = libDir + origName;
    var newPathBytes = Encoding.UTF8.GetBytes(newPath + "\0");
    var apphostExe = IO.File.ReadAllBytes(appHostPath);
    int offset = FindBytes(apphostExe, origPathBytes);
    if(offset < 0) {
        throw new Exception("Could not patch apphost " + appHostPath);
    }
    for(int i = 0; i < newPathBytes.Length; i++)
        apphostExe[offset + i] = newPathBytes[i];
    IO.File.WriteAllBytes(appHostPath, apphostExe);
}

static int FindBytes(byte[] bytes, byte[] pattern) {
    int idx = 0;
    var first = pattern[0];
    while (idx < bytes.Length) {
        idx = Array.IndexOf(bytes, first, idx);
		if (idx < 0) break; //Not Found
        if (BytesEqual(bytes, idx, pattern))
            return idx;
        idx++;
    }
    return -1;
}

static bool BytesEqual(byte[] bytes, int index, byte[] pattern) {
    if (index + pattern.Length > bytes.Length)
        return false;
    for (int i = 0; i < pattern.Length; i++) {
        if (bytes[index + i] != pattern[i])
            return false;
	}
	return true;
}
