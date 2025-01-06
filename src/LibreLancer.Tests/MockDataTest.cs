using System;
using System.IO;
using LibreLancer.Data.IO;
using LibreLancer.GameData.Items;
using LibreLancer.Physics;
using Xunit;

namespace LibreLancer.Tests;


public class MockDataTest
{
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
    static void HashAndAdd<T>(T item, GameItemCollection<T> collection) where T : IdentifiableItem
    {
        item.CRC = FLHash.CreateID(item.Nickname);
        collection.Add(item);
    }

    static GameDataManager ConstructMockData()
    {
        // Set up backing
        // Throw error on any .sur or file access
        var convex = new ConvexMeshCollection(x => throw new InvalidOperationException("Tried to open sur"));
        var fs = new FileSystem(new EmptyFS());
        var gdm = new GameDataManager(fs, new ServerResourceManager(convex, fs));
        // Construct game data without calling LoadData()
        // Will fail on model loads etc., use for nickname/CRC lookups only
        // Each collection used must be inited manually
        gdm.Equipment = new GameItemCollection<Equipment>();
        HashAndAdd(new GunEquipment() { Nickname = "gun01"}, gdm.Equipment);

        return gdm;
    }


    [Fact]
    public void CanQueryEquipment()
    {
        var gdm = ConstructMockData();
        Assert.Equal("gun01", gdm.Equipment.Get("gun01").Nickname);
    }
}
