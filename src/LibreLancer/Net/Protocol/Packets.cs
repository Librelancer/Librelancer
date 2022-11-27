// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using LibreLancer.World.Components;

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
            Register<ObjectUpdatePacket>(ObjectUpdatePacket.Read);
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
        public string Archetype;
        public Vector3 Position;
        public Quaternion Orientation;
        public static SolarInfo Read(PacketReader message)
        {
            return new SolarInfo
            {
                ID = message.GetInt(),
                Archetype = message.GetString(),
                Position = message.GetVector3(),
                Orientation = message.GetQuaternion()
            };
        }
        public void Put(PacketWriter message)
        {
            message.Put(ID);
            message.Put(Archetype);
            message.Put(Position);
            message.Put(Orientation);
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
        public float Health;
        public float Shield;
        public Vector3 Position;
        public Quaternion Orientation;
        public Vector3 LinearVelocity;
        public Vector3 AngularVelocity;
        public float CruiseChargePct;
        public float CruiseAccelPct;
        public static PlayerAuthState Read(ref BitReader reader)
        {
            var pa = new PlayerAuthState();
            pa.Health = reader.GetFloat();
            pa.Shield = reader.GetFloat();
            pa.Position = reader.GetVector3();
            //Extra precision
            pa.Orientation = reader.GetQuaternion(18);
            pa.LinearVelocity = reader.GetVector3();
            pa.AngularVelocity = reader.GetVector3();
            pa.CruiseChargePct = reader.GetRangedFloat(0, 1, 12);
            pa.CruiseAccelPct = reader.GetRangedFloat(0, 1, 12);
            return pa;
        }

        public void Write(ref BitWriter writer)
        {
            writer.PutFloat(Health);
            writer.PutFloat(Shield);
            writer.PutVector3(Position);
            //Extra precision
            writer.PutQuaternion(Orientation, 18);
            writer.PutVector3(LinearVelocity);
            writer.PutVector3(AngularVelocity);
            writer.PutRangedFloat(CruiseChargePct, 0, 1, 12);
            writer.PutRangedFloat(CruiseAccelPct, 0, 1, 12);
        }
    }

    public class ObjectUpdatePacket : IPacket
    {
        public uint Tick;
        public int InputSequence;
        public PlayerAuthState PlayerState;
        public PackedShipUpdate[] Updates;
        public const int UpdateLimit = byte.MaxValue;
        public static object Read(PacketReader message)
        {
            var p = new ObjectUpdatePacket();
            p.Tick = message.GetUInt();
            p.InputSequence = message.GetInt();
            var pack = new BitReader(message.GetRemainingBytes(), 0, message.HpidReader);
            p.PlayerState = PlayerAuthState.Read(ref pack);
            var updateCount = pack.GetUInt(8);
            p.Updates = new PackedShipUpdate[updateCount];
            for(int i = 0; i < p.Updates.Length; i++)
                p.Updates[i] = PackedShipUpdate.ReadFrom(ref pack);
            return p;
        }
        public void WriteContents(PacketWriter outPacket)
        {
            outPacket.Put(Tick);
            outPacket.Put(InputSequence);
            var writer = new BitWriter();
            writer.HpidWriter = outPacket.HpidWriter;
            PlayerState.Write(ref writer);
            if(Updates.Length > 255)
                throw new Exception("Too many updates for net packet");
            writer.PutUInt((uint) Updates.Length, 8);
            foreach (var p in Updates)
                p.WriteTo (ref writer);
            writer.WriteToPacket(outPacket);
        }
    }

    public struct GunOrient
    {
        public string Hardpoint;
        public float AnglePitch;
        public float AngleRot;
        public void ReadIn(ref BitReader message)
        {
            Hardpoint = message.GetHpid();
            AnglePitch = message.GetRadiansQuantized();
            AngleRot = message.GetRadiansQuantized();
        }
        public void WriteTo(ref BitWriter message)
        {
            //5-9 bytes each
            message.PutHpid(Hardpoint);
            message.PutRadiansQuantized(AnglePitch);
            message.PutRadiansQuantized(AngleRot);
        }
    }

    public enum CruiseThrustState
    {
        None = 0,
        Cruising = 1,
        CruiseCharging = 2,
        Thrusting = 3
    }

    public class PackedShipUpdate
    {
        //Info
        public bool IsCRC;
        public bool HasPosition;
        public CruiseThrustState CruiseThrust; //4-5
        public bool Tradelane; //6
        public bool EngineKill; //7
        public RepAttitude RepToPlayer; //8-9
        //0 = 0%, 1 = 100%, 2 = float
        public uint Shield;
        public bool Hull;
        public float Throttle;
        //Data
        public int ID; //Variable or CRC
        public Vector3 Position;
        public Quaternion Orientation;
        public Vector3 LinearVelocity;
        public Vector3 AngularVelocity;
        public float HullValue;
        public float ShieldValue;
        public GunOrient[] Guns;
        public byte[] PartHealth;

        public void WriteTo(ref BitWriter message)
        {
            //Header - 16 bits
            message.PutBool(IsCRC);
            message.PutBool(HasPosition);
            message.PutBool(Tradelane);
            message.PutBool(EngineKill);
            message.PutBool(Hull);
            message.PutUInt((uint)RepToPlayer, 2);
            message.PutBool(LinearVelocity != Vector3.Zero || AngularVelocity != Vector3.Zero);
            message.PutBool(Guns != null && Guns.Length > 0);
            message.PutBool(PartHealth != null && PartHealth.Length > 0);
            message.PutUInt((uint)CruiseThrust, 2);
            message.PutUInt((uint)Shield, 2);
            if (Throttle == 0) {
                message.PutUInt(0, 2); //0%
            } else if (Throttle == 1) {
                message.PutUInt(1, 2); //100%
            } else if (Throttle > 0) {
                message.PutUInt(2,2); //positive throttle
            }
            else {
                message.PutUInt(3, 2); //negative throttle
            }
            //Data
            if (IsCRC) message.PutInt(ID);
            else message.PutVarInt32(ID);
            if (HasPosition) {
                message.PutVector3(Position);
                message.PutQuaternion(Orientation);
            }
            if (LinearVelocity != Vector3.Zero || AngularVelocity != Vector3.Zero) {
                message.PutRangedVector3(LinearVelocity, -32768, 32767, 24);
                message.PutRangedVector3(AngularVelocity, -16384, 16383, 24);
            }
            if (Throttle != 1 && Throttle != 0) {
                message.PutByte((byte)(Math.Abs(Throttle) * 255f));
            }
            if (Hull) message.PutFloat(HullValue);
            if (Shield == 2) message.PutFloat(ShieldValue);
            if (Guns != null && Guns.Length > 0) {
                message.PutByte((byte)Guns.Length);
                foreach(var g in Guns) g.WriteTo(ref message);
            }
            if (PartHealth != null && PartHealth.Length > 0) {
                message.PutByte((byte)PartHealth.Length);
                foreach(var p in PartHealth) message.PutUInt(p, 3);
            }
        }
        public static PackedShipUpdate ReadFrom(ref BitReader message)
        {
            var p = new PackedShipUpdate();
            p.IsCRC = message.GetBool();
            p.HasPosition = message.GetBool();
            p.Tradelane = message.GetBool();
            p.EngineKill = message.GetBool();
            p.RepToPlayer = (RepAttitude) message.GetUInt(2);
            p.Hull = message.GetBool();
            bool readLinear = message.GetBool();
            bool readAngular = message.GetBool();
            bool readGuns = message.GetBool();
            bool readParts = message.GetBool();
            p.CruiseThrust = (CruiseThrustState) message.GetUInt(2);
            p.Shield = message.GetUInt(2);
            var throttle = message.GetUInt(2);
            if (p.IsCRC) p.ID = message.GetInt();
            else p.ID = message.GetVarInt32();
            if (p.HasPosition) {
                p.Position = message.GetVector3();
                p.Orientation = message.GetQuaternion();
            }
            if (readLinear) p.LinearVelocity = message.GetRangedVector3(-32768, 32767, 24);
            if (readAngular) p.AngularVelocity = message.GetRangedVector3(-16384, 16383, 24);
            switch (throttle)
            {
                case 0:
                    p.Throttle = 0; 
                    break;
                case 1:
                    p.Throttle = 1;
                    break;
                case 2:
                    p.Throttle = message.GetByte() / 255f;
                    break;
                case 3:
                    p.Throttle = -(message.GetByte() / 255f);
                    break;
            }
            if (p.Hull) p.HullValue = message.GetFloat();
            if (p.Shield == 2) p.ShieldValue = message.GetFloat();
            if (readGuns) {
                p.Guns = new GunOrient[message.GetByte()];
                for(int i = 0; i < p.Guns.Length; i++) p.Guns[i].ReadIn(ref message);
            }
            else {
                p.Guns = Array.Empty<GunOrient>();
            }
            if (readParts) {
                p.PartHealth = new byte[message.GetByte()];
                for (int i = 0; i < p.PartHealth.Length; i++) p.PartHealth[i] = (byte) message.GetUInt(3);
            }
            else {
                p.PartHealth = Array.Empty<byte>();
            }
            return p;
        }
    }
    

    public struct NetInputControls
    {
        public int Sequence;
        public Vector3 Steering;
        public StrafeControls Strafe;
        public float Throttle;
        public bool Cruise;
        public bool Thrust;    
    }
    
    public class InputUpdatePacket : IPacket
    {
        public NetInputControls Current;
        public NetInputControls HistoryA;
        public NetInputControls HistoryB;
        public NetInputControls HistoryC;

        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        static void WriteDelta(ref BitWriter writer, ref NetInputControls baseline, ref NetInputControls cur)
        {
            writer.PutVarInt32(cur.Sequence - baseline.Sequence);
            writer.PutUInt((uint)cur.Strafe, 4);
            writer.PutBool(cur.Cruise);
            writer.PutBool(cur.Thrust);
            writer.PutBool(cur.Throttle != baseline.Throttle);
            writer.PutBool(cur.Steering != baseline.Steering);
            if(cur.Throttle != baseline.Throttle)
                writer.PutFloat(cur.Throttle);
            if(cur.Steering != baseline.Steering)
                writer.PutVector3(cur.Steering);
        }
        
        static NetInputControls ReadDelta(ref BitReader reader, ref NetInputControls baseline)
        {
            var nc = new NetInputControls();
            nc.Sequence = baseline.Sequence + reader.GetVarInt32();
            nc.Strafe = (StrafeControls)reader.GetUInt(4);
            nc.Cruise = reader.GetBool();
            nc.Thrust = reader.GetBool();
            bool readThrottle = reader.GetBool();
            bool readSteering = reader.GetBool();
            nc.Throttle = readThrottle ? reader.GetFloat() : baseline.Throttle;
            nc.Steering = readSteering ? reader.GetVector3() : baseline.Steering;
            return nc;
        }

        public static object Read(PacketReader message)
        {
            var br = new BitReader(message.GetRemainingBytes(), 0);
            var p = new InputUpdatePacket();
            p.Current.Sequence = br.GetVarInt32();
            p.Current.Steering = br.GetVector3();
            p.Current.Strafe = (StrafeControls) br.GetUInt(4);
            p.Current.Cruise = br.GetBool();
            p.Current.Thrust = br.GetBool();
            var throttle = br.GetUInt(2);
            if (throttle == 0) p.Current.Throttle = 0;
            else if (throttle == 1) p.Current.Throttle = 1;
            else {
                p.Current.Throttle = br.GetFloat();
            }
            p.HistoryA = ReadDelta(ref br, ref p.Current);
            p.HistoryB = ReadDelta(ref br, ref p.HistoryA);
            p.HistoryC = ReadDelta(ref br, ref p.HistoryB);
            return p;
        }
        
        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        public void WriteContents(PacketWriter outPacket)
        {
            var bw = new BitWriter();
            bw.PutVarInt32(Current.Sequence);
            bw.PutVector3(Current.Steering);
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
            
            WriteDelta(ref bw, ref Current, ref HistoryA);
            WriteDelta(ref bw, ref HistoryA, ref HistoryB);
            WriteDelta(ref bw, ref HistoryB, ref HistoryC);
            bw.WriteToPacket(outPacket);
        }
    }
    

    public class NetDlgLine
    {
        public string Voice;
        public uint Hash;
        public static NetDlgLine Read(PacketReader message) => new NetDlgLine()
            {Voice = message.GetString(), Hash = message.GetUInt()};
        public void Put(PacketWriter message)
        {
            message.Put(Voice);
            message.Put(Hash);
        }
    }
}