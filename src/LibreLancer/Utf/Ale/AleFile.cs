// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Utf.Ale
{
	public class AleFile : UtfFile
	{
		public ALEffectLib FxLib;
		public AlchemyNodeLibrary NodeLib;
		public AleFile(string file) : this(parseFile(file))
		{

		}
		public AleFile (IntermediateNode root)
		{
			//TODO: This is ugly
			foreach (var node in root) {
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

