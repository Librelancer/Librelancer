using IO = System.IO;
using System.Security.Cryptography;

void CopyFilesRecursively (DirectoryInfo source, DirectoryInfo target) {
    foreach (DirectoryInfo dir in source.GetDirectories())
        CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
    foreach (FileInfo file in source.GetFiles()) {
        file.CopyTo(IO.Path.Combine(target.FullName, file.Name), true);
    }
}

void MergePublish(string sourceDir, string outputDir)
{

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
foreach(var f in output.GetFiles("*.pdb"))
    f.Delete();
foreach(var f in output.GetFiles("*.json"))
    f.Delete();
if(IO.File.Exists(IO.Path.Combine(outputDir, "createdump")))
    IO.File.Delete(IO.Path.Combine(outputDir, "createdump"));
if(IO.File.Exists(IO.Path.Combine(outputDir, "SOS_README.md")))
    IO.File.Delete(IO.Path.Combine(outputDir, "SOS_README.md"));
Information("Publish Complete");
}

