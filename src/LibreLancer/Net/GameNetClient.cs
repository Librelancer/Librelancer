// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using LiteNetLib;
using LiteNetLib.Utils;

namespace LibreLancer
{
    public class GameNetClient : IPacketConnection
    {
        bool running = false;
        IUIThread mainThread;
        Thread networkThread;
        private NetManager client;
        public string AppIdentifier = LNetConst.DEFAULT_APP_IDENT;
        
        public event Action<LocalServerInfo> ServerFound;
        public event Action<string> AuthenticationRequired;
        public event Action<string> Disconnected;
        public Guid UUID;
        ConcurrentQueue<IPacket> packets = new ConcurrentQueue<IPacket>();
        
        public void Start()
        {
            if(running) throw new InvalidOperationException();
            running = true;
            networkThread = new Thread(NetworkThread);
            networkThread.Name = "NetClient";
            networkThread.Start();
        }

        public GameNetClient(IUIThread mainThread)
        {
            this.mainThread = mainThread;
        }

        public void Stop()
        {
            if(!running) throw new InvalidOperationException();
            running = false;
            networkThread.Join();
        }

        public bool Connected =>
            (client?.FirstPeer != null && client.FirstPeer.ConnectionState == ConnectionState.Connected);

        public void Shutdown()
        {
            if (running) Stop();
        }

        public void DiscoverLocalPeers()
        {
            if (running)
            {
                while (client == null || !client.IsRunning) Thread.Sleep(0);
                var dw = new NetDataWriter();
                dw.Put(LNetConst.BROADCAST_KEY);
                client.SendBroadcast(dw, LNetConst.DEFAULT_PORT);
            }
        }

        public void DiscoverGlobalPeers()
        {
            //HTTP?
        }

        bool connecting = true;
        public void Connect(IPEndPoint endPoint)
        {
            if(!running) throw new InvalidOperationException();
            lock (srvinfo) srvinfo.Clear();
            connecting = true;
            while (client == null || !client.IsRunning) Thread.Sleep(0);
            client.Connect(endPoint, AppIdentifier);
        }

        public bool Connect(string str)
        {
            IPEndPoint ep;
            if (ParseEP(str, out ep))
            {
                Connect(ep);
                return true;
            }
            else
                return false;
        }

        static bool ParseEP(string str, out IPEndPoint endpoint)
        {
            endpoint = new IPEndPoint(IPAddress.None, 0);
            IPAddress ip;
            if (IPAddress.TryParse(str, out ip))
            {
                endpoint = new IPEndPoint(ip, LNetConst.DEFAULT_PORT);
                return true;
            }
            if (str.Contains(":"))
            {
                var idxOf = str.LastIndexOf(':');
                var first = str.Remove(idxOf);
                var last = str.Substring(idxOf + 1);
                int portNum;
                if (!int.TryParse(last, out portNum))
                    return false;
                if (IPAddress.TryParse(first, out ip))
                {
                    endpoint = new IPEndPoint(ip, portNum);
                    return true;
                }
                else
                {
                    if (TryResolve(first, out ip))
                    {
                        endpoint = new IPEndPoint(ip, portNum);
                        return true;
                    }
                    return false;
                }
            }
            if (TryResolve(str, out ip))
            {
                endpoint = new IPEndPoint(ip, LNetConst.DEFAULT_PORT);
                return true;
            }
            return false;
        }

