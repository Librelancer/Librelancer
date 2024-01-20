// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using LibreLancer.Net.Protocol;
using LibreLancer.Net.Protocol.RpcPackets;
using LiteNetLib;
using LiteNetLib.Utils;

namespace LibreLancer.Net
{
    public class GameNetClient : IPacketConnection
    {
        bool running = false;
        IUIThread mainThread;
        Thread networkThread;
        private NetManager client;
        public string AppIdentifier = LNetConst.DEFAULT_APP_IDENT;

        public event Action<LocalServerInfo> ServerFound;
        public event Action<bool> AuthenticationRequired;
        public event Action<DisconnectReason> Disconnected;
        public Guid UUID;
        ConcurrentQueue<IPacket> packets = new ConcurrentQueue<IPacket>();
        private HttpClient http;
        private NetHpidWriter hpidWrite;

        public int LossPercent
        {
            get
            {
                if (running)
                    return (int) (client?.FirstPeer?.Statistics?.PacketLossPercent ?? 100);
                return -1;
            }
        }

        public int Ping
        {
            get
            {
                if (running)
                    //LiteNetLib returns Ping as RTT/2 - not the regular measure of ping.
                    return (client?.FirstPeer?.Ping ?? 0) * 2;
                return -1;
            }
        }

        public int BytesSent
        {
            get
            {
                if (running)
                    return (int) (client?.Statistics?.BytesSent ?? 0);
                return 0;
            }
        }

        public int BytesReceived
        {
            get
            {
                if (running)
                    return (int) (client?.Statistics?.BytesReceived ?? 0);
                return 0;
            }
        }

        public uint EstimateTickDelay()
        {
            const float TickMs = (1 / 60.0f) * 1000.0f;
            var ticks = (int) Math.Ceiling(Ping / TickMs);
            if (ticks < 2) return 2;
            return (uint)ticks;
        }

        public void Start()
        {
            if(running) throw new InvalidOperationException();
            running = true;
            networkThread?.Join();
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
        }

        public bool Connected =>
            (client?.FirstPeer != null && client.FirstPeer.ConnectionState == ConnectionState.Connected);

        private long localPeerRequests;
        public void DiscoverLocalPeers()
        {
            if (running)
            {
                Interlocked.Increment(ref localPeerRequests);
            }
        }

        public void DiscoverGlobalPeers()
        {
            //HTTP?
        }

        bool connecting = true;
        public void Connect(IPEndPoint endPoint)
        {
            ConnectInternal(endPoint, null);
        }

        void ConnectInternal(IPEndPoint endPoint, string token)
        {
            hpidWrite = new NetHpidWriter();
            hpidWrite.OnAddString += s => {
                SendPacket(new AddStringPacket() { ToAdd = s }, PacketDeliveryMethod.ReliableOrdered);
            };
            var dw = new PacketWriter();
            dw.Put(AppIdentifier + GeneratedProtocol.PROTOCOL_HASH);
            if(!string.IsNullOrEmpty(token))
                dw.Put(token);
            if(!running) throw new InvalidOperationException();
            lock (srvinfo) srvinfo.Clear();
            connecting = true;
            loginServer = null;
            while (client == null || !client.IsRunning) Thread.Sleep(0);
            client.Statistics?.Reset();
            client.Connect(endPoint, dw);
        }

