using System.IO;
using System.Text;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Save;
using LibreLancer.Net;
using LibreLancer.Net.Protocol;
using LibreLancer.Server;
using LiteNetLib.Utils;
using Xunit;

namespace LibreLancer.Tests;

public class DestroyedPartPersistenceTests
{
    [Fact]
    public void SavePlayerReadsUnsignedDestroyedPartCrc()
    {
        const uint highCrc = 0xF1234567;
        var save = SaveGame.FromString("destroyed-parts.fl", $"""
            [Player]
            rank = 1
            destroyed_part = 7
            destroyed_part = {highCrc}
            destroyed_part = {unchecked((int)highCrc)}
            """);

        Assert.Equal([7u, highCrc, highCrc], save.Player!.DestroyedParts);
    }

    [Fact]
    public void DestroyedPartsRoundTripThroughSaveWriterFormat()
    {
        const uint highCrc = 0xF1234567;
        var save = new SaveGame
        {
            Player = new SavePlayer
            {
                Rank = 1,
                DestroyedParts = [7u, highCrc]
            }
        };

        using var stream = new MemoryStream();
        IniWriter.WriteIni(stream, save.ToIni());
        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.GetEncoding(1252));
        var loaded = SaveGame.FromString("destroyed-parts.fl", reader.ReadToEnd());

        Assert.Equal([7u, highCrc], loaded.Player!.DestroyedParts);
    }

    [Fact]
    public void CharacterDestroyedPartsAreUniqueAndStable()
    {
        const uint highCrc = 0xF1234567;
        var character = new NetCharacter();

        character.SetDestroyedParts([highCrc, 7, highCrc]);
        character.MarkPartDestroyed(3);
        character.MarkPartDestroyed(7);

        Assert.Equal([3u, 7u, highCrc], character.GetDestroyedParts());
    }

    [Fact]
    public void NewHullInventoryUpdateResetsDestroyedParts()
    {
        var diff = PlayerInventoryDiff.Create(
            new PlayerInventory(),
            new PlayerInventory(),
            resetDestroyedParts: true);
        var data = new NetDataWriter();
        diff.Put(new PacketWriter(data));
        var result = PlayerInventoryDiff.Read(new PacketReader(new NetDataReader(data.CopyData())));

        Assert.True(result.ResetDestroyedParts);
    }
}
