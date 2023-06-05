// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data;
using LibreLancer.Thn;

namespace LibreLancer.GameData.World
{
	public class BaseRoom
	{
        //Populated from room ini
		public string Nickname;
        public string SourceFile;
		public string Camera;
        public ResolvedThn SetScript;
		public List<ResolvedThn> ThnPaths;
		public List<BaseHotspot> Hotspots;
        public List<string> ForSaleShipPlacements;
		public string Music;
        public bool MusicOneShot;
		public string PlayerShipPlacement;
        public ResolvedThn StartScript;
        public ResolvedThn LandScript;
        public ResolvedThn LaunchScript;
        public ResolvedThn GoodscartScript;
        //Populated from mbases        
        public int MaxCharacters;
        public List<BaseFixedNpc> FixedNpcs = new List<BaseFixedNpc>();
        
        public IEnumerable<ThnScript> OpenScene()
        {
            foreach (var p in ThnPaths) yield return new ThnScript(p.ResolvedPath);
        }
        public ThnScript OpenSet()
        {
            if(SetScript != null)
                return new ThnScript(SetScript.ResolvedPath);
            return null;
        }
        public ThnScript OpenGoodscart()
        {
            if (GoodscartScript != null) return new ThnScript(GoodscartScript.ResolvedPath);
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
        public string VirtualRoom;
	}

    public class BaseFixedNpc
    {
        public BaseNpc Npc;
        public string Placement;
        public ResolvedThn FidgetScript;
        public string Action;
    }
}
