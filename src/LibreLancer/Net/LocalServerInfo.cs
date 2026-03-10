// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Net;

namespace LibreLancer.Net
{
	public class LocalServerInfo
	{
		public required string Name;
        public required Guid Unique;
		public required string Description;
        public required string DataVersion;
		public required int CurrentPlayers;
		public required int MaxPlayers;
		public required IPEndPoint EndPoint;
        public int Ping = -1;
        internal long LastPingTime;
	}
}
