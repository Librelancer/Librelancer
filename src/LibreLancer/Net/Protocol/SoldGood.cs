// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Linq;
using LiteNetLib.Utils;

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


    public struct BaselinePriceBundle
    {
        public BaselinePrice[] Prices;
        public byte[] Compressed;

        public static BaselinePriceBundle Compress(BaselinePrice[] prices)
        {
            var writer = new PacketWriter();
            var p2 = prices.OrderBy(x => x.GoodCRC).ToArray();
            writer.PutBigVarUInt32((uint)p2.Length);
            writer.Put(p2[0].GoodCRC);
            for (var i = 1; i < p2.Length; i++)
                writer.PutBigVarUInt32(p2[i].GoodCRC - p2[i - 1].GoodCRC);
            for (int i = 0; i < p2.Length; i++)
                writer.PutVariableUInt64(p2[i].Price);
            using var comp = new ZstdSharp.Compressor(19);
            return new BaselinePriceBundle() { Compressed = comp.Wrap(writer.GetCopy()).ToArray() };
        }

        public void Put(PacketWriter message)
        {
            message.Put(Compressed,0,Compressed.Length);
        }

        public static BaselinePriceBundle Read(PacketReader message)
        {
            var compressed = message.GetRemainingBytes();
            using var comp = new ZstdSharp.Decompressor();
            var reader = new PacketReader(new NetDataReader(comp.Unwrap(compressed).ToArray()));
            var bp = new BaselinePriceBundle();
            bp.Prices = new BaselinePrice[reader.GetBigVarUInt32()];
            bp.Prices[0].GoodCRC = reader.GetUInt();
            for (int i = 1; i < bp.Prices.Length; i++)
                bp.Prices[i].GoodCRC = (uint)(reader.GetBigVarUInt32() + bp.Prices[i - 1].GoodCRC);
            for (int i = 0; i < bp.Prices.Length; i++)
                bp.Prices[i].Price = reader.GetVariableUInt64();
            return bp;
        }
    }

    public struct BaselinePrice
    {
        //Name of Good (CRC)
        public uint GoodCRC;
        //Price
        public ulong Price;
    }
}
