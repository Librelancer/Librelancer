using System;
using System.Numerics;
using LibreLancer.Net;
using LibreLancer.Net.Protocol;
using LibreLancer.Server;
using LibreLancer.Server.Components;
using LibreLancer.World;
using LiteNetLib.Utils;
using Xunit;

namespace LibreLancer.Tests;

public class NetPackingTests
{
    [Fact]
    public void DeltaBits()
    {
        var baseValue = 2000U;
        for (int i = -64; i <= 63; i++)
        {
            var newValue = (uint) (baseValue + i);
            Assert.True(NetPacking.TryDelta(newValue, baseValue, 7, out var d));
            Assert.Equal(newValue, NetPacking.ApplyDelta(d, baseValue, 7));
        }
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    public void WriteBits(int bits)
    {
        for(uint i = 0; i < (2 << (bits - 1)); i++)
        {
            var w = new BitWriter();
            w.PutUInt(1,2);
            w.PutUInt(i, bits);
            w.PutUInt(i, bits);
            w.PutUInt(i, bits);
            var r = new BitReader(w.GetCopy(), 0);
            Assert.Equal(1U, r.GetUInt(2));
            Assert.Equal(i, r.GetUInt(bits));
            Assert.Equal(i, r.GetUInt(bits));
            Assert.Equal(i, r.GetUInt(bits));
        }
    }

    [Theory]
    [InlineData(0xFFFFFFFF)]
    [InlineData(0x7FFFFFF)]
    [InlineData(0x7FFF)]
    [InlineData(0x7FF)]
    [InlineData(0x3F)]
    public void WriteUInt(uint v)
    {
        var w = new BitWriter();
        w.PutUInt(v, 32);
        var r = new BitReader(w.GetCopy(), 0);
        Assert.Equal(v, r.GetUInt());
    }

    [Theory]
    [InlineData(0xFFFFFFFF, 5)]
    [InlineData(541097984, 5)]
    [InlineData(541097983, 4)]
    [InlineData(4227072, 4)]
    [InlineData(4227071, 3)]
    [InlineData(32768, 3)]
    [InlineData(32767, 2)]
    public void BigVarUInt32(uint v, int expectedCount)
    {
        var pw = new PacketWriter(new NetDataWriter());
        pw.PutBigVarUInt32(v);
        var bytes = pw.GetCopy();
        Assert.Equal(expectedCount, bytes.Length);
        var pr = new PacketReader(new NetDataReader(bytes));
        Assert.Equal(v, pr.GetBigVarUInt32());
    }

    [Theory]
    [InlineData(0xFFFFFFFF, 5)]
    [InlineData(270549119, 5)]
    [InlineData(270549118, 4)]
    [InlineData(2113663, 4)]
    [InlineData(2113662, 3)]
    [InlineData(16512, 3)]
    [InlineData(16511, 2)]
    [InlineData(128, 2)]
    [InlineData(127, 1)]
    public void VariableUInt32(uint v, int expectedCount)
    {
        var pw = new PacketWriter(new NetDataWriter());
        pw.PutVariableUInt32(v);
        var bytes = pw.GetCopy();
        Assert.Equal(expectedCount, bytes.Length);
        var pr = new PacketReader(new NetDataReader(bytes));
        Assert.Equal(v, pr.GetVariableUInt32());
    }

    [InlineData(600)]
    [InlineData(1100)]
    [Theory]
    public void PacketVariableInt32(int v)
    {
        var pw = new PacketWriter(new NetDataWriter());
        pw.PutVariableInt32(v);
        var bytes = pw.GetCopy();
        var pr = new PacketReader(new NetDataReader(bytes));
        Assert.Equal(v, pr.GetVariableInt32());
    }

    [InlineData(600)]
    [InlineData(1100)]
    [Theory]
    public void BitWriterVariableInt32(int v)
    {
        var pw = new BitWriter();
        pw.PutVarInt32(v);
        var bytes = pw.GetCopy();
        var pr = new BitReader(bytes, 0);
        Assert.Equal(v, pr.GetVarInt32());
    }



    [Fact]
    public void Alignment()
    {
        var w = new BitWriter();
        w.PutBool(true);
        w.Align();
        w.PutByte(0x33);
        w.Align();
        var result = w.GetCopy();
        Assert.Equal(2, result.Length);
        var r = new BitReader(result, 0);
        Assert.Equal(true, r.GetBool());
        r.Align();
        Assert.Equal(0x33, r.GetByte());
        r.Align();
    }

    [Theory]
    [InlineData("abcdefg")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("123")]
    [InlineData("li01_01_base")]
    [InlineData("The quick brown fox jumps over the lazy dog.")]
    [InlineData(@"案ずるより産むが易し。 - Giving birth to a baby is easier than worrying about it.
出る杭は打たれる。 - The stake that sticks up gets hammered down.
知らぬが仏。 - Not knowing is Buddha. - Ignorance is bliss.
見ぬが花。 - Not seeing is a flower. - Reality can't compete with imagination.
花は桜木人は武士 - Of flowers, the cherry blossom; of men, the warrior.")]
    public void Strings(string s)
    {
        var pw = new PacketWriter();
        pw.Put(s);
        var pr = new PacketReader(new NetDataReader(pw.GetCopy()));
        Assert.Equal(s, pr.GetString());
    }

    [Theory]
    [InlineData("abcdefg")]
    [InlineData(null)]
    [InlineData("123")]
    [InlineData("")]
    [InlineData("The quick brown fox jumps over the lazy dog.")]
    public void TryGetStrings(string? s)
    {
        var pw = new PacketWriter();
        pw.Put(s);
        var pr = new PacketReader(new NetDataReader(pw.GetCopy()));
        Assert.True(pr.TryGetString(out var s2));
        Assert.Equal(s, s2);
    }

    [Fact]
    public void TryGetStringEmpty()
    {
        var pw = new PacketWriter();
        var pr = new PacketReader(new NetDataReader(pw.GetCopy()));
        Assert.False(pr.TryGetString(out _));
    }

    [Fact]
    public void TryGetStringInvalid()
    {
        var pw = new PacketWriter();
        pw.Put((byte)0x45);
        var pr = new PacketReader(new NetDataReader(pw.GetCopy()));
        Assert.False(pr.TryGetString(out _));
    }

    static ObjectUpdate GetUpdate()
    {
        var srcUpdate = new ObjectUpdate();
        srcUpdate.Position = new(new Vector3(-33000, 0, -28000));
        srcUpdate.Shield = 821;
        srcUpdate.Orientation = Quaternion.Identity;
        srcUpdate.Hull = 1300;
        srcUpdate.CruiseThrust = CruiseThrustState.None;
        srcUpdate.ID = new ObjNetId(-1);
        srcUpdate.EngineKill = false;
        srcUpdate.Guns =
        [
            new GunOrient() { AnglePitch = 0, AngleRot = 0 },
            new GunOrient() { AnglePitch = 0, AngleRot = 0 }
        ];
        return srcUpdate;
    }

    static ObjectUpdate GetUpdate2()
    {
        var srcUpdate = new ObjectUpdate();
        srcUpdate.Position = new(new Vector3(-445, 7, -300000));
        srcUpdate.Shield = 7;
        srcUpdate.Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, 2);
        srcUpdate.Hull = 90;
        srcUpdate.CruiseThrust = CruiseThrustState.None;
        srcUpdate.ID = new ObjNetId(-1);
        srcUpdate.EngineKill = true;
        srcUpdate.Guns =
        [
            new GunOrient() { AnglePitch = 0.5f, AngleRot = -0.2f },
            new GunOrient() { AnglePitch = 2f, AngleRot = -0.1f }
        ];
        return srcUpdate;
    }

    static ObjectUpdate GetUpdate3()
    {
        var srcUpdate = new ObjectUpdate();
        srcUpdate.Position = new(new Vector3(-33000, 0, -28000));
        srcUpdate.Shield = 600;
        srcUpdate.Orientation = new() { Largest = 3, Component1 = 566, Component2 = 589, Component3 = 492 };
        srcUpdate.Hull = 1100;
        srcUpdate.CruiseThrust = CruiseThrustState.None;
        srcUpdate.ID = new ObjNetId(-1);
        srcUpdate.EngineKill = false;
        srcUpdate.Guns =
        [
            new GunOrient() { AnglePitch = 0, AngleRot = 0 },
            new GunOrient() { AnglePitch = 0, AngleRot = 0 }
        ];
        return srcUpdate;
    }

    [Fact]
    public void UpdateWithNonZeroBuffer()
    {
        byte[] srcArray = "hff&YHudjewnjlaufjelkfeoiayf7ouiwu570983274ifjalhdfy8e9oiadw"u8.ToArray();

        var srcUpdate = GetUpdate();
        var bw = new NetRleWriter(srcArray);
        srcUpdate.WriteDelta(ObjectUpdate.Blank, 35, 36,  bw);

        var br = new NetRleReader(bw.GetCopy(), 0);
        var dstUpdate = ObjectUpdate.ReadDelta(br, 36, -1, (_, _) => ObjectUpdate.Blank);

        // These should all be the same
        Assert.Equal(srcUpdate.ID, dstUpdate.ID);
        Assert.Equal(srcUpdate.EngineKill, dstUpdate.EngineKill);
        Assert.Equal(srcUpdate.Hull, dstUpdate.Hull);
        Assert.Equal(srcUpdate.Shield, dstUpdate.Shield);
        Assert.Equal(srcUpdate.Guns.Length, dstUpdate.Guns.Length);
        Assert.Equal(srcUpdate.AngularVelocity, dstUpdate.AngularVelocity);
        Assert.Equal(srcUpdate.LinearVelocity, dstUpdate.LinearVelocity);
    }

    [Fact]
    public void TestDeltaUpdate()
    {
        var srcUpdate = GetUpdate2();
        var bw = new NetRleWriter();
        srcUpdate.WriteDelta(GetUpdate(), 35, 36, bw);

        var br = new NetRleReader(bw.GetCopy(), 0);
        var dstUpdate = ObjectUpdate.ReadDelta(br, 36, -1, (_, _) => GetUpdate());
        // These should all be the same
        Assert.Equal(srcUpdate.ID, dstUpdate.ID);
        Assert.Equal(srcUpdate.EngineKill, dstUpdate.EngineKill);
        Assert.Equal(srcUpdate.Hull, dstUpdate.Hull);
        Assert.Equal(srcUpdate.Shield, dstUpdate.Shield);
        Assert.Equal(srcUpdate.Guns.Length, dstUpdate.Guns.Length);
        Assert.Equal(srcUpdate.AngularVelocity, dstUpdate.AngularVelocity);
        Assert.Equal(srcUpdate.LinearVelocity, dstUpdate.LinearVelocity);
        Assert.Equal(srcUpdate.Position, dstUpdate.Position);
        Assert.Equal(srcUpdate.Orientation, dstUpdate.Orientation);
    }


    [Fact]
    public void TestNoDeltaUpdate()
    {
        var srcUpdate = GetUpdate();
        var bw = new NetRleWriter();
        srcUpdate.WriteDelta(ObjectUpdate.Blank, 35, 36, bw);

        var br = new NetRleReader(bw.GetCopy(), 0);
        var dstUpdate = ObjectUpdate.ReadDelta(br, 36, -1, (_, _) => ObjectUpdate.Blank);

        // These should all be the same
        Assert.Equal(srcUpdate.ID, dstUpdate.ID);
        Assert.Equal(srcUpdate.EngineKill, dstUpdate.EngineKill);
        Assert.Equal(srcUpdate.Hull, dstUpdate.Hull);
        Assert.Equal(srcUpdate.Shield, dstUpdate.Shield);
        Assert.Equal(srcUpdate.Guns.Length, dstUpdate.Guns.Length);
        Assert.Equal(srcUpdate.AngularVelocity, dstUpdate.AngularVelocity);
        Assert.Equal(srcUpdate.LinearVelocity, dstUpdate.LinearVelocity);
        Assert.Equal(srcUpdate.Position, dstUpdate.Position);
        Assert.Equal(srcUpdate.Orientation, dstUpdate.Orientation);
    }

    [Fact]
    public void TestIdenticalUpdate()
    {
        var srcUpdate = GetUpdate();
        var bw = new NetRleWriter();
        srcUpdate.WriteDelta(srcUpdate, 35, 36, bw);

        var br = new NetRleReader(bw.GetCopy(), 0);
        var dstUpdate = ObjectUpdate.ReadDelta(br, 36, -1, (_, _) => srcUpdate);

        // These should all be the same (no floating point error)
        Assert.Equal(srcUpdate.ID, dstUpdate.ID);
        Assert.Equal(srcUpdate.EngineKill, dstUpdate.EngineKill);
        Assert.Equal(srcUpdate.Hull, dstUpdate.Hull);
        Assert.Equal(srcUpdate.Shield, dstUpdate.Shield);
        Assert.Equal(srcUpdate.Guns.Length, dstUpdate.Guns.Length);
        Assert.Equal(srcUpdate.AngularVelocity, dstUpdate.AngularVelocity);
        Assert.Equal(srcUpdate.LinearVelocity, dstUpdate.LinearVelocity);
        Assert.Equal(srcUpdate.Position, dstUpdate.Position);
        Assert.Equal(srcUpdate.Orientation, dstUpdate.Orientation);
    }


    [Fact]
    public void TestUpdatePacket()
    {
        var up = new UpdatePacker();
        var obj = new GameObject();
        var pobj = new GameObject();
        var plr = new Player(null!, null!, Guid.Empty);
        var spc = new SPlayerComponent(plr, pobj);

        var auth = new PlayerAuthState();
        auth.Shield = 5;
        auth.Orientation = Quaternion.Identity;
        auth.Position = new(0, 5, 0);
        auth.LinearVelocity = new(0, 0, 5);
        auth.AngularVelocity = new(5, 0, 0);
        auth.Health = 7;

        var srcUpdate = GetUpdate3();

        var pk = up.Begin([srcUpdate], [obj]);


        var packet = pk.Pack(12, auth, spc, pobj, 500);

        var ndw = new NetDataWriter();
        var packetWrite = new PacketWriter(ndw);
        packet.WriteContents(packetWrite);

        var reader = new PacketReader(new NetDataReader(ndw.CopyData()));
        var readPacket = (PackedUpdatePacket)PackedUpdatePacket.Read(reader);

        var (_, upd) = readPacket.GetUpdates(default, (_,_) => ObjectUpdate.Blank);

        Assert.Single(upd);

        var dstUpdate = upd[0];

        Assert.Equal(srcUpdate.ID, dstUpdate.ID);
        Assert.Equal(srcUpdate.EngineKill, dstUpdate.EngineKill);
        Assert.Equal(srcUpdate.Hull, dstUpdate.Hull);
        Assert.Equal(srcUpdate.Shield, dstUpdate.Shield);
        Assert.Equal(srcUpdate.Guns.Length, dstUpdate.Guns.Length);
        Assert.Equal(srcUpdate.AngularVelocity, dstUpdate.AngularVelocity);
        Assert.Equal(srcUpdate.LinearVelocity, dstUpdate.LinearVelocity);
        Assert.Equal(srcUpdate.Position, dstUpdate.Position);
        Assert.Equal(srcUpdate.Orientation, dstUpdate.Orientation);
    }
}
