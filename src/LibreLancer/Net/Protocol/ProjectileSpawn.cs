// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;
using LiteNetLib;
using LiteNetLib.Utils;

namespace LibreLancer.Net
{
    public struct ProjectileSpawn
    {
        public int Owner;
        public uint Gun;
        public uint Hardpoint;
        public Vector3 Start;
        public Vector3 Heading;

        public static ProjectileSpawn Read(NetPacketReader message)
        {
            var p = new ProjectileSpawn();
            p.Owner = message.GetVariableInt32();
            p.Gun = message.GetUInt();
            p.Hardpoint = message.GetUInt();
            p.Start = message.GetVector3();
            p.Heading = message.GetNormal();
            return p;
        }

        public void Put(NetDataWriter message)
        {
            message.PutVariableInt32(Owner);
            message.Put(Gun);
            message.Put(Hardpoint);
            message.Put(Start);
            message.PutNormal(Heading);
        }
    }
}