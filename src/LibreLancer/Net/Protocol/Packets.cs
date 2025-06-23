﻿// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using LibreLancer.GameData.World;
using LibreLancer.Net.Protocol.RpcPackets;
using LibreLancer.World;
using LibreLancer.World.Components;
using Microsoft.EntityFrameworkCore.Internal;
using SharpDX.MediaFoundation;
using Quaternion = System.Numerics.Quaternion;

namespace LibreLancer.Net.Protocol
{
    public interface IPacket
    {
        void WriteContents(PacketWriter outPacket);
    }

    public static class Packets
    {
        static List<Func<PacketReader, object>> parsers = new List<Func<PacketReader,object>>();
        static List<Type> packetTypes = new List<Type>();
        public static void Register<T>(Func<PacketReader,object> parser) where T : IPacket
        {
            packetTypes.Add(typeof(T));
            parsers.Add(parser);
        }

        public static void Write(PacketWriter outPacket, IPacket p)
        {
            var pkt = packetTypes.IndexOf(p.GetType());
            if(pkt == -1) throw new Exception($"Packet type not registered {p.GetType().Name}");
            if (pkt > 254)
            {
                outPacket.Put((byte)255);
                outPacket.PutVariableUInt32((uint)(pkt - 255));
            }
            else
            {
                outPacket.Put((byte)pkt);
            }
            p.WriteContents(outPacket);
        }

        public static IPacket Read(PacketReader inPacket)
        {
            var b1 = inPacket.GetByte();
            uint pkt = b1;
            if (b1 == 255)
            {
                pkt += inPacket.GetVariableUInt32();
            }
            return (IPacket)parsers[(int)pkt](inPacket);
        }

#if DEBUG
        public static void CheckRegistered(IPacket p)
        {
            if (p is SPUpdatePacket) return;
            var idx = packetTypes.IndexOf(p.GetType());
            if(idx == -1) throw new Exception($"Packet type not registered {p.GetType().Name}");
        }
#endif
        static Packets()
        {
            //Authentication
            Register<GuidAuthenticationPacket>(GuidAuthenticationPacket.Read);
            Register<AuthenticationReplyPacket>(AuthenticationReplyPacket.Read);
            Register<LoginSuccessPacket>(LoginSuccessPacket.Read);
            //Menu
            Register<OpenCharacterListPacket>(OpenCharacterListPacket.Read);
            Register<NewCharacterDBPacket>(NewCharacterDBPacket.Read);
            Register<AddCharacterPacket>(AddCharacterPacket.Read);
            //Space
            Register<InputUpdatePacket>(InputUpdatePacket.Read);
            Register<PackedUpdatePacket>(PackedUpdatePacket.Read);
            //Protocol
            GeneratedProtocol.RegisterPackets();
            //String Updates (low priority)
            Register<SetStringsPacket>(SetStringsPacket.Read);
            Register<AddStringPacket>(AddStringPacket.Read);
        }
    }

    public class AddStringPacket : IPacket
    {
        public string ToAdd;

        public static AddStringPacket Read(PacketReader message) => new()
        {
            ToAdd = message.GetString()
        };

        public void WriteContents(PacketWriter outPacket)
        {
            outPacket.Put(ToAdd);
        }
    }

    public class SetStringsPacket : IPacket
    {
        public byte[] Data;
        public static object Read(PacketReader message)
        {
            return new SetStringsPacket() { Data = message.GetRemainingBytes() };
        }

        public void WriteContents(PacketWriter outPacket)
        {
            outPacket.Put(Data, 0, Data.Length);
        }
    }

    public class LoginSuccessPacket : IPacket
    {
        public static object Read(PacketReader message)
        {
            return new LoginSuccessPacket();
        }

        public void WriteContents(PacketWriter outPacket)
        {
        }
    }
    public class GuidAuthenticationPacket : IPacket
    {
        public static GuidAuthenticationPacket Read(PacketReader message)
        {
            return new GuidAuthenticationPacket() { };
        }
        public void WriteContents(PacketWriter outPacket)
        {
        }
    }

