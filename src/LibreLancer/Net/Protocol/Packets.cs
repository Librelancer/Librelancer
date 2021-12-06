// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Net;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.VisualBasic;
using Quaternion = System.Numerics.Quaternion;

namespace LibreLancer
{
    public interface IPacket
    {
        void WriteContents(NetDataWriter msg);
    }

    public static class Packets
    {
        static List<Func<NetPacketReader, object>> parsers = new List<Func<NetPacketReader,object>>();
        static List<Type> packetTypes = new List<Type>();
        public static void Register<T>(Func<NetPacketReader,object> parser) where T : IPacket
        {
            packetTypes.Add(typeof(T));
            parsers.Add(parser);
        }

        public static void Write(NetDataWriter message, IPacket p)
        {
            var pkt = packetTypes.IndexOf(p.GetType());
            if(pkt == -1) throw new Exception($"Packet type not registered {p.GetType().Name}");
            message.PutVariableUInt32((uint) pkt);
            p.WriteContents(message);
        }

        public static IPacket Read(NetPacketReader message)
        { 
            return (IPacket)parsers[(int)message.GetVariableUInt32()](message);
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
            Register<AuthenticationPacket>(AuthenticationPacket.Read);
            Register<AuthenticationReplyPacket>(AuthenticationReplyPacket.Read);
            Register<LoginSuccessPacket>(LoginSuccessPacket.Read);
            //Menu
            Register<OpenCharacterListPacket>(OpenCharacterListPacket.Read);
            Register<NewCharacterDBPacket>(NewCharacterDBPacket.Read);
            Register<AddCharacterPacket>(AddCharacterPacket.Read);
            //Space
            Register<PositionUpdatePacket>(PositionUpdatePacket.Read);
            Register<ObjectUpdatePacket>(ObjectUpdatePacket.Read);
            //Protocol
            GeneratedProtocol.RegisterPackets();
        }
    }
    
    public class LoginSuccessPacket : IPacket
    {
        public static object Read(NetPacketReader message)
        {
            return new LoginSuccessPacket();
        }

        public void WriteContents(NetDataWriter msg)
        {
        }
    }
    public class AuthenticationPacket : IPacket
    {
        public AuthenticationKind Type;
        public static AuthenticationPacket Read(NetPacketReader message)
        {
            return new AuthenticationPacket() { Type = (AuthenticationKind)message.GetByte() };
        }
        public void WriteContents(NetDataWriter message)
        {
            message.Put((byte)Type);
        }
    }

    public class AuthenticationReplyPacket : IPacket
    {
        public Guid Guid;
        public static AuthenticationReplyPacket Read(NetPacketReader message)
        {
            return new AuthenticationReplyPacket() { Guid = new Guid(message.GetBytes(16)) };
        }
        public void WriteContents(NetDataWriter message)
        {
            message.Put(Guid.ToByteArray());
        }

    }
    
    public class SolarInfo
    {
        public int ID;
        public string Archetype;
        public Vector3 Position;
        public Quaternion Orientation;
        public static SolarInfo Read(NetPacketReader message)
        {
            return new SolarInfo
            {
                ID = message.GetInt(),
                Archetype = message.GetString(),
                Position = message.GetVector3(),
                Orientation = message.GetQuaternion()
            };
        }
        public void Put(NetDataWriter message)
        {
            message.Put(ID);
            message.Put(Archetype);
            message.Put(Position);
            message.Put(Orientation);
        }
    }

    public class NetShipEquip
    {
        public uint HardpointCRC;
        public uint EquipCRC;
        public byte Health;
        public NetShipEquip(uint hardpoint, uint crc, byte health)
        {
            HardpointCRC = hardpoint;
            EquipCRC = crc;
            Health = health;
        }
    }

    public class NetShipCargo
    {
        public int ID;
        public uint EquipCRC;
        public int Count;

        public NetShipCargo(int id, uint crc, int count)
        {
            ID = id;
            EquipCRC = crc;
            Count = count;
        }
    }

