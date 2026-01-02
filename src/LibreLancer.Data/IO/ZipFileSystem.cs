using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace LibreLancer.Data.IO;

public sealed class ZipFileSystem : BaseFileSystemProvider
{
    private const uint ZIP_LOCAL_FILE_SIG = 0x04034b50;
    private const uint ZIP_CENTRAL_DIR_SIG = 0x02014b50;
    private const uint ZIP_END_OF_CENTRAL_DIR_SIG = 0x06054b50;
    private const uint ZIP64_END_OF_CENTRAL_DIR_SIG = 0x06064b50;
    private const uint ZIP64_EXTENDED_INFO_EXTRA_FIELD_SIG = 0x0001;

    private Func<Stream> openStream;

    private (long dataStart, long dirOffset, long entryCount) ParseZipEOCD(Stream stream)
    {
        if (!FindEndOfCentralDir(stream))
            throw new ArgumentException("Not a valid .zip file");
        var pos = stream.Position;
        var reader = new BinaryReader(stream);
        if (reader.ReadUInt32() != ZIP_END_OF_CENTRAL_DIR_SIG)
            throw new ArgumentException("Not a valid .zip file");
        //Do Zip64 stuff here

        //Read central dir Zip32
        var eocd = reader.ReadStruct<Zip32EndOfCentralDirectory>();
        var entryCount = eocd.CentralDirectoryTotal;
        var dataStart = pos - (eocd.CentralDirectoryOffset + eocd.CentralDirectorySize);
        var centralDirPos = dataStart + eocd.CentralDirectoryOffset;

        return (dataStart, centralDirPos, entryCount);
    }

    private class ZipVfsFile : VfsFile
    {
        private Func<Stream> getFileStream;
        private long dataOffset = -1;
        private long dataSize = -1;
        private bool decompress;

        private long entryOffset;

        private object checkDataLock = new();

        public ZipVfsFile(string name, Func<Stream> getFileStream, long entryOffset)
        {
            Name = name;
            this.getFileStream = getFileStream;
            this.entryOffset = entryOffset;
        }

        private Stream GetStream()
        {
            lock (checkDataLock)
            {
                if (dataOffset != -1)
                    return getFileStream();
                else
                {
                    var stream = getFileStream();
                    stream.Seek(entryOffset, SeekOrigin.Begin);
                    var reader = new BinaryReader(stream);
                    var header = reader.ReadStruct<ZipLocalFileHeader>();
                    dataOffset = entryOffset + 30 + header.FileNameLength + header.ExtraFieldLength;
                    if (header.Compression != 0 && header.Compression != 8)
                        throw new NotSupportedException($"Unsupported compression {header.Compression}");
                    if (header.Compression == 8) {
                        decompress = true;
                    }
                    else {
                        dataSize = Math.Max(header.UncompressedSize, header.CompressedSize);
                    }
                    return stream;
                }
            }


        }

        private Stream Decompress(Stream baseStream)
        {
            baseStream.Seek(dataOffset, SeekOrigin.Begin);

            using var deflate = new DeflateStream(baseStream, CompressionMode.Decompress);

            var ms = new MemoryStream();
            deflate.CopyTo(ms);
            ms.Position = 0;
            return ms;
        }

        public override Stream OpenRead()
        {
            var baseStream = GetStream();
            if (decompress)
            {
                return Decompress(baseStream);
            }
            else
            {
                return new SlicedStream(dataOffset, dataSize, baseStream);
            }
        }
    }
    public override void Refresh()
    {
        using var stream = openStream();

        var (dataStart, dirOffset, entryCount) = ParseZipEOCD(stream);

        stream.Seek(dirOffset, SeekOrigin.Begin);
        var reader = new BinaryReader(stream);
        List<string[]> directories = [];
        List<(string Name, long Offset)> files = [];
        for (var i = 0; i < entryCount; i++)
        {
            var e = reader.ReadStruct<ZipCentralDirectory>();
            if (e.Signature != ZIP_CENTRAL_DIR_SIG)
                throw new Exception("Corrupt entry");
            var filename = Encoding.UTF8.GetString(reader.ReadBytes(e.FileNameLength)).Replace("\\", "/");
            var nextEntry = stream.Position + e.CommentLength + e.ExtraFieldLength;
            if (filename.EndsWith("/")) {
                directories.Add(filename.Split('/', StringSplitOptions.RemoveEmptyEntries));
            }
            else {
               files.Add((filename, dataStart + e.LocalHeaderOffset));
            }
            reader.BaseStream.Seek(nextEntry, SeekOrigin.Begin);
        }
        Root = new VfsDirectory();
        directories.Sort((x, y) => x.Length.CompareTo(y.Length));
        foreach (var dir in directories)
        {
            var current = Root;
            for (int i = 0; i < dir.Length - 1; i++)
            {
                current = (VfsDirectory)current.Items[dir[i]];
            }

            current.Items.Add(dir[^1], new VfsDirectory() { Name = dir[^1], Parent = current });
        }

        foreach (var file in files)
        {
            var dir = (VfsDirectory)GetItem(Path.GetDirectoryName(file.Name)!)!;
            var filename = Path.GetFileName(file.Name);
            dir.Items[filename] = new ZipVfsFile(filename, openStream, file.Offset);
        }
    }

    public ZipFileSystem(Func<Stream> openStream)
    {
        this.openStream = openStream;
        Refresh();
    }
    public ZipFileSystem(string filename)
    {
        openStream = () => File.OpenRead(filename);
        Refresh();
    }

    private static bool FindEndOfCentralDir(Stream stream, bool seek = true)
    {
        Span<byte> buffer = stackalloc byte[256];
        Span<byte> extra = stackalloc byte[4];
        var length = stream.Length;
        long pos;
        int maxread;
        if (buffer.Length < length)
        {
            pos = length - buffer.Length;
            maxread = buffer.Length;
        }
        else
        {
            pos = 0;
            maxread = (int)length;
        }
        long totalread = 0;
        while ((totalread < length) && (totalread < 65557))
        {
            stream.Seek(pos, SeekOrigin.Begin);
            if (totalread != 0)
            {
                if (stream.Read(buffer.Slice(0, maxread - 4)) == 0)
                    return false;
                extra.CopyTo(buffer.Slice(maxread - 4, 4));
                totalread += maxread - 4;
            }
            else
            {
                if (stream.Read(buffer) == 0)
                    return false;
                totalread += maxread;
            }
            buffer.Slice(0, 4).CopyTo(extra);
            int foundOffset = -1;
            for (int i = maxread - 4; i > 0; i--)
            {
                if (buffer[i + 0] == 0x50 &&
                    buffer[i + 1] == 0x4B &&
                    buffer[i + 2] == 0x05 &&
                    buffer[i + 3] == 0x06)
                {
                    foundOffset = i;
                    break;
                }
            }
            if (foundOffset != -1) {
                if (seek)
                {
                    stream.Seek(pos + foundOffset, SeekOrigin.Begin);
                }
                return true;
            }

            if (pos == 0)
                return false;
            pos -= (maxread - 4);
            if (pos < 0)
                pos = 0;
        }
        return false;
    }

    public static bool IsZip(Stream stream)
    {
        var pos = stream.Position;
        try
        {
            var reader = new BinaryReader(stream);
            if (reader.ReadUInt32() == ZIP_LOCAL_FILE_SIG)
                return true;
            return FindEndOfCentralDir(stream, false);
        }
        finally
        {
            stream.Seek(pos, SeekOrigin.Begin);
        }
    }
}
