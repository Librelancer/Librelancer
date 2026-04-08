using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Runtime.InteropServices;
using static BuildLL.Runtime;

namespace BuildLL
{
    public static class CustomPublish
    {
        static async Task<string> CalculateMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = await md5.ComputeHashAsync(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
        public static void CopyFilesRecursively (DirectoryInfo source, DirectoryInfo target, List<string> copiedFiles = null) {
            foreach (DirectoryInfo dir in source.GetDirectories())
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name), copiedFiles);
            foreach (FileInfo file in source.GetFiles()) {
                string targetPath = Path.Combine(target.FullName,file.Name);
                if(copiedFiles == null || !copiedFiles.Contains(targetPath)) {
                    file.CopyTo(targetPath, true);
                    copiedFiles?.Add(targetPath);
                }
            }
        }

        public static async Task MergeAndPatch(string artifactsDir, string binDir, string rid, string[] projects)
        {
            var splitRID = rid.Split('-');
            var win32 = splitRID[0].ToLowerInvariant() == "win";
            var arch = splitRID[splitRID.Length - 1].ToLowerInvariant();

            var hashes = new Dictionary<string,string>();
            bool valid = true;

            var inputDirs =
                projects.Select(x => Path.Combine(artifactsDir, "publish", x, $"release_{rid}")).ToArray();


            foreach(var dir in inputDirs)
            {
                Console.WriteLine($"Validating {dir}");
                List<Task<(string File, string Hash)>> calculatedHashes = new List<Task<(string File, string Hash)>>();
                foreach(var file in Directory.GetFiles(dir,"*", SearchOption.AllDirectories))
                {
                    calculatedHashes.Add(Task.Run(async () => (file.Substring(dir.Length), await CalculateMD5(file))));
                }
                await Task.WhenAll(calculatedHashes);
                foreach (var result in calculatedHashes.Select(x => x.Result))
                {
                    if(hashes.TryGetValue(result.File, out string oldmd5)) {
                        if(oldmd5 != result.Hash) {
                            Console.Error.WriteLine($"{result.File} MD5 mismatch");
                            valid = false;
                        }
                    } else
                        hashes[result.File] = result.Hash;
                }
            }

            if (!valid) throw new Exception("Publish validation failed");

            var outputDir = Path.Combine(binDir, "lib");

            Directory.CreateDirectory(outputDir);
            var output = new DirectoryInfo(outputDir);
            var copiedFiles = new List<string>();

            foreach(var dir in inputDirs) {
                Console.WriteLine($"Copying {dir}");
                CopyFilesRecursively(new DirectoryInfo(dir), output, copiedFiles);
            }

            if(!win32 || arch == "x64") RmDir(Path.Combine(outputDir, "x86"));
            if(!win32 || arch =="x86") RmDir(Path.Combine(outputDir, "x64"));

            //Delete junk files
            DeleteFilesGlob(outputDir,
                "BepuPhysics.xml",
                "BepuUtilities.xml",
                "LiteNetLib.xml",
                "createdump",
                "SOS_README.md"
            );

            //
            foreach (var d in projects)
            {
                var filename = win32 ? d + ".exe" : d;
                File.Move(Path.Combine(outputDir, filename), Path.Combine(binDir, filename));
                PatchApphost(Path.Combine(binDir, filename), win32);
            }
        }

        static void DeleteFilesGlob(string dir, params string[] globs)
        {
            var inf = new DirectoryInfo(dir);
            foreach(var glob in globs) {
                foreach(var f in inf.GetFiles(glob)) {
                    f.Delete();
                }
            }
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

        public static void PatchApphost(string apphost, bool win32)
        {
            //Move the AppHost
            var apphostName = Path.GetFileNameWithoutExtension(apphost);
            var origName = apphostName + ".dll";
            //Patch the AppHost
            var origPathBytes = Encoding.UTF8.GetBytes(origName + "\0");
            var libDir = IsWindows ? "lib\\" : "lib/";
            var newPath = libDir + origName;
            var newPathBytes = Encoding.UTF8.GetBytes(newPath + "\0");
            var apphostExe = File.ReadAllBytes(apphost);
            int offset = FindBytes(apphostExe, origPathBytes);
            if(offset < 0) {
                throw new Exception("Could not patch apphost " + apphost);
            }
            for(int i = 0; i < newPathBytes.Length; i++)
                apphostExe[offset + i] = newPathBytes[i];
            File.WriteAllBytes(apphost, apphostExe);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                RunCommand("codesign", $"--force --deep -s - {Quote(apphost)}");
            }
        }
    }
}