    public class AuthenticationReplyPacket : IPacket
    {
        public Guid Guid;
        public static AuthenticationReplyPacket Read(PacketReader message)
        {
            return new AuthenticationReplyPacket() { Guid = message.GetGuid() };
        }
        public void WriteContents(PacketWriter outPacket)
        {
            outPacket.Put(Guid);
        }

    }

    [Flags]
    public enum ObjectSpawnFlags : byte
    {
        Debris = (1 << 0),
        Solar = (1 << 1),
        Friendly = (1 << 2),
        Hostile = (1 << 3),
        Neutral = (1 << 4),
        Important = (1 << 5),
        Loot = (1 << 6),
        Mask = 0x7f
    }

    public struct ObjectSpawnInfo
    {
        [Flags]
        enum SpawnHeader : ushort
        {
            //match ObjectSpawnFlags
            Debris = (1 << 0),
            Solar = (1 << 1),
            Friendly = (1 << 2),
            Hostile = (1 << 3),
            Neutral = (1 << 4),
            Important = (1 << 5),
            Loot = (1 << 6),
            //internal field != default
            Name = (1 << 7),
            Affiliation = (1 << 8),
            Comm = (1 << 9),
            Dock = (1 << 10),
            Destroyed = (1 << 11),
            Effects = (1 << 12),
            NicknameNotNull = (1 << 13),
        }
        public ObjNetId ID;
        public string Nickname;
        public ObjectSpawnFlags Flags;
        public ObjectName Name;
        public Vector3 Position;
        public Quaternion Orientation;

        public uint Affiliation;

        public uint CommHead;
        public uint CommBody;
        public uint CommHelmet;

        public uint DebrisPart;
        public NetLoadout Loadout;
        public DockAction? Dock;
        public uint[] DestroyedParts;
        public SpawnedEffect[] Effects;

        public static ObjectSpawnInfo Read(PacketReader message)
        {
            var result = new ObjectSpawnInfo()
            {
                ID = ObjNetId.Read(message)
            };
            var header16 = message.GetShort();
            var header = (SpawnHeader)header16;
            result.Flags = (ObjectSpawnFlags)header16 & ObjectSpawnFlags.Mask;
            result.Position = message.GetVector3();
            result.Orientation = message.GetQuaternion();
            if (header.HasFlag(SpawnHeader.Name)) result.Name = message.GetObjectName();
            if (header.HasFlag(SpawnHeader.NicknameNotNull)) result.Nickname = message.GetString();
            if (header.HasFlag(SpawnHeader.Affiliation)) result.Affiliation = message.GetUInt();
            if (header.HasFlag(SpawnHeader.Comm))
            {
                result.CommHead = message.GetUInt();
                result.CommBody = message.GetUInt();
                result.CommHelmet = message.GetUInt();
            }
            if (header.HasFlag(SpawnHeader.Debris))
            {
                result.DebrisPart = message.GetUInt();
            }
            result.Loadout = NetLoadout.Read(message);
            if (header.HasFlag(SpawnHeader.Dock))
            {
                result.Dock = GetDock(message);
            }
            if (header.HasFlag(SpawnHeader.Destroyed))
            {
                result.DestroyedParts = new uint[message.GetVariableUInt32()];
                for (int i = 0; i < result.DestroyedParts.Length; i++)
                {
                    result.DestroyedParts[i] = message.GetUInt();
                }
            }
            else
            {
                result.DestroyedParts = [];
            }
            if (header.HasFlag(SpawnHeader.Effects))
            {
                result.Effects = new SpawnedEffect[message.GetVariableUInt32()];
                for (int i = 0; i < result.Effects.Length; i++)
                {
                    result.Effects[i] = SpawnedEffect.Read(message);
                }
            }
            else
            {
                result.Effects = [];
            }
            return result;
        }

