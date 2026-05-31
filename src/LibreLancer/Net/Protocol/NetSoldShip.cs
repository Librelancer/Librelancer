// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Net.Protocol
{
    public struct NetSoldShip
    {
        public int ShipCRC;
        public int PackageCRC;
        public ulong HullPrice;
        public ulong PackagePrice;
        public int Rank;
        
        public void Put(PacketWriter message)
        {
            message.Put(ShipCRC);
            message.Put(PackageCRC);
            message.PutVariableUInt64(HullPrice);
            message.PutVariableUInt64(PackagePrice);
            message.PutVariableUInt32(Rank < 0 ? 0U : (uint) (Rank + 1));
        }

        public static NetSoldShip Read(PacketReader message) => new()
        {
            ShipCRC = message.GetInt(),
            PackageCRC = message.GetInt(),
            HullPrice = message.GetVariableUInt64(),
            PackagePrice = message.GetVariableUInt64(),
            Rank = ((int) message.GetVariableUInt32()) - 1
        };
    }
}
