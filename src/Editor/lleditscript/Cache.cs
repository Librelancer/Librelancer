using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace lleditscript;

public static class Cache
{
    static readonly string MyId = typeof(Cache).Assembly.ManifestModule.ModuleVersionId.ToString();

    static readonly string CacheDirectory;

    private const long CacheMax = 8 * 1024 * 1024; //8 MiB cache
    private const long CacheItemMax = 4 * 1024 * 1024; //If assembly is >4MiB, forget it
    private const long CodeLengthMax = 1 * 1024 * 1024; //If code is > 1MiB, forget it


    static string GetCacheDir()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData
                ),
                "lleditscript"
            );
        }
        else
        {
            string osConfigDir = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
            if (String.IsNullOrEmpty(osConfigDir))
            {
                osConfigDir = Environment.GetEnvironmentVariable("HOME");
                if (String.IsNullOrEmpty(osConfigDir))
                {
                    return "./cache"; // Oh well.
                }
                osConfigDir += "/.cache";
            }
            return Path.Combine(osConfigDir, "lleditscript");
        }
    }

    static Cache()
    {
        CacheDirectory = GetCacheDir();
    }

    static void WithMutex(Action action)
    {
        string mutexName = $"Global\\{Environment.UserName}_{Environment.UserDomainName}_lleditscript";
        using var mutex = new Mutex(false, mutexName);
        if (!mutex.WaitOne(8000)) {
            return;
        }
        try
        {
            action();
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }

    private const string ALPHABET = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_";

    static string CacheID(string code)
    {
        var data = SHA256.HashData(Encoding.UTF8.GetBytes(MyId + "\n" + code));
        var builder = new StringBuilder();
        builder.Append("0");
        var val = new BigInteger(data, true);
        var divisor = ALPHABET.Length;
        while (val > 0)
        {
            val = BigInteger.DivRem(val, divisor, out var rem);
            builder.Append(ALPHABET[(int)rem]);
        }
        return builder.ToString();
    }

    record ManifestItem(string Name, long Size, DateTime Created);

    class Manifest
    {
        public List<ManifestItem> Items { get; set; } = new List<ManifestItem>();
    }

    public static byte[] GetCacheItem(string code)
    {
        byte[] result = null;
        if (!Directory.Exists(CacheDirectory))
            return null;
        if (code.Length > CodeLengthMax)
            return null;
        var f = Path.Combine(CacheDirectory, CacheID(code)) + ".dll.zst";
        WithMutex(() =>
        {
            if (File.Exists(f)) result = File.ReadAllBytes(f);
        });
        return result;
    }

    public static void WriteCacheItem(string code, byte[] data)
    {
        if (data.Length > CacheItemMax ||
            code.Length > CodeLengthMax)
            return;

        var id = CacheID(code) + ".dll.zst";
        var p = Path.Combine(CacheDirectory, id);
        WithMutex(() =>
        {
            Directory.CreateDirectory(CacheDirectory);
            Manifest manifest = new Manifest();
            var manifestPath = Path.Combine(CacheDirectory, "manifest.json");
            try
            {
                if(File.Exists(manifestPath))
                {
                    using var f = File.OpenRead(manifestPath);
                    manifest = JsonSerializer.Deserialize<Manifest>(f);
                }
            }
            catch (Exception)
            {
                manifest = new Manifest();
            }

            var totalSize = manifest.Items.Select(x => x.Size).Sum() + data.Length;
            if (totalSize > CacheMax)
            {
                try
                {
                    // just delete all cached code if we get too big
                    Directory.Delete(CacheDirectory, true);
                    manifest.Items.Clear();
                }
                catch (Exception)
                {
                }
                Directory.CreateDirectory(CacheDirectory);
            }
            manifest.Items.Add(new ManifestItem(id, totalSize, DateTime.Now));
            File.WriteAllBytes(p, data);
            using var m2 = File.Create(manifestPath);
            JsonSerializer.Serialize(m2, manifest);
        });
    }
}
