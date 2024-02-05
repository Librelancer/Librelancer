// MIT License - Copyright (c) Callum McGing
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
            outPacket.PutVariableUInt32((uint) pkt);
            p.WriteContents(outPacket);
        }

        public static IPacket Read(PacketReader inPacket)
        {
            return (IPacket)parsers[(int)inPacket.GetVariableUInt32()](inPacket);
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

    public class SolarInfo
    {
        public int ID;
        public ObjectName Name;
        public string Nickname;
        public string Archetype;
        public string Faction;
        public DockAction Dock;
        public Vector3 Position;
        public Quaternion Orientation;

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

        public static SolarInfo Read(PacketReader message)
        {
            return new SolarInfo
            {
                ID = message.GetVariableInt32(),
                Name = message.GetObjectName(),
                Nickname = message.GetString(),
                Archetype = message.GetString(),
                Faction = message.GetString(),
                Dock = GetDock(message),
                Position = message.GetVector3(),
                Orientation = message.GetQuaternion()
            };
        }
        public void Put(PacketWriter message)
        {
            message.PutVariableInt32(ID);
            message.Put(Name);
            message.Put(Nickname);
            message.Put(Archetype);
            message.Put(Faction);
            if (Dock != null)
            {
                message.Put((byte)(((byte)Dock.Kind << 4) | 1));
                message.Put(Dock.Target);
                message.Put(Dock.TargetLeft);
                message.Put(Dock.Exit);
                message.Put(Dock.Tunnel);
            }
            else
            {
                message.Put((byte)0);
            }
            message.Put(Position);
            message.Put(Orientation);
        }
    }

    public struct ShipSpawnInfo
    {
        public ObjectName Name;
        public Vector3 Position;
        public Quaternion Orientation;
        public uint CommHead;
        public uint CommBody;
        public uint CommHelmet;
        public uint Affiliation;
        public NetShipLoadout Loadout;

        public static ShipSpawnInfo Read(PacketReader message) => new ShipSpawnInfo()
        {
            Name = message.GetObjectName(),
            Position = message.GetVector3(),
            Orientation = message.GetQuaternion(),
            CommHead = message.GetUInt(),
            CommBody = message.GetUInt(),
            CommHelmet = message.GetUInt(),
            Affiliation = message.GetUInt(),
            Loadout = NetShipLoadout.Read(message)
        };

        public void Put(PacketWriter message)
        {
            message.Put(Name);
            message.Put(Position);
            message.Put(Orientation);
            message.Put(CommHead);
            message.Put(CommBody);
            message.Put(CommHelmet);
            message.Put(Affiliation);
            Loadout.Put(message);
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
    }

    public class NetShipLoadout
    {
        public uint ShipCRC;
        public float Health;
        public List<NetShipCargo> Items;
        public static NetShipLoadout Read(PacketReader message)
        {
            var s = new NetShipLoadout();
            s.ShipCRC = message.GetUInt();
            s.Health = message.GetFloat();
            var cargoCount = (int)message.GetVariableUInt32();
            s.Items = new List<NetShipCargo>(cargoCount);
            for (int i = 0; i < cargoCount; i++)
            {
                s.Items.Add(new NetShipCargo(
                    message.GetVariableInt32(),
                    message.GetUInt(),
                    message.GetHpid(),
                    message.GetByte(),
                    (int)message.GetVariableUInt32()
                    ));
            }
            return s;
        }
        public void Put(PacketWriter message)
        {
            message.Put(ShipCRC);
            message.Put(Health);
            message.PutVariableUInt32((uint) Items.Count);
            foreach (var c in Items)
            {
                message.PutVariableInt32(c.ID);
                message.Put(c.EquipCRC);
                message.PutHpid(c.Hardpoint);
                message.Put(c.Health);
                message.PutVariableUInt32((uint)c.Count);
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

        static void EncodeFloat(ref BitWriter writer, float old, float current)
        {
            var diff = current - old;
            if (diff >= -32 && diff < 31) {
                writer.PutBool(true);
                writer.PutRangedFloat(diff, -32, 31, 24);
            }
            else {
                writer.PutBool(false);
                writer.PutFloat(current);
            }
        }

        static void EncodeVec3(ref BitWriter writer, Vector3 old, Vector3 current)
        {
            EncodeFloat(ref writer, old.X, current.X);
            EncodeFloat(ref writer, old.Y, current.Y);
            EncodeFloat(ref writer, old.Z, current.Z);
        }

        static float DecodeFloat(ref BitReader reader, float old) =>
            reader.GetBool()
                ? old + reader.GetRangedFloat(-32, 31, 24)
                : reader.GetFloat();

        static Vector3 DecodeVector3(ref BitReader reader, Vector3 old) =>
            new Vector3(DecodeFloat(ref reader, old.X), DecodeFloat(ref reader, old.Y), DecodeFloat(ref reader, old.Z));

        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        public void Write(ref BitWriter writer, PlayerAuthState prev)
        {
            EncodeVec3(ref writer, prev.Position, Position);
            //Extra precision
            writer.PutQuaternion(Orientation, 18);
            EncodeVec3(ref writer, prev.LinearVelocity, LinearVelocity);
            EncodeVec3(ref writer, prev.AngularVelocity, AngularVelocity);

            if (Health == prev.Health) {
                writer.PutBool(false);
            }
            else {
                writer.PutBool(true);
                writer.PutFloat(Health);
            }
            if (Shield == prev.Shield) {
                writer.PutBool(false);
            }
            else {
                writer.PutBool(true);
                writer.PutFloat(Shield);
            }
            if(NetPacking.QuantizedEqual(CruiseChargePct, prev.CruiseChargePct, 0, 1, 12))
                writer.PutBool(false);
            else
            {
                writer.PutBool(true);
                writer.PutRangedFloat(CruiseChargePct, 0, 1, 12);
            }
            if(NetPacking.QuantizedEqual(CruiseAccelPct, prev.CruiseAccelPct, 0, 1, 12))
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
        public uint AckTick;
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
            p.AckTick = br.GetVarUInt32();
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
            bw.PutVarUInt32(AckTick);
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
