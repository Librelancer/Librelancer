// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
namespace LibreLancer.Fx
{
	public class ParticleEffect
	{
		ParticleLibrary lib;

		public string Name;
		public uint CRC;

		public List<NodeReference> References = new List<NodeReference>();

		public ParticleEffect (ParticleLibrary lib)
		{
			this.lib = lib;
		}

		public ResourceManager ResourceManager
		{	
			get
			{
				return lib.Resources;
			}
		}
	}
}

