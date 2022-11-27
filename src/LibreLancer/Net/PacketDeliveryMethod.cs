// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LiteNetLib;

namespace LibreLancer.Net
{
    public enum PacketDeliveryMethod
    {
        ReliableOrdered,
        ReliableOrderedB,
        ReliableOrderedC,
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
            switch (self)
            {
                case PacketDeliveryMethod.SequenceA:
                    method = DeliveryMethod.Sequenced;
                    break;
                case PacketDeliveryMethod.SequenceB:
                    method = DeliveryMethod.Sequenced;
                    channel = 1;
                    break;
                case PacketDeliveryMethod.SequenceC:
                    method = DeliveryMethod.Unreliable;
                    channel = 2;
                    break;
                case PacketDeliveryMethod.ReliableOrderedB:
                    channel = 1;
                    break;
                case PacketDeliveryMethod.ReliableOrderedC:
                    channel = 2;
                    break;
            }
        }
    }
}