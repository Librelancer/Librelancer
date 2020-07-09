// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LiteNetLib;
namespace LibreLancer
{
    public enum PacketDeliveryMethod
    {
        ReliableOrdered,
        SequenceA,
        SequenceB,
        SequenceC
    }

    public static class PacketDeliveryMethodExt
    {
        public static void ToLiteNetLib(this PacketDeliveryMethod self, out DeliveryMethod method, out byte channel)
        {
            channel = 0;
            method = DeliveryMethod.ReliableOrdered;
            if (self == PacketDeliveryMethod.SequenceA)
            {
                method = DeliveryMethod.Sequenced;
                channel = 1;
            }
            if (self == PacketDeliveryMethod.SequenceB)
            {
                method = DeliveryMethod.Sequenced;
                channel = 2;
            }
            if (self == PacketDeliveryMethod.SequenceC)
            {
                method = DeliveryMethod.Sequenced;
                channel = 3;
            }
        }
    }
}