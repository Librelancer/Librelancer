// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Net;
using LiteNetLib;
using LiteNetLib.Utils;

namespace LibreLancer
{
    public class RemotePacketClient : IPacketClient
    {
        public NetPeer Client;
        private NetHpidWriter hpids;
        Queue<IPacket> reliableSend = new Queue<IPacket>();
        object reliableLock = new object();
        public void SendPacket(IPacket packet, PacketDeliveryMethod method, bool force = false)
        {
            method.ToLiteNetLib(out DeliveryMethod mt, out byte ch);
            if (mt == DeliveryMethod.ReliableOrdered && !force)
            {
                lock (reliableLock)
                {
                    reliableSend.Enqueue(packet);
                }
            }
            else
            {
                var m = new PacketWriter(new NetDataWriter(), hpids);
                m.Put((byte)1);
                Packets.Write(m, packet);
                Client.Send(m, ch, mt);
            }
        }

        private double sendTime = 1 / 66.0;
        private double elapsed = 0.0;
        public void Update(double t)
        {
            elapsed -= t;
            if (elapsed <= 0.0)
            {
                elapsed = sendTime;
                lock (reliableSend)
                {
                    while (reliableSend.Count > 0)
                    {
                        var dw = new PacketWriter(new NetDataWriter(), hpids);
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
        

        public void SendPacketWithEvent(IPacket packet, Action onAck, PacketDeliveryMethod method)
        {
            var m = new PacketWriter();
            m.Put((byte) 1);
            Packets.Write(m, packet);
            method.ToLiteNetLib(out var mtd, out var channel);
            Client.SendWithDeliveryEvent(m, channel, mtd, onAck);
        }

        public RemotePacketClient(NetPeer client, NetHpidWriter hpids)
        {
            Client = client;
            this.hpids = hpids;
        }
    }
}