// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer.Net.Protocol
{
    public struct ProjectileSpawn
    {
        public ObjNetId Owner;
        public Vector3 Target;
        //1 bit set for each gun on owner that fired
        public ulong Guns;
        //1 bit set for each gun not firing at Target
        public ulong Unique;
        public Vector3[] OtherTargets;

        public static ProjectileSpawn Read(PacketReader message)
        {
            var p = new ProjectileSpawn();
            p.Owner = ObjNetId.Read(message);
            p.Target = message.GetVector3();
            p.Guns = message.GetVariableUInt64();
            p.Unique = message.GetVariableUInt64();
            p.OtherTargets = new Vector3[BitOperations.PopCount(p.Unique)];
            for (int i = 0; i < p.OtherTargets.Length; i++)
                p.OtherTargets[i] = message.GetVector3();
            return p;
        }

        public void Put(PacketWriter message)
        {
            Owner.Put(message);
            message.Put(Target);
            message.PutVariableUInt64(Guns);
            message.PutVariableUInt64(Unique);
            if (BitOperations.PopCount(Unique) != OtherTargets.Length)
                throw new InvalidOperationException("Unique popcnt must match OtherTargets");
            for(int i = 0; i < OtherTargets.Length; i++)
                message.Put(OtherTargets[i]);
        }
    }
}
