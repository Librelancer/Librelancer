// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
namespace LibreLancer.GameData
{
	public class BaseRoom
	{
		public string Nickname;
		public string Camera;
        public string SetScript;
		public List<string> ThnPaths;
		public List<BaseHotspot> Hotspots;
        public List<string> ForSaleShipPlacements;
		public List<BaseNpc> Npcs = new List<BaseNpc>();
		public string Music;
        public bool MusicOneShot;
		public string PlayerShipPlacement;
        public string StartScript;
        public string LandScript;
        public string LaunchScript;
        public string GoodscartScript;

        public IEnumerable<ThnScript> OpenScene()
        {
            foreach (var p in ThnPaths) yield return new ThnScript(p);
        }
        public ThnScript OpenSet()
        {
            if(SetScript != null)
                return new ThnScript(SetScript);
            return null;
        }
        public ThnScript OpenGoodscart()
        {
            if (GoodscartScript != null) return new ThnScript(GoodscartScript);
            return null;
        }

        internal Action InitAction;
        public void InitForDisplay()
        {
            if(InitAction != null)
            {
                InitAction();
                InitAction = null;
            }
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