        public void Put(PacketWriter message)
        {
            // Build header
            SpawnHeader header = (SpawnHeader)(ushort)Flags;
            if(Name != null) header |= SpawnHeader.Name;
            if(!string.IsNullOrEmpty(Nickname)) header |= SpawnHeader.NicknameNotNull;
            if(Affiliation != 0) header |= SpawnHeader.Affiliation;
            if(CommHead != 0 || CommBody != 0 || CommHelmet != 0) header |= SpawnHeader.Comm;
            if(Dock != null) header |= SpawnHeader.Dock;
            if(DestroyedParts is { Length: >0 }) header |= SpawnHeader.Destroyed;
            if(Effects is { Length: >0 }) header |= SpawnHeader.Effects;
            //Write
            ID.Put(message);
            message.Put((ushort)header);
            message.Put(Position);
            message.Put(Orientation);
            if(Name != null) message.Put(Name);
            if(!string.IsNullOrEmpty(Nickname)) message.Put(Nickname);
            if(Affiliation != 0) message.Put(Affiliation);
            if (CommHead != 0 || CommBody != 0 || CommHelmet != 0)
            {
                message.Put(CommHead);
                message.Put(CommBody);
                message.Put(CommHelmet);
            }
            if (header.HasFlag(SpawnHeader.Debris)) // Set from source flags
            {
                message.Put(DebrisPart);
            }
            Loadout.Put(message);
            if (Dock != null)
            {
                PutDock(message, Dock);
            }

            if (DestroyedParts is { Length: > 0 })
            {
                message.PutVariableUInt32((uint)DestroyedParts.Length);
                for (int i = 0; i < DestroyedParts.Length; i++)
                {
                    message.Put(DestroyedParts[i]);
                }
            }

            if (Effects is { Length: > 0 })
            {
                message.PutVariableUInt32((uint)Effects.Length);
                for (int i = 0; i < Effects.Length; i++)
                {
                    Effects[i].Put(message);
                }
            }
        }

        static DockAction GetDock(PacketReader message)
        {
            var k = message.GetByte();
            if (k == 0) return null;
            return new DockAction()
            {
                Kind = (DockKinds) (k >> 4),
                Target = message.GetString(),
                TargetLeft = message.GetString(),
                Exit = message.GetString(),
                Tunnel = message.GetString()
            };
        }

        static void PutDock(PacketWriter message, DockAction dock)
        {
            if (dock != null)
            {
                message.Put((byte)(((byte)dock.Kind << 4) | 1));
                message.Put(dock.Target);
                message.Put(dock.TargetLeft);
                message.Put(dock.Exit);
                message.Put(dock.Tunnel);
            }
            else
            {
                message.Put((byte)0);
            }
        }
    }

    public record struct NetBasicCargo(uint EquipCRC, int Count)
    {
        public static NetBasicCargo Read(PacketReader message) => new(
            message.GetUInt(), message.GetVariableInt32());

        public void Put(PacketWriter message)
        {
            message.Put(EquipCRC);
            message.PutVariableInt32(Count);
        }
    }

    public class NetShipCargo
    {
        public int ID;
        public uint EquipCRC;
        public string Hardpoint;
        public byte Health;
        public int Count;

        public NetShipCargo(int id, uint crc, string hp, byte health, int count)
        {
            ID = id;
            EquipCRC = crc;
            Hardpoint = hp;
            Health = health;
            Count = count;
        }

        public static NetShipCargo Read(PacketReader message) => new(
            message.GetVariableInt32(),
            message.GetUInt(),
            message.GetHpid(),
            message.GetByte(),
            (int)message.GetVariableUInt32()
        );

        public void Put(PacketWriter message)
        {
            message.PutVariableInt32(ID);
            message.Put(EquipCRC);
            message.PutHpid(Hardpoint);
            message.Put(Health);
            message.PutVariableUInt32((uint)Count);
        }
    }

