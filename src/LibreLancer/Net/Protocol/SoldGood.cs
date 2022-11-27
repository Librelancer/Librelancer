// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Net.Protocol
{
    public struct SoldGood
    {
        //Name of Good (CRC)
        public uint GoodCRC;
        //Required player rank
        public int Rank;
        //Required reputation [-1,1]
        public float Rep;
        //Price
        public ulong Price;
        //For Sale
        public bool ForSale;
        
        public void Put(PacketWriter message)
        {
            message.Put(GoodCRC);
            message.Put((short)(Rep * 32767f));
            message.PutVariableUInt32(Rank < 0 ? 0U : (uint)(Rank + 1));
            message.PutVariableUInt64(Price);
            message.Put(ForSale);
        }

        public static SoldGood Read(PacketReader message) => new()
        {
            GoodCRC = message.GetUInt(),
            Rep = message.GetShort() / 32767f,
            Rank = ((int)message.GetVariableUInt32()) - 1,
            Price = message.GetVariableUInt64(),
            ForSale = message.GetBool()
        };
    }

    public struct BaselinePrice
    {
        //Name of Good (CRC)
        public uint GoodCRC;
        //Price
        public ulong Price;
        
        public void Put(PacketWriter message)
        {
            message.Put(GoodCRC);
            message.PutVariableUInt64(Price);
        }

        public static BaselinePrice Read(PacketReader message) => new()
        {
            GoodCRC = message.GetUInt(),
            Price = message.GetVariableUInt64()
        };
    }
}