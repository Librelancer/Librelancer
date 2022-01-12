// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LiteNetLib;
using LiteNetLib.Utils;

namespace LibreLancer.Net
{
    public struct NetSoldShip
    {
        public int ShipCRC;
        public int PackageCRC;
        public ulong HullPrice;
        public ulong PackagePrice;
        
        public void Put(NetDataWriter message)
        {
            message.Put(ShipCRC);
            message.Put(PackageCRC);
            message.PutVariableUInt64(HullPrice);
            message.PutVariableUInt64(PackagePrice);
        }

        public static NetSoldShip Read(NetPacketReader message) => new()
        {
            ShipCRC = message.GetInt(),
            PackageCRC = message.GetInt(),
            HullPrice = message.GetVariableUInt64(),
            PackagePrice = message.GetVariableUInt64()
        };
    }
}