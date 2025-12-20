// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
namespace LibreLancer.Data.Schema.Solar
{
    public enum DockSphereType
    {
        berth,
        moor_small,
        moor_medium,
        moor_large,
        ring,
        airlock
    }
	public class DockSphere
	{
		public DockSphereType Type;
		public string Hardpoint;
		public int Radius;
		public string Script;
		public DockSphere()
		{
		}
	}
}
