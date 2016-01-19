using System;

namespace LibreLancer.Utf.Ale
{
	public class AleFile : UtfFile
	{
		public ALEffectLib FxLib;
		public AlchemyNodeLibrary NodeLib;
		public AleFile (string file)
		{
			//TODO: This is ugly
			foreach (var node in parseFile(file)) {
				switch (node.Name.ToLowerInvariant ()) {
				case "aleffectlib":
					FxLib = new ALEffectLib ((node as IntermediateNode) [0] as LeafNode);
					break;
				case "alchemynodelibrary":
					NodeLib = new AlchemyNodeLibrary ((node as IntermediateNode) [0] as LeafNode);
					break;
				default:
					throw new NotImplementedException (node.Name);
				}
			}
		}
	}
}

