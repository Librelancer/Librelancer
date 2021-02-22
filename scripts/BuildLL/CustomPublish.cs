using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using static BuildLL.Runtime;

namespace BuildLL
{
    public static class CustomPublish
    {
        static string CalculateMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
        static void CopyFilesRecursively (DirectoryInfo source, DirectoryInfo target) {
            foreach (DirectoryInfo dir in source.GetDirectories())
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            foreach (FileInfo file in source.GetFiles()) {
                file.CopyTo(Path.Combine(target.FullName, file.Name), true);
            }
        }

        public static void Merge(string sourceDir, string outputDir, string rid)
        {
            var splitRID = rid.Split('-');
            var win32 = splitRID[0].ToLowerInvariant() == "win7";
            var arch = splitRID[splitRID.Length - 1].ToLowerInvariant();
            
            var hashes = new Dictionary<string,string>();

            bool valid = true;
            foreach(var dir in Directory.GetDirectories(sourceDir)) {
                Console.WriteLine($"Validating {dir}");
                foreach(var file in Directory.GetFiles(dir,"*", SearchOption.AllDirectories)) {
                    var fname = file.Substring(dir.Length);
                    var md5 = CalculateMD5(file);
                    if(hashes.TryGetValue(fname, out string oldmd5)) {
                        if(oldmd5 != md5) {
                            Console.Error.WriteLine($"{fname} MD5 mismatch");
                            valid = false;
                        }
                    } else
                        hashes[fname] = md5;
                }
            }

            if (!valid) throw new Exception("Publish validation failed");
            Directory.CreateDirectory(outputDir);
            var output = new DirectoryInfo(outputDir);
            foreach(var dir in new DirectoryInfo(sourceDir).GetDirectories()) {
                Console.WriteLine($"Copying {dir}");
                CopyFilesRecursively(dir, output);
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
                "*.json",
                "createdump",
                "SOS_README.md"
            );
            //TODO: Fix this msbuild side
            if(rid == "win7-x86") {
                RmDir(Path.Combine(publishDir, "x64"));
            }
            if(rid == "win7-x64")
            {
                RmDir(Path.Combine(publishDir, "x86"));
            }
            //Move the AppHost
            var apphostName = Path.GetFileNameWithoutExtension(proj);
            var origName = apphostName + ".dll";
            if(IsWindows) apphostName += ".exe";
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
            File.WriteAllBytes(appHostPath, apphostExe);
        }
        
    }
}