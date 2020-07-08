// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;

namespace LibreLancer
{
    public class RemotePacketClient : IPacketClient
    {
        public NetPeer Client;
        Queue<IPacket> reliableSend = new Queue<IPacket>();
        object reliableLock = new object();
        public void SendPacket(IPacket packet, DeliveryMethod method)
        {
            if (method == DeliveryMethod.ReliableOrdered)
            {
                lock (reliableLock)
                {
                    reliableSend.Enqueue(packet);
                }
            }
            else
            {
                var m = new NetDataWriter();
                m.Put((byte)1);
                Packets.Write(m, packet);
                Client.Send(m, method);
            }
        }

        private TimeSpan sendTime = TimeSpan.FromSeconds(1 / 66.0);
        private TimeSpan elapsed = TimeSpan.Zero;
        public void Update(TimeSpan t)
        {
            elapsed -= t;
            if (elapsed <= TimeSpan.Zero)
            {
                elapsed = sendTime;
                lock (reliableSend)
                {
                    while (reliableSend.Count > 0)
                    {
                        var dw = new NetDataWriter();
                        if(reliableSend.Count > 255)
                            dw.Put((byte)255);
                        else
                            dw.Put((byte)reliableSend.Count);
                        for (int i = 0; (i < 255 && reliableSend.Count > 0); i++)
                        {
                            Packets.Write(dw, reliableSend.Dequeue());
                        }
                        Client.Send(dw, DeliveryMethod.ReliableOrdered);
                    }
                }
            }
        }
        

        public void SendPacketWithEvent(IPacket packet, Action onAck, DeliveryMethod method)
        {
            if(method == DeliveryMethod.Unreliable) throw new ArgumentException();
            var m = new NetDataWriter();
            m.Put((byte) 1);
            Packets.Write(m, packet);
            Client.SendWithDeliveryEvent(m, 0, method, onAck);
        }

        public RemotePacketClient(NetPeer client)
        {
            Client = client;
        }
    }
}