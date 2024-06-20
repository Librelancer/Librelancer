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

        public static async Task Merge(string sourceDir, string outputDir, string rid, string[] directories)
        {
            var splitRID = rid.Split('-');
            var win32 = splitRID[0].ToLowerInvariant() == "win7";
            var arch = splitRID[splitRID.Length - 1].ToLowerInvariant();

            var hashes = new Dictionary<string,string>();

            bool valid = true;
            foreach(var dir in Directory.GetDirectories(sourceDir))
            {
                if (!directories.Contains(Path.GetFileNameWithoutExtension(dir))) continue;
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
            Directory.CreateDirectory(outputDir);
            var output = new DirectoryInfo(outputDir);
            var copiedFiles = new List<string>();

            foreach(var dir in new DirectoryInfo(sourceDir).GetDirectories()) {
                if (!directories.Contains(Path.GetFileNameWithoutExtension(dir.Name))) continue;
                Console.WriteLine($"Copying {dir}");
                CopyFilesRecursively(dir, output, copiedFiles);
            }

            if(!win32 || arch == "x64") RmDir(Path.Combine(outputDir, "x86"));
            if(!win32 || arch =="x86") RmDir(Path.Combine(outputDir, "x64"));

            Console.WriteLine("Publish Complete");
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

        private const int E_LFANEW = 0x3C;
        private const int SUBSYSTEM_OFFSET = 0x5C;
        private static Regex winExe = new Regex(@"<\s*OutputType\s*>\s*WinExe", RegexOptions.IgnoreCase);
        public static void PatchedPublish(string proj, string outputDirectory, string rid)
        {
            Directory.CreateDirectory(outputDirectory);
            string publishDir = Path.Combine(outputDirectory,"lib");
            //Publish
            var publishSettings = new DotnetPublishSettings
            {
                Configuration = "Release",
                OutputDirectory = publishDir,
                SelfContained = true,
                Runtime = rid
            };
            Dotnet.Publish(proj, publishSettings);
            //Delete junk files
            DeleteFilesGlob(publishDir,
                "*.pdb",
                "createdump",
                "SOS_README.md"
            );
            //TODO: Fix this msbuild side
            if(!rid.Contains("win")) {
                RmDir(Path.Combine(publishDir, "x64"));
            }
            //Move the AppHost
            var apphostName = Path.GetFileNameWithoutExtension(proj);
            var origName = apphostName + ".dll";
            if(rid.StartsWith("win", StringComparison.OrdinalIgnoreCase)) apphostName += ".exe";
            string appHostPath = Path.Combine(outputDirectory, apphostName);
            if(File.Exists(appHostPath)) File.Delete(appHostPath);
            File.Move(Path.Combine(publishDir, apphostName), appHostPath);
            //Patch the AppHost
            var origPathBytes = Encoding.UTF8.GetBytes(origName + "\0");
            var libDir = IsWindows ? "lib\\" : "lib/";
            var newPath = libDir + origName;
            var newPathBytes = Encoding.UTF8.GetBytes(newPath + "\0");
            var apphostExe = File.ReadAllBytes(appHostPath);
            int offset = FindBytes(apphostExe, origPathBytes);
            if(offset < 0) {
                throw new Exception("Could not patch apphost " + appHostPath);
            }
            for(int i = 0; i < newPathBytes.Length; i++)
                apphostExe[offset + i] = newPathBytes[i];
            if (!IsWindows && rid.StartsWith("win", StringComparison.OrdinalIgnoreCase))
            {
                if (winExe.IsMatch(File.ReadAllText(proj)))
                {
                    var peHeaderLocation = BitConverter.ToInt32(apphostExe, E_LFANEW);
                    var subsystemLocation = peHeaderLocation + SUBSYSTEM_OFFSET;
                    Console.WriteLine($"Patching subsystem for {appHostPath}");
                    var winexeBytes = BitConverter.GetBytes((ushort) Subsystem.WindowsGui);
                    apphostExe[subsystemLocation] = winexeBytes[0];
                    apphostExe[subsystemLocation + 1] = winexeBytes[1];
                }
            }
            File.WriteAllBytes(appHostPath, apphostExe);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                RunCommand("codesign", $"--force --deep -s - {Quote(appHostPath)}");
            }
        }
    }
}
