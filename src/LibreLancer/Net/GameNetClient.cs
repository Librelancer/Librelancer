// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Lidgren.Network;
namespace LibreLancer
{
    public class GameNetClient : IPacketConnection
    {
        bool running = false;
        IUIThread mainThread;
        Thread networkThread;
        NetClient client;
        public event Action<LocalServerInfo> ServerFound;
        public event Action<string> AuthenticationRequired;
        public event Action<string> Disconnected;
        public Guid UUID;
        ConcurrentQueue<IPacket> packets = new ConcurrentQueue<IPacket>();
        
        public void Start()
        {
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
            running = false;
            networkThread.Join();
        }

        public bool Connected => client.ConnectionStatus == NetConnectionStatus.Connected;

        public void Shutdown()
        {
            if (running) Stop();
        }

        public void DiscoverLocalPeers()
        {
            if (running)
            {
                while (client == null || client.Status != NetPeerStatus.Running) Thread.Sleep(0);
                client.DiscoverLocalPeers(NetConstants.DEFAULT_PORT);
            }
        }

        public void DiscoverGlobalPeers()
        {
            //HTTP?
        }

        bool connecting = true;
        public void Connect(IPEndPoint endPoint)
        {
            lock (srvinfo) srvinfo.Clear();
            connecting = true;
            var message = client.CreateMessage();
            message.Write("Hello World!");
            client.Connect(endPoint, message);
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
                endpoint = new IPEndPoint(ip, NetConstants.DEFAULT_PORT);
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
                endpoint = new IPEndPoint(ip, NetConstants.DEFAULT_PORT);
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
            var conf = new NetPeerConfiguration(NetConstants.DEFAULT_APP_IDENT);
            conf.EnableMessageType(NetIncomingMessageType.UnconnectedData);
            conf.DualStack = true;
            client = new NetClient(conf);
            client.Start();
            NetIncomingMessage im;
            while (running)
            {
                //ping servers
                lock (srvinfo)
                {
                    foreach (var inf in srvinfo)
                    {
                        var nowMs = (long)(NetTime.Now * 1000);
                        if (nowMs - inf.LastPingTime > 600)
                        {
                            inf.LastPingTime = nowMs;
                            var om = client.CreateMessage();
                            om.Write(NetConstants.PING_MAGIC);
                            client.SendUnconnectedMessage(om, inf.EndPoint);
                        }
                    }
                }
                while ((im = client.ReadMessage()) != null)
                {
                    try
                    {
                        switch (im.MessageType)
                        {
                            case NetIncomingMessageType.DebugMessage:
                            case NetIncomingMessageType.ErrorMessage:
                            case NetIncomingMessageType.WarningMessage:
                            case NetIncomingMessageType.VerboseDebugMessage:
                                FLLog.Info("Lidgren", im.ReadString());
                                break;
                            case NetIncomingMessageType.UnconnectedData:
                                lock (srvinfo)
                                {
                                    foreach (var info in srvinfo)
                                    {
                                        if (info.EndPoint.Equals(im.SenderEndPoint))
                                        { 
                                            var t = (long)(im.ReceiveTime * 1000);
                                            info.Ping = (int)(t - info.LastPingTime);
                                            if (info.Ping < 0) info.Ping = 0;
                                        }
                                    }
                                }
                                break;
                            case NetIncomingMessageType.DiscoveryResponse:
                                if (ServerFound != null)
                                {
                                    var info = new LocalServerInfo();
                                    info.EndPoint = im.SenderEndPoint;
                                    info.Name = im.ReadString();
                                    info.Description = im.ReadString();
                                    info.DataVersion = im.ReadString();
                                    info.CurrentPlayers = im.ReadInt32();
                                    info.MaxPlayers = im.ReadInt32();
                                    info.LastPingTime = sw.ElapsedMilliseconds;
                                    var om = client.CreateMessage();
                                    om.Write(NetConstants.PING_MAGIC);
                                    client.SendUnconnectedMessage(om, info.EndPoint);
                                    lock (srvinfo) srvinfo.Add(info);
                                    mainThread.QueueUIThread(() => ServerFound?.Invoke(info));
                                }
                                break;
                            case NetIncomingMessageType.StatusChanged:
                                var status = (NetConnectionStatus)im.ReadByte();
                                if (status == NetConnectionStatus.Disconnected)
                                {
                                    FLLog.Info("Net", "Disconnected");
                                    var reason = im.ReadString();
                                    mainThread.QueueUIThread(() => Disconnected?.Invoke(reason)); 
                                    running = false;
                                }
                                break;
                            case NetIncomingMessageType.Data:
                                var pkt = im.ReadPacket();
                                if (connecting)
                                {
                                    if (pkt is AuthenticationPacket)
                                    {
                                        var auth = (AuthenticationPacket) pkt;
                                        FLLog.Info("Net", "Authentication Packet Received");
                                        if (auth.Type == AuthenticationKind.Token)
                                        {
                                            FLLog.Info("Net", "Token");
                                            var str = im.ReadString();
                                            mainThread.QueueUIThread(() => AuthenticationRequired(str));
                                        }
                                        else if (auth.Type == AuthenticationKind.GUID)
                                        {
                                            FLLog.Info("Net", "GUID");
                                            var response = client.CreateMessage();
                                            response.Write(new AuthenticationReplyPacket()
                                            {
                                                Guid = this.UUID
                                            });
                                            client.SendMessage(response, NetDeliveryMethod.ReliableOrdered);
                                        }
                                    }
                                    else if (pkt is LoginSuccessPacket)
                                    {
                                        connecting = false;
                                    }
                                    else
                                    {
                                        client.Disconnect("Invalid Packet");
                                    }
                                }
                                else
                                {
                                    packets.Enqueue(pkt);
                                }
                                break;
                        }
                    }
                    catch (Exception)
                    {
                        FLLog.Error("Net", "Error reading message of type " + im.MessageType.ToString());
                        throw;
                    }
                    client.Recycle(im);
                }
                Thread.Sleep(1);
            }
            FLLog.Info("Lidgren", "Client shutdown");
            client.Shutdown("Shutdown");
        }

        public void SendPacket(IPacket packet, NetDeliveryMethod method)
        {
            var msg = client.CreateMessage();
            msg.Write(packet);
            client.SendMessage(msg, method);
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
