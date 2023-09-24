// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Net.Protocol
{
    public class SpawnedEffect
    {
        public uint ID;
        public string Effect;
        public string[] Hardpoints;
        public void Put(PacketWriter message)
        {
            message.PutVariableUInt32(ID);
            message.Put(Effect);
            message.PutVariableUInt32((uint)Hardpoints.Length);
            foreach(var hp in Hardpoints)
                message.Put(hp);
        }

        public static SpawnedEffect Read(PacketReader message)
        {
            var x = new SpawnedEffect()
            {
                ID = message.GetVariableUInt32(),
                Effect = message.GetString()
            };
            x.Hardpoints = new string[message.GetVariableUInt32()];
            for (int i = 0; i < x.Hardpoints.Length; i++)
                x.Hardpoints[i] = message.GetString();
            return x;
        }
    }
}
