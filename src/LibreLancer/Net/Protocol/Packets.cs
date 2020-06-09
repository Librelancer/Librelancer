// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using Lidgren.Network;

namespace LibreLancer
{
    public interface IPacket
    {
        void WriteContents(NetOutgoingMessage msg);
    }
    public static class Packets
    {
        static List<Func<NetIncomingMessage, object>> parsers = new List<Func<NetIncomingMessage,object>>();
        static List<Type> packetTypes = new List<Type>();
        public static void Register<T>(Func<NetIncomingMessage,object> parser) where T : IPacket
        {
            packetTypes.Add(typeof(T));
            parsers.Add(parser);
        }

        public static void Write(this NetOutgoingMessage message, IPacket p)
        {
            message.WriteVariableUInt32((uint)packetTypes.IndexOf(p.GetType()));
            p.WriteContents(message);
        }

        public static IPacket ReadPacket(this NetIncomingMessage message)
        {
            return (IPacket)parsers[(int)message.ReadVariableUInt32()](message);
        }

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
            Register<ObjectUpdatePacket>(ObjectUpdatePacket.Read);
            Register<DespawnObjectPacket>(DespawnObjectPacket.Read);
            Register<CallThornPacket>(CallThornPacket.Read);
            //Chat
            Register<ConsoleCommandPacket>(ConsoleCommandPacket.Read);
            //Server->Client Generic Commands
            Register<PlaySoundPacket>(PlaySoundPacket.Read);
            Register<PlayMusicPacket>(PlayMusicPacket.Read);
            Register<UpdateRTCPacket>(UpdateRTCPacket.Read);
            //Client->Server Responses
            Register<EnterLocationPacket>(EnterLocationPacket.Read);
            Register<RTCCompletePacket>(RTCCompletePacket.Read);
        }
    }

    public class LoginSuccessPacket : IPacket
    {
        public static object Read(NetIncomingMessage message)
        {
            return new LoginSuccessPacket();
        }

        public void WriteContents(NetOutgoingMessage msg)
        {
        }
    }
    public class AuthenticationPacket : IPacket
    {
        public AuthenticationKind Type;
        public static AuthenticationPacket Read(NetIncomingMessage message)
        {
            return new AuthenticationPacket() { Type = (AuthenticationKind)message.ReadByte() };
        }
        public void WriteContents(NetOutgoingMessage message)
        {
            message.Write((byte)Type);
        }
    }

    public class AuthenticationReplyPacket : IPacket
    {
        public Guid Guid;
        public static AuthenticationReplyPacket Read(NetIncomingMessage message)
        {
            return new AuthenticationReplyPacket() { Guid = new Guid(message.ReadBytes(16)) };
        }
        public void WriteContents(NetOutgoingMessage message)
        {
            message.Write(Guid.ToByteArray());
        }
    }

    public class SpawnPlayerPacket : IPacket
    {
        public string System;
        public Vector3 Position;
        public Quaternion Orientation;
        public NetShipLoadout Ship;

        public static object Read(NetIncomingMessage message)
        {
            return new SpawnPlayerPacket()
            {
                System = message.ReadString(),
                Position = message.ReadVector3(),
                Orientation = message.ReadQuaternion(),
                Ship = NetShipLoadout.Read(message)
            };
        }
        public void WriteContents(NetOutgoingMessage message)
        {
            message.Write(System);
            message.Write(Position);
            message.Write(Orientation);
            Ship.Write(message);
        }
    }

    public class BaseEnterPacket : IPacket
    {
        public string Base;
        public NetShipLoadout Ship;
        public string[] RTCs;
        public static object Read(NetIncomingMessage message)
        {
            return new BaseEnterPacket()
            {
                Base = message.ReadString(),
                Ship = NetShipLoadout.Read(message), RTCs = message.ReadStringArray()
            };
        }
        public void WriteContents(NetOutgoingMessage message)
        {
            message.Write(Base);
            Ship.Write(message);
            message.Write(RTCs);
        }
    }

    public class EnterLocationPacket : IPacket
    {
        public string Base;
        public string Room;
        public static object Read(NetIncomingMessage message)
        {
            return new EnterLocationPacket() {Base = message.ReadString(), Room = message.ReadString()};
        }
        public void WriteContents(NetOutgoingMessage msg)
        {
            msg.Write(Base);
            msg.Write(Room);
        }
    }

    public class UpdateRTCPacket : IPacket
    {
        public string[] RTCs;
        public static object Read(NetIncomingMessage message) =>
            new UpdateRTCPacket() {RTCs = message.ReadStringArray()};
        public void WriteContents(NetOutgoingMessage msg) => msg.Write(RTCs);
    }

    public class RTCCompletePacket : IPacket
    {
        public string RTC;
        public static object Read(NetIncomingMessage message) => new RTCCompletePacket() {RTC = message.ReadString()};
        public void WriteContents(NetOutgoingMessage msg) => msg.Write(RTC);
    }

    public class SpawnObjectPacket : IPacket
    {
        public int ID;
        public string Name;
        public Vector3 Position;
        public Quaternion Orientation;
        public NetShipLoadout Loadout;

        public static object Read(NetIncomingMessage message)
        {
            return new SpawnObjectPacket()
            {
                ID = message.ReadInt32(),
                Name = message.ReadString(),
                Position = message.ReadVector3(),
                Orientation = message.ReadQuaternion(),
                Loadout = NetShipLoadout.Read(message)
            };
        }
        public void WriteContents(NetOutgoingMessage message)
        {
            message.Write(ID);
            message.Write(Name);
            message.Write(Position);
            message.Write(Orientation);
            Loadout.Write(message);
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
        public static NetShipLoadout Read(NetIncomingMessage message)
        {
            var s = new NetShipLoadout();
            s.ShipCRC = message.ReadUInt32();
            var equipCount = (int)message.ReadVariableUInt32();
            s.Equipment = new List<NetShipEquip>(equipCount);
            for(int i = 0; i < equipCount; i++) {
                s.Equipment.Add(new NetShipEquip(message.ReadUInt32(), message.ReadUInt32(), message.ReadByte()));
            }
            return s;
        }
        public void Write(NetOutgoingMessage message)
        {
            message.Write(ShipCRC);
            message.WriteVariableUInt32((uint)Equipment.Count);
            foreach(var equip in Equipment) {
                message.Write(equip.HardpointCRC);
                message.Write(equip.EquipCRC);
                message.Write(equip.Health);
            }
        }
    }

    public class ObjectUpdatePacket : IPacket
    {
        public PackedShipUpdate[] Updates;
        public static object Read(NetIncomingMessage message)
        {
            var p = new ObjectUpdatePacket();
            p.Updates = new PackedShipUpdate[message.ReadVariableUInt32()];
            for (int i = 0; i < p.Updates.Length; i++)
                p.Updates[i] = PackedShipUpdate.ReadFrom(message);
            return p;
        }
        public void WriteContents(NetOutgoingMessage message)
        {
            message.WriteVariableUInt32((uint)Updates.Length);
            foreach (var p in Updates)
                p.WriteTo(message);
        }
    }

    public struct GunOrient
    {
        public float AnglePitch;
        public float AngleRot;
        public void ReadIn(NetIncomingMessage message)
        {
            AnglePitch = message.ReadRadiansQuantized();
            AngleRot = message.ReadRadiansQuantized();
        }
        public void WriteTo(NetOutgoingMessage message)
        {
            //2 bytes each
            message.WriteRadiansQuantized(AnglePitch);
            message.WriteRadiansQuantized(AngleRot);
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

        public void WriteTo(NetOutgoingMessage message)
        {
            message.Write(ID);
            message.Write(Hidden);
            if(Hidden)
            {
                message.WritePadBits();
                return;
            }
            message.WriteRangedInteger(0, 3, (int)CruiseThrust); //2 bits
            message.Write(HasPosition);
            message.Write(HasOrientation);
            message.Write(HasHealth);
            message.Write(HasGuns);
            message.Write(DockingLights);
            if (HasPosition)
            {
                message.Write(Position);
                message.Write(EngineThrottlePct);
            }
            if(HasOrientation) 
                message.Write(Orientation);
            if (CruiseThrust == CruiseThrustState.CruiseCharging)
                message.Write(CruiseChargePct);
            if(HasHealth) 
            {
                message.Write(HasShield);
                message.Write(HasHull);
                message.Write(HasParts);
                if(HasParts)
                {
                    message.Write((byte)Parts.Length);
                    for(int i = 0; i < Parts.Length; i++) {
                        message.WriteRangedInteger(Parts[i], 0, 7); //3 bits
                    }
                    message.WritePadBits();
                }
                if (HasShield) message.Write(ShieldHp); //4 bytes
                if (HasHull) message.Write(HullHp); //4 bytes
            }
            if(HasGuns) 
            {
                message.Write((byte)GunOrients.Length);
                foreach (var g in GunOrients)
                    g.WriteTo(message);
            }
        }
        public static PackedShipUpdate ReadFrom(NetIncomingMessage message)
        {
            var p = new PackedShipUpdate();
            p.ID = message.ReadInt32();
            p.Hidden = message.ReadBoolean();
            if(p.Hidden)
            {
                message.ReadPadBits();
                return p;
            }
            p.CruiseThrust = (CruiseThrustState)message.ReadRangedInteger(0, 3);
            p.HasPosition = message.ReadBoolean();
            p.HasOrientation = message.ReadBoolean();
            p.HasHealth = message.ReadBoolean();
            p.HasGuns = message.ReadBoolean();
            p.DockingLights = message.ReadBoolean();
            if(p.HasPosition)
            {
                p.Position = message.ReadVector3();
                p.EngineThrottlePct = message.ReadByte();
            }
            if(p.HasOrientation)
                p.Orientation = message.ReadQuaternion();
            if (p.CruiseThrust == CruiseThrustState.CruiseCharging)
                p.CruiseChargePct = message.ReadByte();
            if(p.HasHealth)
            {
                p.HasShield = message.ReadBoolean();
                p.HasHull = message.ReadBoolean();
                p.HasParts = message.ReadBoolean();
                if(p.HasParts) {
                    p.Parts = new byte[message.ReadByte()];
                    for(int i = 0; i < p.Parts.Length; i++)
                    {
                        p.Parts[i] = (byte)message.ReadRangedInteger(0, 7); //3 bits
                    }
                    message.ReadPadBits();
                }
                if (p.HasShield) p.ShieldHp = message.ReadInt32();
                if (p.HasHull) p.HullHp = message.ReadInt32();
            }
            if (p.HasGuns)
            {
                p.GunOrients = new GunOrient[message.ReadByte()];
                for (int i = 0; i < p.GunOrients.Length; i++) {
                    p.GunOrients[i].ReadIn(message);
                }
            }
            return p;
        }
    }

    public class DespawnObjectPacket : IPacket
    {
        public int ID;
        public static object Read(NetIncomingMessage message) => new DespawnObjectPacket() { ID = message.ReadInt32() };
        public void WriteContents(NetOutgoingMessage message) => message.Write(ID);
    }

    public class LaunchPacket : IPacket
    {
        public static object Read(NetIncomingMessage message) => new LaunchPacket();
        public void WriteContents(NetOutgoingMessage message) { }
    }

    public class PositionUpdatePacket : IPacket
    {
        public Vector3 Position;
        public Quaternion Orientation;

        public static object Read(NetIncomingMessage message)
        {
            return new PositionUpdatePacket()
            {
                Position = message.ReadVector3(),
                Orientation = message.ReadQuaternion()
            };
        }
        public void WriteContents(NetOutgoingMessage msg)
        {
            msg.Write(Position);
            msg.Write(Orientation);
        }
    }

    public class PlaySoundPacket : IPacket
    {
        public string Sound;
        public static object Read(NetIncomingMessage message)
        {
            return new PlaySoundPacket() {Sound = message.ReadString()};
        }
        public void WriteContents(NetOutgoingMessage msg)
        {
            msg.Write(Sound);
        }
    }
    
    public class PlayMusicPacket : IPacket
    {
        public string Music;
        public static object Read(NetIncomingMessage message)
        {
            return new PlayMusicPacket() { Music = message.ReadString()};
        }
        public void WriteContents(NetOutgoingMessage msg)
        {
            msg.Write(Music);
        }
    }

    public class ConsoleCommandPacket : IPacket
    {
        public string Command;
        public static object Read(NetIncomingMessage message)
        {
            return new ConsoleCommandPacket() {Command = message.ReadString()};
        }
        public void WriteContents(NetOutgoingMessage msg)
        {
            msg.Write(Command);
        }
    }

    public class CallThornPacket : IPacket
    {
        public string Thorn;
        public static object Read(NetIncomingMessage message)
        {
            return new CallThornPacket() { Thorn = message.ReadString() };
        }
        public void WriteContents(NetOutgoingMessage msg)
        {
            msg.Write(Thorn);
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
        public static object Read(NetIncomingMessage message)
        {
            var pk = new MsnDialogPacket() { Lines = new NetDlgLine[message.ReadVariableInt32()] };
            for (int i = 0; i < pk.Lines.Length; i++)
            {
                pk.Lines[i] = new NetDlgLine() {
                    Voice = message.ReadString(),
                    Hash = message.ReadUInt32()
                };
            }

            return pk;
        }
        public void WriteContents(NetOutgoingMessage msg) {
            msg.WriteVariableInt32(Lines.Length);
            foreach (var ln in Lines)
            {
                msg.Write(ln.Voice);
                msg.Write(ln.Hash);
            }
        }
    }

    public class LineSpokenPacket : IPacket
    {
        public uint Hash;
        public static object Read(NetIncomingMessage message)
        {
            return new LineSpokenPacket() { Hash = message.ReadUInt32() };
        }
        public void WriteContents(NetOutgoingMessage msg)
        {
            msg.Write(Hash);
        }
    }
}
