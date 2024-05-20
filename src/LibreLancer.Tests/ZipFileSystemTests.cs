using System.IO;
using LibreLancer.Data.IO;
using Xunit;

namespace LibreLancer.Tests;

public class ZipFileSystemTests
{
    [Fact]
    public static void CanDetectZipStream()
    {
        using var normal = TestAsset.Open<ZipFileSystemTests>("plainzip.zip");
        using var withextra = TestAsset.Open<ZipFileSystemTests>("zipwithextra.dat");
        Assert.True(ZipFileSystem.IsZip(normal));
        Assert.True(ZipFileSystem.IsZip(withextra));
    }

    [Fact]
    public static void CanReadWithExtra()
    {
        var withextra = new ZipFileSystem(() => TestAsset.Open<ZipFileSystemTests>("zipwithextra.dat"));
        AssertFileContents("abc.txt", "12345", withextra);
    }

    static void AssertFileContents(string file, string contents, ZipFileSystem fs)
    {
        using var reader = new StreamReader(fs.Open(file));
        var fileContent = reader.ReadToEnd().Trim();
        Assert.Equal(contents, fileContent);
    }
    [Fact]
    public static void CanReadFile()
    {
        var fs = new ZipFileSystem(() => TestAsset.Open<ZipFileSystemTests>("plainzip.zip"));
        AssertFileContents("abc.txt", "12345", fs);
        AssertFileContents("def/file2.txt", "abcdefg", fs);
    }

    [Fact]
    public static void CanListDirectory()
    {
        var fs = new ZipFileSystem(() => TestAsset.Open<ZipFileSystemTests>("plainzip.zip"));
        Assert.Single(fs.GetDirectories("/"));
        Assert.Single(fs.GetFiles("/"));
    }

    [Fact]
    public static void CanFoldCase()
    {
        var fs = new ZipFileSystem(() => TestAsset.Open<ZipFileSystemTests>("plainzip.zip"));
        AssertFileContents("DEF/NESTED/fIlE3.txt", "hello", fs);
    }
    [Fact]
    public static void CanTraverse()
    {
        var fs = new ZipFileSystem(() => TestAsset.Open<ZipFileSystemTests>("plainzip.zip"));
        AssertFileContents("def/../abc.txt", "12345", fs);
    }

    [Fact]
    public static void CanHandleWeirdPath()
    {
        var fs = new ZipFileSystem(() => TestAsset.Open<ZipFileSystemTests>("plainzip.zip"));
        AssertFileContents("/def\\.././\\abc.txt", "12345", fs);
    }
}
