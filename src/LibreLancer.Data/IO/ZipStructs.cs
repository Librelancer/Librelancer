using System.Runtime.InteropServices;

namespace LibreLancer.Data.IO;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct Zip32EndOfCentralDirectory
{
    //Skip signature
    public ushort DiskNumber;
    public ushort CentralDirectoryDiskNumber;
    public ushort CentralDirectoryCount;
    public ushort CentralDirectoryTotal;
    public uint CentralDirectorySize;
    public uint CentralDirectoryOffset;
    public uint CommentLength;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct ZipCentralDirectory
{
    public uint Signature;
    public ushort Version;
    public ushort VersionNeeded;
    public ushort GeneralBits;
    public ushort CompressionMethod;
    public uint ModTime;
    public uint DataCrc;
    public uint CompressedSize;
    public uint UncompressedSize;
    public ushort FileNameLength;
    public ushort ExtraFieldLength;
    public ushort CommentLength;
    public ushort DiskNumber;
    public ushort InternalFileAttributes;
    public uint ExtFileAttributes;
    public uint LocalHeaderOffset;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct ZipLocalFileHeader
{
    public uint Signature;
    public ushort VersionNeeded;
    public ushort GeneralBits;
    public ushort Compression;
    public uint ModTime;
    public uint DataCrc;
    public uint CompressedSize;
    public uint UncompressedSize;
    public ushort FileNameLength;
    public ushort ExtraFieldLength;
}