    public class NetShipLoadout
    {
        public uint ShipCRC;
        public float Health;
        public List<NetShipEquip> Equipment;
        public List<NetShipCargo> Cargo;
        public static NetShipLoadout Read(NetPacketReader message)
        {
            var s = new NetShipLoadout();
            s.ShipCRC = message.GetUInt();
            s.Health = message.GetFloat();
            var equipCount = (int)message.GetVariableUInt32();
            s.Equipment = new List<NetShipEquip>(equipCount);
            for(int i = 0; i < equipCount; i++) {
                s.Equipment.Add(new NetShipEquip(message.GetUInt(), message.GetUInt(), message.GetByte()));
            }
            var cargoCount = (int)message.GetVariableUInt32();
            s.Cargo = new List<NetShipCargo>(cargoCount);
            for (int i = 0; i < cargoCount; i++)
            {
                s.Cargo.Add(new NetShipCargo(message.GetInt(), message.GetUInt(), message.GetInt()));
            }
            return s;
        }
        public void Put(NetDataWriter message)
        {
            message.Put(ShipCRC);
            message.Put(Health);
            message.PutVariableUInt32((uint)Equipment.Count);
            foreach(var equip in Equipment) {
                message.Put(equip.HardpointCRC);
                message.Put(equip.EquipCRC);
                message.Put(equip.Health);
            }
            message.PutVariableUInt32((uint) Cargo.Count);
            foreach (var c in Cargo)
            {
                message.Put(c.ID);
                message.Put(c.EquipCRC);
                message.Put(c.Count);
            }
        }
    }

    public class ObjectUpdatePacket : IPacket
    {
        public uint Tick;
        public float PlayerHealth;
        public float PlayerShield;
        public PackedShipUpdate[] Updates;
        public const int UpdateLimit = byte.MaxValue;
        public static object Read(NetPacketReader message)
        {
            var p = new ObjectUpdatePacket();
            p.Tick = message.GetUInt();
            p.PlayerHealth = message.GetFloat();
            p.PlayerShield = message.GetFloat();
            var pack = new BitReader(message.GetRemainingBytes(), 0);
            var updateCount = pack.GetUInt(8);
            p.Updates = new PackedShipUpdate[updateCount];
            for(int i = 0; i < p.Updates.Length; i++)
                p.Updates[i] = PackedShipUpdate.ReadFrom(ref pack);
            return p;
        }
        public void WriteContents(NetDataWriter message)
        {
            message.Put(Tick);
            message.Put(PlayerHealth);
            message.Put(PlayerShield);
            var writer = new BitWriter();
            if(Updates.Length > 255)
                throw new Exception("Too many updates for net packet");
            writer.PutUInt((uint) Updates.Length, 8);
            foreach (var p in Updates)
                p.WriteTo (ref writer);
            writer.WriteToPacket(message);
        }
    }