    public class NetLoadout
    {
        public uint ArchetypeCrc;
        public float Health;
        public List<NetShipCargo> Items;
        public static NetLoadout Read(PacketReader message)
        {
            var s = new NetLoadout();
            s.ArchetypeCrc = message.GetUInt();
            s.Health = message.GetFloat();
            var cargoCount = (int)message.GetVariableUInt32();
            s.Items = new List<NetShipCargo>(cargoCount);
            for (int i = 0; i < cargoCount; i++)
            {
                s.Items.Add(NetShipCargo.Read(message));
            }
            return s;
        }
        public void Put(PacketWriter message)
        {
            message.Put(ArchetypeCrc);
            message.Put(Health);
            message.PutVariableUInt32((uint) Items.Count);
            foreach (var c in Items)
            {
                c.Put(message);
            }
        }
    }

    public struct PlayerAuthState
    {
        public Vector3 Position;
        public Quaternion Orientation;
        public Vector3 LinearVelocity;
        public Vector3 AngularVelocity;

        public float Health;
        public float Shield;
        public float CruiseChargePct;
        public float CruiseAccelPct;
        public static PlayerAuthState Read(ref BitReader reader, PlayerAuthState src)
        {
            var pa = new PlayerAuthState();
            pa.Position = DecodeVector3(ref reader, src.Position);
            //Extra precision
            pa.Orientation = reader.GetQuaternion(18);
            pa.LinearVelocity = DecodeVector3(ref reader, src.LinearVelocity);
            pa.AngularVelocity = DecodeVector3(ref reader, src.AngularVelocity);
            pa.Health = reader.GetBool() ? reader.GetFloat() : src.Health;
            pa.Shield = reader.GetBool() ? reader.GetFloat() : src.Shield;
            pa.CruiseChargePct = reader.GetBool() ? reader.GetRangedFloat(0, 1, 12) : src.CruiseChargePct;
            pa.CruiseAccelPct = reader.GetBool() ? reader.GetRangedFloat(0, 1, 12) : src.CruiseAccelPct;
            return pa;
        }

        static void EncodeFloat(ref BitWriter writer, float old, float current, bool force)
        {
            var diff = current - old;
            if (!force && diff >= -32 && diff < 31) {
                writer.PutBool(true);
                writer.PutRangedFloat(diff, -32, 31, 24);
            }
            else {
                writer.PutBool(false);
                writer.PutFloat(current);
            }
        }

        static void EncodeVec3(ref BitWriter writer, Vector3 old, Vector3 current, bool force)
        {
            EncodeFloat(ref writer, old.X, current.X, force);
            EncodeFloat(ref writer, old.Y, current.Y, force);
            EncodeFloat(ref writer, old.Z, current.Z, force);
        }

        static float DecodeFloat(ref BitReader reader, float old) =>
            reader.GetBool()
                ? old + reader.GetRangedFloat(-32, 31, 24)
                : reader.GetFloat();

        static Vector3 DecodeVector3(ref BitReader reader, Vector3 old) =>
            new Vector3(DecodeFloat(ref reader, old.X), DecodeFloat(ref reader, old.Y), DecodeFloat(ref reader, old.Z));

        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        public void Write(ref BitWriter writer, PlayerAuthState prev, uint tick)
        {
            uint forced = tick % 15;
            EncodeVec3(ref writer, prev.Position, Position, forced == 1);
            //Extra precision
            writer.PutQuaternion(Orientation, 18);
            EncodeVec3(ref writer, prev.LinearVelocity, LinearVelocity, forced == 3);
            EncodeVec3(ref writer, prev.AngularVelocity, AngularVelocity, forced == 5);

            if (forced != 7 && Health == prev.Health) {
                writer.PutBool(false);
            }
            else {
                writer.PutBool(true);
                writer.PutFloat(Health);
            }
            if (forced != 9 && Shield == prev.Shield) {
                writer.PutBool(false);
            }
            else {
                writer.PutBool(true);
                writer.PutFloat(Shield);
            }
            if(forced != 11 && NetPacking.QuantizedEqual(CruiseChargePct, prev.CruiseChargePct, 0, 1, 12))
                writer.PutBool(false);
            else
            {
                writer.PutBool(true);
                writer.PutRangedFloat(CruiseChargePct, 0, 1, 12);
            }
            if(forced != 13 && NetPacking.QuantizedEqual(CruiseAccelPct, prev.CruiseAccelPct, 0, 1, 12))
                writer.PutBool(false);
            else
            {
                writer.PutBool(true);
                writer.PutRangedFloat(CruiseAccelPct, 0, 1, 12);
            }
        }
    }

