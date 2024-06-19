// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data;
using LibreLancer.Thn;

namespace LibreLancer.GameData.World
{
    public record SceneScript(bool AllAmbient, bool TrafficPriority, ResolvedThn Thn);
	public class BaseRoom : IdentifiableItem
	{
        //Populated from room ini
        public string SourceFile;
		public string Camera;
        public ResolvedThn SetScript;
		public List<SceneScript> SceneScripts;
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
            foreach (var p in SceneScripts) yield return p.Thn.LoadScript();
        }
        public ThnScript OpenSet()
        {
            if (SetScript != null)
                return SetScript.LoadScript();
            return null;
        }
        public ThnScript OpenGoodscart()
        {
            if (GoodscartScript != null) return GoodscartScript.LoadScript();
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
