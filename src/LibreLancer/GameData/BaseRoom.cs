// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and confiditons defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
namespace LibreLancer.GameData
{
	public class BaseRoom
	{
		public string Nickname;
		public string Camera;
		public List<string> ThnPaths;
		public List<BaseHotspot> Hotspots;
		public List<BaseNpc> Npcs = new List<BaseNpc>();
		public string Music;
		public string PlayerShipPlacement;

		public IEnumerable<ThnScript> OpenScripts()
		{
			foreach (var p in ThnPaths) yield return new ThnScript(p);
		}
	}
	public class BaseHotspot
	{
		public string Name;
		public string Behavior;
		public string Room;
		public string SetVirtualRoom;
	}
	public class BaseNpc
	{
		public string StandingPlace;
		public string HeadMesh;
		public string BodyMesh;
		public string LeftHandMesh;
		public string RightHandMesh;
	}
}