    public struct GunOrient
    {
        public uint Hardpoint;
        public float AnglePitch;
        public float AngleRot;
        public void ReadIn(ref BitReader message)
        {
            Hardpoint = message.GetUInt();
            AnglePitch = message.GetRadiansQuantized();
            AngleRot = message.GetRadiansQuantized();
        }
        public void WriteTo(ref BitWriter message)
        {
            //8 bytes each
            message.PutUInt(Hardpoint, 32);
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
        //Player ID
        public int ID;
        //1 byte bitfield
        public bool Hidden;
        public CruiseThrustState CruiseThrust; //2 bits
        public bool HasPosition;
        public bool HasOrientation;
        public bool HasHealth;
        public bool HasGuns;
        public bool DockingLights;
        //Position - 13 bytes (vec3 + throttle)
        public Vector3 Position;
        public byte EngineThrottlePct;
        //Orientation
        public Quaternion Orientation; //packed 6 bytes
        //Health!
        public bool HasShield; //1 bit
        public bool HasHull; //1 bit
        public bool HasParts; //1 bit
        public byte[] Parts; //3 bits each
        public int ShieldHp; //4 bytes
        public int HullHp; //4 bytes
        //Guns
        public GunOrient[] GunOrients; //4 bytes each (half floats)

        public void WriteTo(ref BitWriter message)
        {
            message.PutInt(ID);
            message.PutBool(Hidden);
            if(Hidden)
            {
                return;
            }
            message.PutUInt((uint)CruiseThrust, 2); //2 bits
            message.PutBool(HasPosition);
            message.PutBool(HasOrientation);
            message.PutBool(HasHealth);
            message.PutBool(HasGuns);
            message.PutBool(DockingLights);
            if (HasPosition)
            {
                message.PutVector3(Position);
                message.PutByte(EngineThrottlePct);
            }
            if(HasOrientation) 
                message.PutQuaternion(Orientation);
            if(HasHealth) 
            {
                message.PutBool(HasShield);
                message.PutBool(HasHull);
                message.PutBool(HasParts);
                if(HasParts)
                {
                    message.PutByte((byte)Parts.Length);
                    for(int i = 0; i < Parts.Length; i++) {
                        message.PutUInt(Parts[i], 3);
                    }
                }
                if (HasShield) message.PutInt(ShieldHp); //4 bytes
                if (HasHull) message.PutInt(HullHp); //4 bytes
            }
            if(HasGuns) 
            {
                message.PutByte((byte)GunOrients.Length);
                foreach (var g in GunOrients)
                    g.WriteTo(ref message);
            }
        }
        public static PackedShipUpdate ReadFrom(ref BitReader message)
        {
            var p = new PackedShipUpdate();
            p.ID = (int)message.GetUInt(32);
            p.Hidden = message.GetBool();
            if(p.Hidden)
            {
                return p;
            }
            p.CruiseThrust = (CruiseThrustState)message.GetUInt(2);
            p.HasPosition = message.GetBool();
            p.HasOrientation = message.GetBool();
            p.HasHealth = message.GetBool();
            p.HasGuns = message.GetBool();
            p.DockingLights = message.GetBool();
            if(p.HasPosition)
            {
                p.Position = message.GetVector3();
                p.EngineThrottlePct = message.GetByte();
            }
            if(p.HasOrientation)
                p.Orientation = message.GetQuaternion();
            if(p.HasHealth)
            {
                p.HasShield = message.GetBool();
                p.HasHull = message.GetBool();
                p.HasParts = message.GetBool();
                if(p.HasParts) {
                    p.Parts = new byte[message.GetByte()];
                    for(int i = 0; i < p.Parts.Length; i++)
                    {
                        p.Parts[i] = (byte)message.GetUInt(3);
                    }
                }
                if (p.HasShield) p.ShieldHp = message.GetInt();
                if (p.HasHull) p.HullHp = message.GetInt();
            }
            if (p.HasGuns)
            {
                p.GunOrients = new GunOrient[message.GetByte()];
                for (int i = 0; i < p.GunOrients.Length; i++) {
                    p.GunOrients[i].ReadIn(ref message);
                }
            }
            return p;
        }
    }
    

    public class PositionUpdatePacket : IPacket
    {
        public Vector3 Position;
        public Quaternion Orientation;
        public float Speed;

        public static object Read(NetPacketReader message)
        {
            return new PositionUpdatePacket()
            {
                Position = message.GetVector3(),
                Orientation = message.GetQuaternion(),
                Speed = message.GetFloat()
            };
        }
        public void WriteContents(NetDataWriter msg)
        {
            msg.Put(Position);
            msg.Put(Orientation);
            msg.Put(Speed);
        }
    }
    

    public class NetDlgLine
    {
        public string Voice;
        public uint Hash;
        public static NetDlgLine Read(NetPacketReader message) => new NetDlgLine()
            {Voice = message.GetString(), Hash = message.GetUInt()};
        public void Put(NetDataWriter message)
        {
            message.Put(Voice);
            message.Put(Hash);
        }
    }
}
