using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BepuUtilities;
using SharpDX.DirectWrite;
using ZstdSharp;

namespace LibreLancer.ContentEdit;

public class LrpkPack
{
    public record LrpkWildcard(LrpkPack.PackMethod Method, string Wildcard);

    public List<LrpkWildcard> Rules = new List<LrpkWildcard>();

    public event Action<string> Log;
    public bool Verbose = false;
    public int MaxThreads = 0;

    public LrpkPack(string rulesFile)
    {
        var lines = File.ReadAllLines(rulesFile);
        foreach (var ln in lines)
        {
            if (string.IsNullOrWhiteSpace(ln))
                continue;
            var idx = ln.IndexOf(':');
            if (idx == -1)
                continue;
            var rule = ln.Substring(0, idx).Trim();
            var wildcard = ln.Substring(idx + 1).Trim();
            Rules.Add(new LrpkWildcard(Enum.Parse<PackMethod>(rule, true), wildcard));
        }
    }

    public LrpkPack()
    {
    }

    public enum PackMethod
    {
        Auto,
        ZeroLength,
        Uncompressed,
        Zstandard,
        Block0,
        Reference
    }

    static Regex TranslateWildcard(string pattern)
    {
        var builder = new StringBuilder();
        for (int i = 0; i < pattern.Length; i++)
        {
            if (pattern[i] == '*')
            {
                if (i + 1 < pattern.Length && pattern[i + 1] == '*')
                {
                    builder.Append(".*\\/");
                    i++;
                }
                else
                {
                    builder.Append("[^/]*");
                }
            }
            else
            {
                builder.Append(Regex.Escape(pattern[i].ToString()));
            }
        }
        return new Regex(builder.ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    class MatchRule
    {
        public PackMethod Method;
        public Regex Regex;
        public string Wildcard;

        public MatchRule(PackMethod method, Regex regex, string wildcard)
        {
            Method = method;
            Regex = regex;
            Wildcard = wildcard;
        }

        public bool IsMatch(string path) =>
            Regex.IsMatch(path);
    }


    class PackItem
    {
        public bool IsDirectory = false;
        public string SourcePath;
        public string Name;
        public string FullPath;
        public PackMethod Method;
        public List<PackItem> Children;
        public MatchRule Matched;
        public long Offset;
        public long Length;
        public double TestRatio;

        public PackItem Referenced;
    }

    static void ApplyRules(PackItem item, MatchRule[] rules, string directoryPath)
    {
        if (item.IsDirectory)
        {
            foreach (var child in item.Children)
                ApplyRules(child, rules, (directoryPath == "" ? "" : directoryPath + "/") + item.Name);
        }
        else
        {
            var name = (directoryPath == "" ? "" : directoryPath + "/") + item.Name;
            foreach (var r in rules)
            {
                if (r.IsMatch(name))
                {
                    item.Method = r.Method;
                    item.Matched = r;
                }
            }
        }
    }

    static string ComputeSHA256(Stream data)
    {
        // Create a SHA256
        using (SHA256 sha256Hash = SHA256.Create())
        {
            // ComputeHash - returns byte array
            byte[] bytes = sha256Hash.ComputeHash(data);
            data.Position = 0;
            // Convert byte array to a string
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }

    void ResolveDuplicates(Dictionary<string, List<PackItem>> potentialDuplicates)
    {
        //Clear dictionary of all non-duplicates
        foreach (var k in potentialDuplicates.Keys.ToArray())
        {
            if (potentialDuplicates[k].Count <= 1)
                potentialDuplicates.Remove(k);
        }
        Span<byte> buffer1 = stackalloc byte[8192];
        Span<byte> buffer2 = stackalloc byte[8192];
        foreach (var kv in potentialDuplicates)
        {
            using var s1 = File.OpenRead(kv.Value[0].SourcePath);
            using var s2 = File.OpenRead(kv.Value[1].SourcePath);

            if (s1.Length != s2.Length)
                continue;
            int ret1 = 0;
            int ret2 = 0;
            bool equal = true;
            while ((ret1 = s1.Read(buffer1)) != 0)
            {
                ret2 = s2.Read(buffer2);
                if (ret2 != ret1) {
                    equal = false;
                    break;
                }
                if (!buffer1.SequenceEqual(buffer2)) {
                    equal = false;
                    break;
                }
            }
            if (equal) {
                for (int i = 1; i < kv.Value.Count; i++)
                {
                    if(Verbose)
                        Log?.Invoke($"DUP: {kv.Value[i].FullPath} == {kv.Value[0].FullPath}");
                    kv.Value[i].Method = PackMethod.Reference;
                    kv.Value[i].Referenced = kv.Value[0];
                }
            }
        }

    }

    void AutodetectCompress(Dictionary<string, List<PackItem>> potentialDuplicates, PackItem item, string path)
    {
        var fullPath = path == "" ? item.Name : path + "/" + item.Name;

        if (item.IsDirectory)
        {
            if (Verbose && Log != null)
            {
                Log($"Directory: {fullPath}");
            }
            foreach (var child in item.Children)
                AutodetectCompress(potentialDuplicates, child, fullPath);
        }
        else
        {
            using var f = File.OpenRead(item.SourcePath);
            if (f.Length != 0)
            {
                var hash = ComputeSHA256(f);
                if (!potentialDuplicates.TryGetValue(hash, out var lst)) {
                    lst = new List<PackItem>();
                    potentialDuplicates[hash] = lst;
                }
                lst.Add(item);
            }
            if (f.Length == 0)
                item.Method = PackMethod.ZeroLength;
            else if (item.Method == PackMethod.Auto)
            {
                var mem = new WriteStatisticsStream();
                using (var comp = new CompressionStream(mem, 3))
                    f.CopyTo(comp);
                var ratio = (double)mem.Length / f.Length;
                item.TestRatio = (ratio * 100);
                if (ratio <= 0.95)
                    item.Method = PackMethod.Zstandard;
                else
                {
                    //Try harder
                    f.Position = 0;
                    mem.Reset();
                    using (var comp = new CompressionStream(mem, 18))
                        f.CopyTo(comp);
                    ratio = (double)mem.Length / f.Length;
                    item.Method = ratio <= 0.95 ? PackMethod.Zstandard : PackMethod.Uncompressed;
                    item.TestRatio = (ratio * 100);
                }
            }

            if (Verbose && Log != null) {
                if (item.Matched != null)
                {
                    Log($"{item.Method.ToString().ToUpper()}: {fullPath} (rule: {item.Matched.Wildcard})");
                }
                else
                {
                    var rstring = item.Method == PackMethod.Uncompressed
                        ? $"{item.TestRatio:F2}% > 95%"
                        : $"{item.TestRatio:F2}% <= 95%";
                    Log($"{item.Method.ToString().ToUpper()}: {fullPath} (auto-detected {rstring})");
                }
            }
        }
    }


    IEnumerable<PackItem> IteratePack(PackItem item)
    {
        yield return item;
        if (item.IsDirectory) {
            foreach (var child in item.Children)
            {
                foreach (var x in IteratePack(child))
                    yield return x;
            }
        }
    }

    PackItem Iterate(DirectoryInfo info, string path = null)
    {
        var pk = new PackItem();
        pk.IsDirectory = true;
        pk.Children = new List<PackItem>();
        pk.Name = info.Name;
        pk.FullPath = path == null ? "" : $"{path}{info.Name}/";
        foreach (var d in info.EnumerateDirectories())
        {
            pk.Children.Add(Iterate(d, pk.FullPath));
        }
        foreach (var f in info.EnumerateFiles())
        {
            var child = new PackItem();
            child.IsDirectory = false;
            child.SourcePath = f.FullName;
            child.Name = f.Name;
            child.FullPath = $"{pk.FullPath}{f.Name}";
            pk.Children.Add(child);
        }
        return pk;
    }

    private PackItem sourceRoot;

    public void ReadSource(string directory)
    {
        Log?.Invoke($"Reading {directory}");
        var items = Iterate(new DirectoryInfo(directory));
        items.Name = "";

        Log?.Invoke("Applying rules");
        var rules = Rules.Select(x => new MatchRule(x.Method, TranslateWildcard(x.Wildcard), x.Wildcard)).ToArray();
        ApplyRules(items, rules, "");
        Log?.Invoke("Analyzing");
        var potentialDuplicate = new Dictionary<string, List<PackItem>>();
        AutodetectCompress(potentialDuplicate, items, "");
        ResolveDuplicates(potentialDuplicate);
        sourceRoot = items;
    }

    bool WriteBlock(PackMethod blockIdx, Stream outputStream, out long offset, out long length)
    {
        Log?.Invoke("Compressing Block0...");
        offset = outputStream.Position;
        var blk = IteratePack(sourceRoot).Where(x => x.Method == blockIdx).ToArray();
        if (blk.Length == 0) {
            length = 0;
            return false;
        }

        long fileOff = 0;
        using (var comp = new CompressionStream(outputStream, 22)) {
            foreach (var toComp in blk)
            {
                if(Verbose)
                    Log?.Invoke(toComp.FullPath);
                using var src = File.OpenRead(toComp.SourcePath);
                toComp.Offset = fileOff;
                toComp.Length = src.Length;
                fileOff += src.Length;
                src.CopyTo(comp);
            }
        }
        length = outputStream.Position - offset;
        return true;
    }

    void WriteTree(BinaryWriter writer, PackItem item)
    {
        if (item.IsDirectory)
        {
            writer.Write((byte)0);
            writer.WriteStringUTF8(item.Name);
            writer.WriteVarUInt64((ulong)item.Children.Count);
            foreach(var child in item.Children)
                WriteTree(writer, child);
        }
        else if (item.Method == PackMethod.Reference)
        {
            var type = item.Referenced.Method switch
            {
                PackMethod.ZeroLength => 1,
                PackMethod.Uncompressed => 2,
                PackMethod.Zstandard => 3,
                PackMethod.Block0 => 4,
                _ => throw new Exception("Internal pack error"),
            };
            writer.Write((byte)type);
            writer.WriteStringUTF8(item.Name);
            if (type != 1)
            {
                writer.WriteVarUInt64((ulong)item.Referenced.Offset);
                writer.WriteVarUInt64((ulong)item.Referenced.Length);
            }
        }
        else
        {
            var type = item.Method switch
            {
                PackMethod.ZeroLength => 1,
                PackMethod.Uncompressed => 2,
                PackMethod.Zstandard => 3,
                PackMethod.Block0 => 4,
                _ => throw new Exception("Internal pack error"),
            };
            writer.Write((byte)type);
            writer.WriteStringUTF8(item.Name);
            if (type != 1)
            {
                writer.WriteVarUInt64((ulong)item.Offset);
                writer.WriteVarUInt64((ulong)item.Length);
            }
        }
    }

    void CompressSt(PackItem[] compTasks, Stream outputStream)
    {
        var compressor = new Compressor(22);
        double lastpct = -1.0;
        int i = 0;
        foreach (var comp in compTasks) {
            comp.Offset = outputStream.Position;
            using var src = File.OpenRead(comp.SourcePath);
            using (var dst = new CompressionStream(outputStream, compressor))
            {
                src.CopyTo(dst);
            }
            comp.Length = outputStream.Position - comp.Offset;
            i++;
            var pct = ((double)i / compTasks.Length) * 100.0;
            if(Verbose)
                Log?.Invoke($"Compressed: {comp.FullPath} ({i}/{compTasks.Length}) ({pct:F2}%)");
            else if ((pct - lastpct) >= 0.1)
            {
                Log?.Invoke($"{pct:F2}%");
                lastpct = pct;
            }
        }
    }

    unsafe void CompressMt(PackItem[] compTasks, Stream outputStream, int threads)
    {
        BlockingCollection<(string FullPath, PackItem Item, UnmanagedWriteStream Data)> compWriteItems = new();
        var compWriteTask = Task.Run(async () =>
        {
            int i = 0;
            double lastpct = -1.0;
            foreach(var comp in compWriteItems.GetConsumingEnumerable())
            {
                i++;
                var pct = ((double)i / compTasks.Length) * 100.0;
                if(Verbose)
                    Log?.Invoke($"Compressed: {comp.FullPath} ({i}/{compTasks.Length}) ({pct:F2}%)");
                else if ((pct - lastpct) >= 0.1)
                {
                    Log?.Invoke($"{pct:F2}%");
                    lastpct = pct;
                }

                comp.Item.Offset = outputStream.Position;
                comp.Item.Length = comp.Data.Length;
                comp.Data.WriteAndDispose(outputStream);
            }
        });

        //Bepu's ThreadDispatcher is faster than Parallel.ForEach by a
        //matter of minutes on large data sets.
        using var dispatch = new ThreadDispatcher(threads);
        int packItemIndex = 0;

        void Worker(int workerIndex)
        {
            using var compressor = new Compressor(22);
            int jobIndex;
            while ((jobIndex = Interlocked.Increment(ref packItemIndex)) < compTasks.Length)
            {
                var toComp = compTasks[jobIndex];
                using var src = File.OpenRead(toComp.SourcePath);
                var mem = new UnmanagedWriteStream();
                using (var comp = new CompressionStream(mem, compressor))
                    src.CopyTo(comp);
                compWriteItems.Add((toComp.FullPath, toComp, mem));
            }
        }

        dispatch.DispatchWorkers((Action<int>)Worker, compTasks.Length);
        compWriteItems.CompleteAdding();
        compWriteTask.Wait();
    }


    public void Pack(string outputFile)
    {
        Log?.Invoke($"Creating {outputFile}");
        using var outputStream = File.Create(outputFile);
        var outputWriter = new BinaryWriter(outputStream);

        outputWriter.Write((byte)'\b');
        outputWriter.Write((byte)'L');
        outputWriter.Write((byte)'P');
        outputWriter.Write((byte)0);
        outputWriter.Write((long)0); //Offset to metadata

        Log?.Invoke("Compressing files...");
        // Use threads for ZSTD, level 22 compression is very slow
        var compTasks = IteratePack(sourceRoot).Where(x => x.Method == PackMethod.Zstandard).ToArray();


        int threads = MaxThreads <= 0 ? Environment.ProcessorCount : MaxThreads;
        if (threads == 1)
        {
            CompressSt(compTasks, outputStream);
        }
        else
        {
            CompressMt(compTasks, outputStream, threads);
        }
        //
        Log?.Invoke("Adding uncompressed files");
        // Copying is fast
        foreach(var toCopy in IteratePack(sourceRoot).Where(x => x.Method == PackMethod.Uncompressed))
        {
            if(Verbose)
                Log?.Invoke($"Copying {toCopy.FullPath}");
            using var src = File.OpenRead(toCopy.SourcePath);
            toCopy.Offset = outputStream.Position;
            toCopy.Length = src.Length;
            src.CopyTo(outputStream);
        }

        var hasBlock0 = WriteBlock(PackMethod.Block0, outputStream, out var block0Offset, out var block0Length);

        var fullLength = outputStream.Position;
        outputStream.Seek(4, SeekOrigin.Begin);
        outputWriter.Write(fullLength);
        outputStream.Seek(fullLength, SeekOrigin.Begin);
        if (hasBlock0) {
            outputWriter.Write((byte)1);
            outputWriter.WriteVarUInt64((ulong)block0Offset);
            outputWriter.WriteVarUInt64((ulong)block0Length);
        }
        else {
            outputWriter.Write((byte)0);
        }
        WriteTree(outputWriter, sourceRoot);

        var metadataSize = outputStream.Position - fullLength;
        if(Verbose)
            Log?.Invoke($"Metadata Size: {DebugDrawing.SizeSuffix(metadataSize)}");
    }
}
