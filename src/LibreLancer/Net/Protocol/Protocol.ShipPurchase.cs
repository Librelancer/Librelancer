// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code packa

namespace LibreLancer.Net.Protocol
{

    public enum ShipPurchaseStatus
    {
        Fail,
        Success,
        SuccessGainCredits
    }

    public struct SellCount
    {
        public int ID;
        public int Count;
        public static SellCount Read(PacketReader message) => new()
        {
            ID = message.GetInt(),
            Count = (int)message.GetVariableUInt32()
        };

        public void Put(PacketWriter message)
        {
            message.Put(ID);
            message.PutVariableUInt32((uint)Count);
        }
    }
    public struct MountId
    {
        public int ID;
        public string Hardpoint;

        public static MountId Read(PacketReader message) => new()
        {
            ID = message.GetInt(),
            Hardpoint = message.GetString()
        };

        public void Put(PacketWriter message)
        {
            message.Put(ID);
            message.Put(Hardpoint);
        }
    }
    
    public struct IncludedGood
    {
        public uint EquipCRC;
        public string Hardpoint;
        public int Amount;
        public static IncludedGood Read(PacketReader message)
        {
            var ic = new IncludedGood();
            ic.EquipCRC = message.GetUInt();
            ic.Hardpoint = message.GetHpid();
            ic.Amount = (int)message.GetVariableUInt32();
            return ic;
        }

        public void Put(PacketWriter message)
        {
            message.Put(EquipCRC);
            message.PutHpid(Hardpoint);
            message.PutVariableUInt32((uint)Amount);
        }
        
    }
    
    public class ShipPackageInfo
    {
        public IncludedGood[] Included;
        public static ShipPackageInfo Read(PacketReader message)
        {
            var p = new ShipPackageInfo();
            var inclen = message.GetVariableUInt32();
            if (inclen > 0) {
                p.Included = new IncludedGood[inclen - 1];
                for(int i = 0; i < p.Included.Length; i++) p.Included[i] = IncludedGood.Read(message);
            }
            return p;
        }

        public void Put(PacketWriter message)
        {
            if (Included != null) 
            {
                message.PutVariableUInt32((uint)(Included.Length + 1));
                foreach(var inc in Included) inc.Put(message);
            }
            else
            {
                message.PutVariableUInt32(0);
            }
        }
    }
}