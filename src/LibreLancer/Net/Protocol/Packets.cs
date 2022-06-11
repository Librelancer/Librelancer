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
            Register<InputUpdatePacket>(InputUpdatePacket.Read);
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

    public class NetShipCargo
    {
        public int ID;
        public uint EquipCRC;
        public uint HardpointCRC;
        public byte Health;
        public int Count;

        public static readonly uint InternalCrc = CrcTool.FLModelCrc("internal");

        public NetShipCargo(int id, uint crc, uint hp, byte health, int count)
        {
            ID = id;
            EquipCRC = crc;
            HardpointCRC = hp;
            Health = health;
            Count = count;
        }
    }

    public class NetShipLoadout
    {
        public uint ShipCRC;
        public float Health;
        public List<NetShipCargo> Items;
        public static NetShipLoadout Read(NetPacketReader message)
        {
            var s = new NetShipLoadout();
            s.ShipCRC = message.GetUInt();
            s.Health = message.GetFloat();
            var cargoCount = (int)message.GetVariableUInt32();
            s.Items = new List<NetShipCargo>(cargoCount);
            for (int i = 0; i < cargoCount; i++)
            {
                s.Items.Add(new NetShipCargo(message.GetInt(), message.GetUInt(), message.GetUInt(), message.GetByte(), message.GetInt()));
            }
            return s;
        }
        public void Put(NetDataWriter message)
        {
            message.Put(ShipCRC);
            message.Put(Health);
            message.PutVariableUInt32((uint) Items.Count);
            foreach (var c in Items)
            {
                message.Put(c.ID);
                message.Put(c.EquipCRC);
                message.Put(c.HardpointCRC);
                message.Put(c.Health);
                message.Put(c.Count);
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
        }
    }

    public class ObjectUpdatePacket : IPacket
    {
        public uint Tick;
        public int InputSequence;
        public PlayerAuthState PlayerState;
        public PackedShipUpdate[] Updates;
        public const int UpdateLimit = byte.MaxValue;
        public static object Read(NetPacketReader message)
        {
            var p = new ObjectUpdatePacket();
            p.Tick = message.GetUInt();
            p.InputSequence = message.GetInt();
            var pack = new BitReader(message.GetRemainingBytes(), 0);
            p.PlayerState = PlayerAuthState.Read(ref pack);
            var updateCount = pack.GetUInt(8);
            p.Updates = new PackedShipUpdate[updateCount];
            for(int i = 0; i < p.Updates.Length; i++)
                p.Updates[i] = PackedShipUpdate.ReadFrom(ref pack);
            return p;
        }
        public void WriteContents(NetDataWriter message)
        {
            message.Put(Tick);
            message.Put(InputSequence);
            var writer = new BitWriter();
            PlayerState.Write(ref writer);
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
        //Info
        public bool IsCRC;
        public bool HasPosition;
        public CruiseThrustState CruiseThrust; //4-5
        public bool Tradelane; //6
        public bool EngineKill; //7
        public bool DockingLights; //8
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
            message.PutBool(DockingLights);
            message.PutBool(Hull);
            message.PutBool(LinearVelocity != Vector3.Zero);
            message.PutBool(AngularVelocity != Vector3.Zero);
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
            if(LinearVelocity != Vector3.Zero)
                message.PutVector3(LinearVelocity);
            if(AngularVelocity != Vector3.Zero)
                message.PutVector3(AngularVelocity);
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
            p.DockingLights = message.GetBool();
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
            if (readLinear) p.LinearVelocity = message.GetVector3();
            if (readAngular) p.AngularVelocity = message.GetVector3();
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
    

    public class InputUpdatePacket : IPacket
    {
        public int Sequence;
        public Vector3 Steering;
        public StrafeControls Strafe;
        public float Throttle;
        public bool Cruise;
        public bool Thrust;

        public static object Read(NetPacketReader message)
        {
            return new InputUpdatePacket()
            {
                Sequence = message.GetInt(),
                Steering =  message.GetVector3(),
                Strafe = (StrafeControls)message.GetByte(),
                Throttle = message.GetFloat(),
                Cruise = message.GetBool(),
                Thrust = message.GetBool()
            };
        }
        public void WriteContents(NetDataWriter msg)
        {
            msg.Put(Sequence);
            msg.Put(Steering);
            msg.Put((byte)Strafe);
            msg.Put(Throttle);
            msg.Put(Cruise);
            msg.Put(Thrust);
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
