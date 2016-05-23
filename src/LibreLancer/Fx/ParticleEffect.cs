using System;
using System.Collections.Generic;
namespace LibreLancer.Fx
{
	public class ParticleEffect
	{
		ParticleLibrary lib;

		public string Name;
		public uint CRC;
		public Dictionary<FxNode, FxNode> Parents = new Dictionary<FxNode, FxNode>();
		public List<FxNode> AttachmentNodes = new List<FxNode>();
		public List<FxNode> Nodes = new List<FxNode>();

		public ParticleEffect (ParticleLibrary lib)
		{
			this.lib = lib;
		}
	}
}