    public struct NetInputControls
    {
        public uint Tick;
        public Vector3 Steering;
        public Vector3 AimPoint;
        public StrafeControls Strafe;
        public float Throttle;
        public bool Cruise;
        public bool Thrust;
        public ProjectileFireCommand? FireCommand;
    }

    public struct ProjectileFireCommand
    {
        public Vector3 Target;
        //1 bit set for each gun on owner that fired
        public ulong Guns;
        //1 bit set for each gun not firing at Target
        public ulong Unique;
        public Vector3[] OtherTargets;
    }


    public class InputUpdatePacket : IPacket
    {
        public ObjNetId SelectedObject;
        public UpdateAck Acks;
        public NetInputControls Current;
        public NetInputControls HistoryA;
        public NetInputControls HistoryB;
        public NetInputControls HistoryC;

        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        static void WriteDelta(ref BitWriter writer, ref NetInputControls baseline, ref NetInputControls cur)
        {
            writer.PutVarInt32((int)((long)cur.Tick - baseline.Tick));
            writer.PutUInt((uint)cur.Strafe, 4);
            writer.PutBool(cur.Cruise);
            writer.PutBool(cur.Thrust);
            writer.PutBool(cur.Throttle != baseline.Throttle);
            writer.PutBool(cur.Steering != baseline.Steering);
            writer.PutBool(cur.AimPoint != baseline.AimPoint);
            if(cur.Throttle != baseline.Throttle)
                writer.PutFloat(cur.Throttle);
            if(cur.Steering != baseline.Steering)
                writer.PutVector3(cur.Steering);
            if(cur.AimPoint != baseline.AimPoint)
                writer.PutVector3(cur.AimPoint);
            WriteFireCommand(ref writer, ref cur);
        }

        static ProjectileFireCommand? ReadFireCommand(ref BitReader reader, ref NetInputControls controls)
        {
            if (!reader.GetBool())
                return null;
            var fc = new ProjectileFireCommand();
            if (reader.GetBool())
                fc.Target = reader.GetVector3();
            else
                fc.Target = controls.AimPoint;
            fc.Guns = reader.GetVarUInt64();
            fc.Unique = reader.GetVarUInt64();
            if (fc.Unique > 0) {
                var c = BitOperations.PopCount(fc.Unique);
                fc.OtherTargets = new Vector3[c];
                for (int i = 0; i < c; i++)
                    fc.OtherTargets[i] = reader.GetVector3();
            } else {
                fc.OtherTargets = Array.Empty<Vector3>();
            }
            return fc;
        }

        static void WriteFireCommand(ref BitWriter writer, ref NetInputControls controls)
        {
            if (controls.FireCommand == null) {
                writer.PutBool(false);
                return;
            }
            var fc = controls.FireCommand.Value;
            writer.PutBool(true);
            if (fc.Target != controls.AimPoint) {
                writer.PutBool(true);
                writer.PutVector3(fc.Target);
            }
            else {
                writer.PutBool(false);
            }
            writer.PutVarUInt64(fc.Guns);
            writer.PutVarUInt64(fc.Unique);
            if (fc.OtherTargets is { Length: > 0 })
            {
                for(int i = 0; i < fc.OtherTargets.Length; i++)
                    writer.PutVector3(fc.OtherTargets[i]);
            }
        }

