using System.Numerics;
using LiteNetLib;
using LiteNetLib.Utils;

namespace LibreLancer.Net
{
    public struct ProjectileSpawn
    {
        public uint Gun;
        public Vector3 Start;
        public Vector3 Heading;

        public static ProjectileSpawn Read(NetPacketReader message)
        {
            var p = new ProjectileSpawn();
            p.Gun = message.GetUInt();
            p.Start = message.GetVector3();
            p.Heading = message.GetVector3();
            return p;
        }

        public void Put(NetDataWriter message)
        {
            message.Put(Gun);
            message.Put(Start);
            message.Put(Heading);
        }
    }
}