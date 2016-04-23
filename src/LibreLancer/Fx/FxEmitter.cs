using System;
using LibreLancer.Utf.Ale;
namespace LibreLancer.Fx
{
	public class FxEmitter : FxNode
	{
		public int InitialParticles;
		public AlchemyFloatAnimation Frequency;
		public AlchemyFloatAnimation EmitCount;
		public AlchemyFloatAnimation InitLifeSpan;

		public FxEmitter (AlchemyNode ale) : base(ale)
		{
		}
	}
}