        static NetInputControls ReadDelta(ref BitReader reader, ref NetInputControls baseline)
        {
            var nc = new NetInputControls();
            nc.Tick = (uint) (baseline.Tick + reader.GetVarInt32());
            nc.Strafe = (StrafeControls)reader.GetUInt(4);
            nc.Cruise = reader.GetBool();
            nc.Thrust = reader.GetBool();
            bool readThrottle = reader.GetBool();
            bool readSteering = reader.GetBool();
            bool readAimPoint = reader.GetBool();
            nc.Throttle = readThrottle ? reader.GetFloat() : baseline.Throttle;
            nc.Steering = readSteering ? reader.GetVector3() : baseline.Steering;
            nc.AimPoint = readAimPoint ? reader.GetVector3() : baseline.AimPoint;
            nc.FireCommand = ReadFireCommand(ref reader, ref nc);
            return nc;
        }

        public static object Read(PacketReader message)
        {
            var br = new BitReader(message.GetRemainingBytes(), 0);
            var p = new InputUpdatePacket();
            p.Acks = new UpdateAck(br.GetVarUInt32(), br.GetUInt(), br.GetUInt());
            p.SelectedObject = ObjNetId.Read(ref br);
            p.Current.Tick = br.GetVarUInt32();
            p.Current.Steering = br.GetVector3();
            p.Current.AimPoint = br.GetVector3();
            p.Current.Strafe = (StrafeControls) br.GetUInt(4);
            p.Current.Cruise = br.GetBool();
            p.Current.Thrust = br.GetBool();
            var throttle = br.GetUInt(2);
            if (throttle == 0) p.Current.Throttle = 0;
            else if (throttle == 1) p.Current.Throttle = 1;
            else {
                p.Current.Throttle = br.GetFloat();
            }
            p.Current.FireCommand = ReadFireCommand(ref br, ref p.Current);
            p.HistoryA = ReadDelta(ref br, ref p.Current);
            p.HistoryB = ReadDelta(ref br, ref p.HistoryA);
            p.HistoryC = ReadDelta(ref br, ref p.HistoryB);
            return p;
        }

        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        public void WriteContents(PacketWriter outPacket)
        {
            var bw = new BitWriter();
            bw.PutVarUInt32(Acks.Tick);
            bw.PutUInt(Acks.History0, 32);
            bw.PutUInt(Acks.History1, 32);
            SelectedObject.Put(bw);
            bw.PutVarUInt32(Current.Tick);
            bw.PutVector3(Current.Steering);
            bw.PutVector3(Current.AimPoint);
            bw.PutUInt((uint)Current.Strafe, 4);
            bw.PutBool(Current.Cruise);
            bw.PutBool(Current.Thrust);
            if (Current.Throttle == 0){
                bw.PutUInt(0, 2);
            } else if (Current.Throttle >= 1){
                bw.PutUInt(1,2);
            }else {
                bw.PutUInt(2, 2);
                bw.PutFloat(Current.Throttle);
            }
            WriteFireCommand(ref bw, ref Current);
            WriteDelta(ref bw, ref Current, ref HistoryA);
            WriteDelta(ref bw, ref HistoryA, ref HistoryB);
            WriteDelta(ref bw, ref HistoryB, ref HistoryC);
            bw.WriteToPacket(outPacket);
        }
    }


    public class NetDlgLine
    {
        public int Source;
        public bool TargetIsPlayer;
        public string Voice;
        public uint Hash;
        public static NetDlgLine Read(PacketReader message) => new NetDlgLine()
            {Source = message.GetVariableInt32(), TargetIsPlayer = message.GetBool(), Voice = message.GetString(), Hash = message.GetUInt()};
        public void Put(PacketWriter message)
        {
            message.PutVariableInt32(Source);
            message.Put(TargetIsPlayer);
            message.Put(Voice);
            message.Put(Hash);
        }
    }
}
