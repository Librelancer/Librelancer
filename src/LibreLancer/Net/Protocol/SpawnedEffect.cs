// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Net;
using LiteNetLib;
using LiteNetLib.Utils;

namespace LibreLancer
{
    public class SpawnedEffect
    {
        public uint ID;
        public string Effect;
        public string[] Hardpoints;
        public void Put(NetDataWriter message)
        {
            message.PutVariableUInt32(ID);
            message.PutStringPacked(Effect);
            message.PutVariableUInt32((uint)Hardpoints.Length);
            foreach(var hp in Hardpoints)
                message.PutStringPacked(hp);
        }

        public static SpawnedEffect Read(NetPacketReader message)
        {
            var x = new SpawnedEffect()
            {
                ID = message.GetVariableUInt32(),
                Effect= message.GetStringPacked()
            };
            x.Hardpoints = new string[message.GetVariableUInt32()];
            for (int i = 0; i < x.Hardpoints.Length; i++)
                x.Hardpoints[i] = message.GetStringPacked();
            return x;
        }
    }
}