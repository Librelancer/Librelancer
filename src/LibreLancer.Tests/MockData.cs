using System;
using System.IO;
using LibreLancer.Data;
using LibreLancer.Data.IO;
using LibreLancer.Physics;
using LibreLancer.Resources;
using LibreLancer.Sur;

namespace LibreLancer.Tests;

public class MockData
{
    public GameDataManager GameData;
    public FileSystem VFS;
    public ResourceManager Resources;

    public MockData()
    {
        VFS = new FileSystem(new EmptyFS(), new ResourceFS());
        var convex = new ConvexShapeCollection(x =>
        {
            using var f = VFS.Open(x);
            return SurFile.Read(f).Convert();
        });
        Resources = new ServerResourceManager(convex, VFS);
        GameData = new GameDataManager(new GameItemDb(VFS), Resources);
    }

    sealed class ResourceFS : BaseFileSystemProvider
    {
        private const string Prefix = "LibreLancer.Tests.TestAssets.";
        public ResourceFS() => Refresh();

        class ResourceFile : VfsFile
        {
            public string ResName;
            public override Stream OpenRead() => typeof(ResourceFS).Assembly.GetManifestResourceStream(ResName)!;
        }

        public override void Refresh()
        {
            Root = new VfsDirectory();
            foreach (var fs in typeof(ResourceFS).Assembly.GetManifestResourceNames())
            {
                if (!fs.StartsWith(Prefix))
                    continue;
                var n = fs.Substring(Prefix.Length);
                Root.Items[n] = new ResourceFile() { Name = n, ResName = fs };
            }
        }
    }
    // FS containing empty freelancer.ini
    // Satisfy GameDataManager constructor
    sealed class EmptyFS : BaseFileSystemProvider
    {
        public EmptyFS() => Refresh();
        class EmptyFile : VfsFile
        {
            public override Stream OpenRead() => new MemoryStream();
        }
        public override void Refresh()
        {
            Root = new VfsDirectory();
            var exe = new VfsDirectory() { Name = "EXE", Parent = Root };
            Root.Items["EXE"] = exe;
            exe.Items.Add("freelancer.ini", new EmptyFile() { Name = "freelancer.ini" });
        }
    }


    public void HashAndAdd<T>(T item, GameItemCollection<T> collection) where T : IdentifiableItem
    {
        item.CRC = FLHash.CreateID(item.Nickname);
        collection.Add(item);
    }
}
