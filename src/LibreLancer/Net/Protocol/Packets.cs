﻿// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using LiteNetLib;
using LiteNetLib.Utils;

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
            Register<CharacterListActionPacket>(CharacterListActionPacket.Read);
            Register<CharacterListActionResponsePacket>(CharacterListActionResponsePacket.Read);
            Register<AddCharacterPacket>(AddCharacterPacket.Read);
            //Scene
            Register<SpawnPlayerPacket>(SpawnPlayerPacket.Read);
            Register<BaseEnterPacket>(BaseEnterPacket.Read);
            //Base-side
            Register<LaunchPacket>(LaunchPacket.Read);
            //Space
            Register<PositionUpdatePacket>(PositionUpdatePacket.Read);
            Register<SpawnObjectPacket>(SpawnObjectPacket.Read);
            Register<SpawnDebrisPacket>(SpawnDebrisPacket.Read);
            Register<ObjectUpdatePacket>(ObjectUpdatePacket.Read);
            Register<SpawnSolarPacket>(SpawnSolarPacket.Read);
            Register<DestroyPartPacket>(DestroyPartPacket.Read);
            Register<DespawnObjectPacket>(DespawnObjectPacket.Read);
            Register<CallThornPacket>(CallThornPacket.Read);
            //Chat
            Register<ConsoleCommandPacket>(ConsoleCommandPacket.Read);
            //Server->Client Generic Commands
            Register<PlaySoundPacket>(PlaySoundPacket.Read);
            Register<PlayMusicPacket>(PlayMusicPacket.Read);
            Register<UpdateRTCPacket>(UpdateRTCPacket.Read);
            Register<MsnDialogPacket>(MsnDialogPacket.Read);
            //Client->Server Responses
            Register<LineSpokenPacket>(LineSpokenPacket.Read);
            Register<EnterLocationPacket>(EnterLocationPacket.Read);
            Register<RTCCompletePacket>(RTCCompletePacket.Read);
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

    public class SpawnPlayerPacket : IPacket
    {
        public string System;
        public Vector3 Position;
        public Quaternion Orientation;
        public NetShipLoadout Ship;

        public static object Read(NetPacketReader message)
        {
            return new SpawnPlayerPacket()
            {
                System = message.GetString(),
                Position = message.GetVector3(),
                Orientation = message.GetQuaternion(),
                Ship = NetShipLoadout.Read(message)
            };
        }
        public void WriteContents(NetDataWriter message)
        {
            message.Put(System);
            message.Put(Position);
            message.Put(Orientation);
            Ship.Put(message);
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
        public void Write(NetDataWriter message)
        {
            message.Put(ID);
            message.Put(Archetype);
            message.Put(Position);
            message.Put(Orientation);
        }
    }
    public class SpawnSolarPacket : IPacket
    {
        public List<SolarInfo> Solars;
        public static object Read(NetPacketReader message)
        {
            var count = message.GetVariableUInt32();
            var solars = new List<SolarInfo>((int)count);
            for (int i = 0; i < count; i++)
                solars.Add(SolarInfo.Read(message));
            return new SpawnSolarPacket() {Solars = solars};
        }
        public void WriteContents(NetDataWriter message)
        {
            message.PutVariableUInt32((uint)Solars.Count);
            foreach (var si in Solars) si.Write(message);
        }
    }

    public class BaseEnterPacket : IPacket
    {
        public string Base;
        public NetShipLoadout Ship;
        public string[] RTCs;
        public static object Read(NetPacketReader message)
        {
            return new BaseEnterPacket()
            {
                Base = message.GetString(),
                Ship = NetShipLoadout.Read(message), RTCs = message.GetStringArray()
            };
        }
        public void WriteContents(NetDataWriter message)
        {
            message.Put(Base);
            Ship.Put(message);
            message.PutArray(RTCs);
        }
    }

    public class EnterLocationPacket : IPacket
    {
        public string Base;
        public string Room;
        public static object Read(NetPacketReader message)
        {
            return new EnterLocationPacket() {Base = message.GetString(), Room = message.GetString()};
        }
        public void WriteContents(NetDataWriter msg)
        {
            msg.Put(Base);
            msg.Put(Room);
        }
    }

    public class UpdateRTCPacket : IPacket
    {
        public string[] RTCs;
        public static object Read(NetPacketReader message) =>
            new UpdateRTCPacket() {RTCs = message.GetStringArray()};
        public void WriteContents(NetDataWriter msg) => msg.PutArray(RTCs);
    }

    public class RTCCompletePacket : IPacket
    {
        public string RTC;
        public static object Read(NetPacketReader message) => new RTCCompletePacket() {RTC = message.GetString()};
        public void WriteContents(NetDataWriter msg) => msg.Put(RTC);
    }

    public class DestroyPartPacket : IPacket
    {
        public byte IDType;
        public int ID;
        public string PartName;

        public static object Read(NetPacketReader message)
        {
            return new DestroyPartPacket()
            {
                IDType = message.GetByte(),
                ID = message.GetInt(),
                PartName = message.GetString()
            };
        }

        public void WriteContents(NetDataWriter message)
        {
            message.Put(IDType);
            message.Put(ID);
            message.Put(PartName);
        }
    }

    public class SpawnDebrisPacket : IPacket
    {
        public int ID;
        public string Archetype;
        public string Part;
        public Vector3 Position;
        public Quaternion Orientation;
        public float Mass;

        public static object Read(NetPacketReader message)
        {
            return new SpawnDebrisPacket()
            {
                ID = message.GetInt(),
                Archetype = message.GetString(),
                Part = message.GetString(),
                Position = message.GetVector3(),
                Orientation = message.GetQuaternion(),
                Mass = message.GetFloat()
            };
        }

        public void WriteContents(NetDataWriter message)
        {
            message.Put(ID);
            message.Put(Archetype);
            message.Put(Part);
            message.Put(Position);
            message.Put(Orientation);
            message.Put(Mass);
        }
    }
    public class SpawnObjectPacket : IPacket
    {
        public int ID;
        public string Name;
        public Vector3 Position;
        public Quaternion Orientation;
        public NetShipLoadout Loadout;

        public static object Read(NetPacketReader message)
        {
            return new SpawnObjectPacket()
            {
                ID = message.GetInt(),
                Name = message.GetString(),
                Position = message.GetVector3(),
                Orientation = message.GetQuaternion(),
                Loadout = NetShipLoadout.Read(message)
            };
        }
        public void WriteContents(NetDataWriter message)
        {
            message.Put(ID);
            message.Put(Name);
            message.Put(Position);
            message.Put(Orientation);
            Loadout.Put(message);
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

    public class NetShipLoadout
    {
        public uint ShipCRC;
        public List<NetShipEquip> Equipment;
        public static NetShipLoadout Read(NetPacketReader message)
        {
            var s = new NetShipLoadout();
            s.ShipCRC = message.GetUInt();
            var equipCount = (int)message.GetVariableUInt32();
            s.Equipment = new List<NetShipEquip>(equipCount);
            for(int i = 0; i < equipCount; i++) {
                s.Equipment.Add(new NetShipEquip(message.GetUInt(), message.GetUInt(), message.GetByte()));
            }
            return s;
        }
        public void Put(NetDataWriter message)
        {
            message.Put(ShipCRC);
            message.PutVariableUInt32((uint)Equipment.Count);
            foreach(var equip in Equipment) {
                message.Put(equip.HardpointCRC);
                message.Put(equip.EquipCRC);
                message.Put(equip.Health);
            }
        }
    }

    public class ObjectUpdatePacket : IPacket
    {
        public uint Tick;
        public PackedShipUpdate[] Updates;
        public const int UpdateLimit = byte.MaxValue;
        public static object Read(NetPacketReader message)
        {
            var p = new ObjectUpdatePacket();
            p.Tick = message.GetUInt();
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
        public float AnglePitch;
        public float AngleRot;
        public void ReadIn(ref BitReader message)
        {
            AnglePitch = message.GetRadiansQuantized();
            AngleRot = message.GetRadiansQuantized();
        }
        public void WriteTo(ref BitWriter message)
        {
            //2 bytes each
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
        //Cruise Charge Pct (CruiseCharging == 2)
        public byte CruiseChargePct;
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
            if (CruiseThrust == CruiseThrustState.CruiseCharging)
                message.PutByte(CruiseChargePct);
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
            if (p.CruiseThrust == CruiseThrustState.CruiseCharging)
                p.CruiseChargePct = message.GetByte();
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

    public class DespawnObjectPacket : IPacket
    {
        public int ID;
        public static object Read(NetPacketReader message) => new DespawnObjectPacket() { ID = message.GetInt() };
        public void WriteContents(NetDataWriter message) => message.Put(ID);
    }

    public class LaunchPacket : IPacket
    {
        public static object Read(NetPacketReader message) => new LaunchPacket();
        public void WriteContents(NetDataWriter message) { }
    }

    public class PositionUpdatePacket : IPacket
    {
        public Vector3 Position;
        public Quaternion Orientation;

        public static object Read(NetPacketReader message)
        {
            return new PositionUpdatePacket()
            {
                Position = message.GetVector3(),
                Orientation = message.GetQuaternion()
            };
        }
        public void WriteContents(NetDataWriter msg)
        {
            msg.Put(Position);
            msg.Put(Orientation);
        }
    }

    public class PlaySoundPacket : IPacket
    {
        public string Sound;
        public static object Read(NetPacketReader message)
        {
            return new PlaySoundPacket() {Sound = message.GetString()};
        }
        public void WriteContents(NetDataWriter msg)
        {
            msg.Put(Sound);
        }
    }
    
    public class PlayMusicPacket : IPacket
    {
        public string Music;
        public static object Read(NetPacketReader message)
        {
            return new PlayMusicPacket() { Music = message.GetString()};
        }
        public void WriteContents(NetDataWriter msg)
        {
            msg.Put(Music);
        }
    }

    public class ConsoleCommandPacket : IPacket
    {
        public string Command;
        public static object Read(NetPacketReader message)
        {
            return new ConsoleCommandPacket() {Command = message.GetString()};
        }
        public void WriteContents(NetDataWriter msg)
        {
            msg.Put(Command);
        }
    }

    public class CallThornPacket : IPacket
    {
        public string Thorn;
        public static object Read(NetPacketReader message)
        {
            return new CallThornPacket() { Thorn = message.GetString() };
        }
        public void WriteContents(NetDataWriter msg)
        {
            msg.Put(Thorn);
        }
    }

    public class NetDlgLine
    {
        public string Voice;
        public uint Hash;
    }
    
    public class MsnDialogPacket : IPacket
    {
        public NetDlgLine[] Lines;
        public static object Read(NetPacketReader message)
        {
            var pk = new MsnDialogPacket() { Lines = new NetDlgLine[(int)message.GetVariableUInt32()] };
            for (int i = 0; i < pk.Lines.Length; i++)
            {
                pk.Lines[i] = new NetDlgLine() {
                    Voice = message.GetString(),
                    Hash = message.GetUInt()
                };
            }

            return pk;
        }
        public void WriteContents(NetDataWriter msg) {
            msg.PutVariableUInt32((uint)Lines.Length);
            foreach (var ln in Lines)
            {
                msg.Put(ln.Voice);
                msg.Put(ln.Hash);
            }
        }
    }

    public class LineSpokenPacket : IPacket
    {
        public uint Hash;
        public static object Read(NetPacketReader message)
        {
            return new LineSpokenPacket() { Hash = message.GetUInt() };
        }
        public void WriteContents(NetDataWriter msg)
        {
            msg.Put(Hash);
        }
    }
}
