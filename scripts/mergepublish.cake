using IO = System.IO;
using System.Security.Cryptography;

void CopyFilesRecursively (DirectoryInfo source, DirectoryInfo target) {
    foreach (DirectoryInfo dir in source.GetDirectories())
        CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
    foreach (FileInfo file in source.GetFiles()) {
        file.CopyTo(IO.Path.Combine(target.FullName, file.Name), true);
    }
}

void DeleteFilesGlob(string dir, params string[] globs)
{
	var inf = new DirectoryInfo(dir);
	foreach(var glob in globs) {
		foreach(var f in inf.GetFiles(glob)) {
			f.Delete();
		}
	}
}

void MergePublish(string sourceDir, string outputDir, string rid)
{

var splitRID = rid.Split('-');
var win32 = splitRID[0].ToLowerInvariant() == "win7";
var arch = splitRID[splitRID.Length - 1].ToLowerInvariant();

Func<string, string> CalculateMD5 = (filename) =>
{
    using (var md5 = MD5.Create())
    {
        using (var stream = IO.File.OpenRead(filename))
        {
            var hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
};

var hashes = new Dictionary<string,string>();

bool valid = true;
foreach(var dir in IO.Directory.GetDirectories(sourceDir)) {
    Information($"Validating {dir}");
    foreach(var file in IO.Directory.GetFiles(dir)) {
        var fname = IO.Path.GetFileName(file);
        var md5 = CalculateMD5(file);
        if(hashes.TryGetValue(fname, out string oldmd5)) {
            if(oldmd5 != md5) {
                Error($"{fname} MD5 mismatch");
                valid = false;
            }
        } else
            hashes[fname] = md5;
    }
}
if(!valid) {
    throw new Exception("Publish validation failed");
}
IO.Directory.CreateDirectory(outputDir);
var output = new DirectoryInfo(outputDir);
foreach(var dir in new DirectoryInfo(sourceDir).GetDirectories()) {
    Information($"Copying {dir}");
    CopyFilesRecursively(dir, output);
}
Information("Cleaning");

DeleteFilesGlob(outputDir,
	"*.pdb",
	"*.json",
	"createdump",
	"SOS_README.md"
);

if(!win32 || arch == "x64") EnsureDirDeleted(PathCombine(outputDir, "x86"));
if(!win32 || arch =="x86") EnsureDirDeleted(PathCombine(outputDir, "x64"));
	
Information("Publish Complete");
}

