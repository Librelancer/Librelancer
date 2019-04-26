// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
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
            message.Write((byte)packetTypes.IndexOf(p.GetType()));
            p.WriteContents(message);
        }

        public static IPacket ReadPacket(this NetIncomingMessage message)
        {
            return (IPacket)parsers[message.ReadByte()](message);
        }

        static Packets()
        {
            Register<AuthenticationPacket>(AuthenticationPacket.Read);
            Register<AuthenticationReplyPacket>(AuthenticationReplyPacket.Read);
            Register<PositionUpdatePacket>(PositionUpdatePacket.Read);
            Register<SpawnObjectPacket>(SpawnObjectPacket.Read);
            Register<ObjectUpdatePacket>(ObjectUpdatePacket.Read);
            Register<DespawnObjectPacket>(DespawnObjectPacket.Read);
            Register<LaunchPacket>(LaunchPacket.Read);
            Register<SpawnPlayerPacket>(SpawnPlayerPacket.Read);
            Register<BaseEnterPacket>(BaseEnterPacket.Read);
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
        public static object Read(NetIncomingMessage message)
        {
            return new SpawnPlayerPacket()
            {
                System = message.ReadString(),
                Position = message.ReadVector3(),
                Orientation = message.ReadQuaternion()
            };
        }
        public void WriteContents(NetOutgoingMessage message)
        {
            message.Write(System);
            message.Write(Position);
            message.Write(Orientation);
        }
    }

    public class BaseEnterPacket : IPacket
    {
        public string Base;
        public static object Read(NetIncomingMessage message)
        {
            return new BaseEnterPacket() { Base = message.ReadString() };
        }
        public void WriteContents(NetOutgoingMessage message)
        {
            message.Write(Base);
        }
    }

    public class SpawnObjectPacket : IPacket
    {
        public int ID;
        public string Name;
        public Vector3 Position;
        public Quaternion Orientation;

        public static object Read(NetIncomingMessage message)
        {
            return new SpawnObjectPacket()
            {
                ID = message.ReadInt32(),
                Name = message.ReadString(),
                Position = message.ReadVector3(),
                Orientation = message.ReadQuaternion()
            };
        }
        public void WriteContents(NetOutgoingMessage message)
        {
            message.Write(ID);
            message.Write(Name);
            message.Write(Position);
            message.Write(Orientation);
        }
    }

    public class ObjectUpdatePacket : IPacket
    {
        public int ID;
        public Vector3 Position;
        public Quaternion Orientation;
        public static object Read(NetIncomingMessage message)
        {
            return new ObjectUpdatePacket()
            {
                ID = message.ReadInt32(),
                Position = message.ReadVector3(),
                Orientation = message.ReadQuaternion()
            };
        }
        public void WriteContents(NetOutgoingMessage message)
        {
            message.Write(ID);
            message.Write(Position);
            message.Write(Orientation);
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

}