        public void Connect(string str)
        {
            Task.Run(() =>
            {
                IPEndPoint ep;
                if (ParseEP(str, out ep))
                {
                    Connect(ep);
                }
                else {
                    mainThread.QueueUIThread(() => { Disconnected?.Invoke(DisconnectReason.InvalidEndpoint); });
                }
            });
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

        private AuthInfo loginServer;
        private IPEndPoint loginEndpoint;
        public void Login(string username, string password)
        {
            if (!running || loginServer == null) throw new InvalidOperationException();
            Task.Run(async () =>
            {
                var token = await http.Login(loginServer, username, password);
                if (token != null) {
                    ConnectInternal(loginEndpoint, token);
                }
                else
                {
                    FLLog.Error("Http", "Login failed");
                    mainThread.QueueUIThread(() => AuthenticationRequired?.Invoke(true));
                }
            });
        }

        private NetHpidReader hpidReader;

        public NetHpidReader HpidReader => hpidReader;

        void NetworkThread()
        {
            sw = Stopwatch.StartNew();
            http = new HttpClient();
            var listener = new EventBasedNetListener();
            hpidReader = new NetHpidReader();
            client = new NetManager(listener)
            {
                UnconnectedMessagesEnabled = true,
                IPv6Enabled = true,
                NatPunchEnabled = true,
                EnableStatistics = true,
                ChannelsCount =  3
            };
            listener.NetworkReceiveUnconnectedEvent += (remote, npr, type) =>
            {
                if (type == UnconnectedMessageType.Broadcast) return;
                var msg = new PacketReader(npr);
                if (msg.GetByte() == 0) {
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
                    info.Unique = msg.GetGuid();
                    info.EndPoint.Port = (int)msg.GetVariableUInt32();
                    info.Name = msg.GetString();
                    info.Description = msg.GetString();
                    info.DataVersion = msg.GetString();
                    info.CurrentPlayers = (int)msg.GetVariableUInt32();
                    info.MaxPlayers = (int)msg.GetVariableUInt32();
                    info.LastPingTime = sw.ElapsedMilliseconds;
                    PacketWriter writer = new PacketWriter();
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
                npr.Recycle();
            };
            listener.NetworkReceiveEvent += (peer, msg, channel, method) =>
            {
#if !DEBUG
                try
                {
#endif
                    var reader = new PacketReader(msg, hpidReader);
                    var pkt = Packets.Read(reader);
                    if (pkt is SetStringsPacket strs) {
                        hpidReader.SetStrings(strs.Data);
                    }
                    else if (pkt is AddStringPacket add) {
                        hpidReader.AddString(add.ToAdd);
                    }
                    else if (connecting)
                    {
                        if (pkt is GuidAuthenticationPacket)
                        {
                            var auth = (GuidAuthenticationPacket) pkt;
                            FLLog.Info("Net", "GUID Request Received");
                            SendPacket(new AuthenticationReplyPacket() {Guid = this.UUID},
                                PacketDeliveryMethod.ReliableOrdered);
                        }
                        else if (pkt is LoginSuccessPacket)
                        {
                            FLLog.Info("Client", "Login success");
                            SendPacket(new SetStringsPacket() { Data = hpidWrite.GetData() }, PacketDeliveryMethod.ReliableOrdered);
                            connecting = false;
                        }
                        else
                        {
                            FLLog.Info("Net", $"Invalid login packet {pkt.GetType()}");
                            client.DisconnectAll();
                        }
                    }
                    else
                    {
                        packets.Enqueue(pkt);
                    }
#if !DEBUG
                }

                catch (Exception e)
                {
                    FLLog.Error("Client", "Error reading packet");
                    client.DisconnectAll();
                }
#endif
            };
            listener.PeerDisconnectedEvent += (peer, info) =>
            {
                var additional = new PacketReader(info.AdditionalData);
                if (additional.TryGetDisconnectReason(out var reason) &&
                    connecting &&
                    reason == DisconnectReason.TokenRequired &&
                    additional.TryGetString(out var url))
                {
                    loginEndpoint = new IPEndPoint(peer.Address, peer.Port);
                    Task.Run(async () =>
                    {
                        loginServer = await http.LoginServerInfo(url);
                        if (loginServer != null)
                        {
                            mainThread.QueueUIThread(() => AuthenticationRequired?.Invoke(false));
                        }
                        else
                        {
                            FLLog.Error("Net", "Remote disconnected with invalid login server");
                            mainThread.QueueUIThread(() => { Disconnected?.Invoke(DisconnectReason.ConnectionError); });
                        }
                    });
                }
                else
                {
                    mainThread.QueueUIThread(() =>
                    {
                        FLLog.Info("Net", $"Remote disconnected for reason '{reason}' ({info.Reason})");
                        Disconnected?.Invoke(reason == DisconnectReason.Unknown ? DisconnectReason.ConnectionError : reason);
                    });
                }
            };
            client.Start();
            while (running)
            {
                if (Interlocked.Read(ref localPeerRequests) > 0)
                {
                    Interlocked.Decrement(ref localPeerRequests);
                    lock (srvinfo) srvinfo.Clear();
                    var dw = new PacketWriter();
                    dw.Put(LNetConst.BROADCAST_KEY);
                    FLLog.Debug("Net", "Sending broadcast");
                    client.SendBroadcast(dw, LNetConst.BROADCAST_PORT);
                }
                //ping servers
                lock (srvinfo)
                {
                    foreach (var inf in srvinfo)
                    {
                        var nowMs = sw.ElapsedMilliseconds;
                        if (nowMs - inf.LastPingTime > 2000) //ping every 2 seconds?
                        {
                            inf.LastPingTime = nowMs;
                            var om = new PacketWriter();
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
            http.Dispose();
        }

        public void Update() => client?.TriggerUpdate();

        public void SendPacket(IPacket packet, PacketDeliveryMethod method)
        {
            var om = new PacketWriter(new NetDataWriter(), hpidWrite);
            Packets.Write(om, packet);
            method.ToLiteNetLib(out DeliveryMethod mt, out byte ch);
            client.FirstPeer?.Send(om, ch, mt);
        }

        public void Shutdown()
        {
            if (running) Stop();
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
