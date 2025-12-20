// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Data.GameData.World
{
	public enum DockKinds
	{
		Base,
		Jump,
		Tradelane
	}

	public class DockAction
	{
		public DockKinds Kind;
		public string Target;
		public string TargetLeft;
		public string Exit;
		public string Tunnel;
	}
}
