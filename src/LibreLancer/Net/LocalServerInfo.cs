// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Net;

namespace LibreLancer.Net
{
	public class LocalServerInfo
	{
		public string Name;
        public Guid Unique;
		public string Description;
        public string DataVersion;
		public int CurrentPlayers;
		public int MaxPlayers;
		public IPEndPoint EndPoint;
        public int Ping = -1;
        internal long LastPingTime;
	}
}