        static bool TryResolve(string str, out IPAddress addr)
        {
            addr = IPAddress.None;
            try
            {
                var addresses = Dns.GetHostAddresses(str);
                if (addresses.Length > 0)
                {
                    addr = addresses[0];
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        Stopwatch sw;
        List<LocalServerInfo> srvinfo = new List<LocalServerInfo>();

        void NetworkThread()
        {
            sw = Stopwatch.StartNew();
            var listener = new EventBasedNetListener();
            client = new NetManager(listener)
            {
                UnconnectedMessagesEnabled = true,
                IPv6Enabled = true,
                NatPunchEnabled = true
            };
            listener.NetworkReceiveUnconnectedEvent += (remote, msg, type) =>
            {
                if (type == UnconnectedMessageType.Broadcast) return;
                if (msg.GetInt() == 0) {
                    lock (srvinfo)
                    {
                        foreach (var info in srvinfo)
                        {
                            if (info.EndPoint.Equals(remote))
                            {
                                var t = sw.ElapsedMilliseconds;
                                info.Ping = (int)(t - info.LastPingTime);
                                if (info.Ping < 0) info.Ping = 0;
                            }
                        }
                    }
                }
                else if (ServerFound != null)
                {
                    var info = new LocalServerInfo();
                    info.EndPoint = remote;
                    info.Unique = msg.GetInt();
                    info.Name = msg.GetString();
                    info.Description = msg.GetString();
                    info.DataVersion = msg.GetString();
                    info.CurrentPlayers = msg.GetInt();
                    info.MaxPlayers = msg.GetInt();
                    info.LastPingTime = sw.ElapsedMilliseconds;
                    NetDataWriter writer = new NetDataWriter();
                    writer.Put(LNetConst.PING_MAGIC);
                    client.SendUnconnectedMessage(writer, remote);
                    lock (srvinfo)
                    {
                        bool add = true;
                        for (int i = 0; i < srvinfo.Count; i++)
                        {
                            if (srvinfo[i].Unique == info.Unique)
                            {
                                add = false;
                                //Prefer IPv6
                                if(srvinfo[i].EndPoint.AddressFamily != AddressFamily.InterNetwork &&
                                   info.EndPoint.AddressFamily == AddressFamily.InterNetwork)
                                    srvinfo[i].EndPoint = info.EndPoint;
                                break;
                            }
                        }
                        if (add) {
                            srvinfo.Add(info);
                            mainThread.QueueUIThread(() => ServerFound?.Invoke(info));
                        }
                    }
                }
                msg.Recycle();
            };
            listener.NetworkReceiveEvent += (peer, reader, method) =>
            {
                try
                {
                    var packetCount = reader.GetByte(); //reliable packets can be merged
                    if (packetCount > 1)
                        FLLog.Debug("Net", $"Received {packetCount} merged packets");
                    for (int i = 0; i < packetCount; i++)
                    {
                        var pkt = Packets.Read(reader);
                        if (connecting)
                        {
                            if (pkt is AuthenticationPacket)
                            {
                                var auth = (AuthenticationPacket) pkt;
                                FLLog.Info("Net", "Authentication Packet Received");
                                if (auth.Type == AuthenticationKind.Token)
                                {
                                    FLLog.Info("Net", "Token");
                                    var str = reader.GetString();
                                    mainThread.QueueUIThread(() => AuthenticationRequired(str));
                                }
                                else if (auth.Type == AuthenticationKind.GUID)
                                {
                                    FLLog.Info("Net", "GUID");
                                    SendPacket(new AuthenticationReplyPacket() {Guid = this.UUID},
                                        PacketDeliveryMethod.ReliableOrdered);
                                }
                            }
                            else if (pkt is LoginSuccessPacket)
                            {
                                FLLog.Info("Client", "Login success");
                                connecting = false;
                            }
                            else
                            {
                                client.DisconnectAll();
                            }
                        }
                        else
                        {
                            packets.Enqueue(pkt);
                        }
                    }
                }
                catch (Exception e)
                {
                    FLLog.Error("Client", "Error reading packet");
                    client.DisconnectAll();
                }
            };
            listener.PeerDisconnectedEvent += (peer, info) =>
            {
                mainThread.QueueUIThread(() => { Disconnected?.Invoke(info.Reason.ToString()); });
            };
            client.Start();
            while (running)
            {
                //ping servers
                lock (srvinfo)
                {
                    foreach (var inf in srvinfo)
                    {
                        var nowMs = sw.ElapsedMilliseconds;
                        if (nowMs - inf.LastPingTime > 2000) //ping every 2 seconds?
                        {
                            inf.LastPingTime = nowMs;
                            var om = new NetDataWriter();
                            om.Put(LNetConst.PING_MAGIC);
                            client.SendUnconnectedMessage(om, inf.EndPoint);
                        }
                    }
                }
                //events
                client.PollEvents();
                Thread.Sleep(1);
            }
            client.DisconnectAll();
            client.Stop();
        }

        public void SendPacket(IPacket packet, PacketDeliveryMethod method)
        {
            var om = new NetDataWriter();
            Packets.Write(om, packet);
            method.ToLiteNetLib(out DeliveryMethod mt, out byte ch);
            client.FirstPeer?.Send(om, ch, mt);
        }
        public bool PollPacket(out IPacket packet)
        {
            packet = null;
            if (!packets.IsEmpty)
            {
                return packets.TryDequeue(out packet);
            }
            return false;
        }
    }
}