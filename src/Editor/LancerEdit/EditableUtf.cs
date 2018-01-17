/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.Collections.Generic;
using LL = LibreLancer.Utf;
using System.IO;
using System.Text;

namespace LancerEdit
{
	public class EditableUtf : LL.UtfFile
	{
		public LUtfNode Root;

		public EditableUtf()
		{
			Root = new LUtfNode();
			Root.Name = "/";
			Root.Children = new List<LUtfNode>();
		}

		public EditableUtf(string filename) : this()
		{
			foreach (var node in parseFile(filename))
			{
				Root.Children.Add(ConvertNode(node, Root));
			}
		}

		//Produce an engine-internal representation of the nodes
		public LL.IntermediateNode Export()
		{
			var children = new List<LL.Node>();
			foreach (var child in Root.Children)
				children.Add(ExportNode(child));
			return new LL.IntermediateNode("/", children);
		}

		LL.Node ExportNode(LUtfNode n)
		{
			if (n.Data != null)
				return new LL.LeafNode(n.Name, n.Data);
			var children = new List<LL.Node>();
			foreach (var child in n.Children)
				children.Add(ExportNode(child));
			return new LL.IntermediateNode(n.Name, children);
		}

		LUtfNode ConvertNode(LL.Node node, LUtfNode parent)
		{
			var n = new LUtfNode();
			n.Name = node.Name;
			n.Parent = parent;
			if (node is LL.IntermediateNode)
			{
				var im = (LL.IntermediateNode)node;
				n.Children = new List<LUtfNode>();
				foreach (var child in im)
					n.Children.Add(ConvertNode(child, n));
			}
			else
			{
				var lf = (LL.LeafNode)node;
				n.Data = lf.ByteArrayData;
			}
			return n;
		}

		//Write the nodes out to a file
		public void Save(string filename)
		{
			Dictionary<string, int> stringOffsets = new Dictionary<string, int>();
			Dictionary<LUtfNode, int> dataOffsets = new Dictionary<LUtfNode, int>();
			List<string> strings = new List<string>();
			using (var writer = new BinaryWriter(File.Create(filename)))
			{
				/*write signature*/
				writer.Write((byte)'U');
				writer.Write((byte)'T');
				writer.Write((byte)'F');
				writer.Write((byte)' ');
				writer.Write(LibreLancer.Utf.UtfFile.FILE_VERSION);

				int currentDataOffset = 0;
				foreach (var node in Root.IterateAll())
				{
					if (!strings.Contains(node.Name)) strings.Add(node.Name);
					if (node.Data != null)
					{
						dataOffsets.Add(node, currentDataOffset);
						currentDataOffset += node.Data.Length;
					}
				}

				byte[] stringBlock;
				using (var mem = new MemoryStream())
				{
					foreach (var str in strings)
					{
						stringOffsets.Add(str, (int)mem.Position);
						var strb = Encoding.ASCII.GetBytes(str);
						mem.Write(strb, 0, strb.Length);
						mem.WriteByte(0); //null terminate
					}
					strings = null;
					stringBlock = mem.ToArray();
				}

				writer.Write((int)40 + stringBlock.Length); //nodeBlockOffset
				int strlen = stringBlock.Length;
				var nodeBlockSz = writer.BaseStream.Position;
				writer.Write((int)0); //nodeBlockSize
				writer.Write((int)0); //padding
				writer.Write((int)40); //headerSize
				writer.Write((int)40); //stringBlockOffset
				writer.Write((int)stringBlock.Length);
				writer.Write((int)0); //unknown
				var dataBlockDesc = writer.BaseStream.Position;
				writer.Write((int)0); //dataBlockOffset
									  //write out string block
				writer.Write(stringBlock);
				stringBlock = null;
				//write out node block
				WriteNode(Root, writer, stringOffsets, dataOffsets, 40 + strlen, true);
				var endNodes = (int)writer.BaseStream.Position;
				//write out data block
				foreach (var node in Root.IterateAll())
				{
					if (node.Data != null)
					{
						writer.Write(node.Data);
					}
				}
				writer.BaseStream.Seek(nodeBlockSz, SeekOrigin.Begin);
				int nodesSize = (endNodes - 40 - strlen);
				writer.Write((int)(endNodes - 40 - strlen));
				writer.BaseStream.Seek(dataBlockDesc, SeekOrigin.Begin);
				writer.Write((int)endNodes);
			}
		}
		void WriteNode(LUtfNode node, BinaryWriter writer, Dictionary<string, int> strOff, Dictionary<LUtfNode, int> datOff, int nodeblockoffset, bool last)
		{
			if (node.Data != null)
			{
				if (last)
					writer.Write((int)0); //no siblings
				else
					writer.Write((int)(writer.BaseStream.Position - nodeblockoffset + sizeof(int) * 11)); //peerOffset
				writer.Write(strOff[node.Name]); //nameOffset
				writer.Write((int)LL.NodeFlags.Leaf); //leafNode

				writer.Write((int)0); //zero
				writer.Write(datOff[node]); //dataOffset
				writer.Write((int)0); //allocatedSize (?)
				writer.Write(node.Data.Length);
				writer.Write(node.Data.Length);

				writer.Write((int)0); //padding/timestamp?
				writer.Write((int)0);
				writer.Write((int)0);
				return;
			}

			long startPos = writer.BaseStream.Position;
			writer.Write((int)0); //peerOffset
			writer.Write(strOff[node.Name]);
			writer.Write((int)LL.NodeFlags.Intermediate); //intermediateNode
			writer.Write((int)0); //padding
			writer.Write((int)(writer.BaseStream.Position - nodeblockoffset + 4)); //children start immediately after node
			for (int i = 0; i < node.Children.Count; i++)
			{
				WriteNode(node.Children[i], writer, strOff, datOff, nodeblockoffset, i == (node.Children.Count - 1));
			}
			if (!last) //if there's siblings
			{
				var endPos = writer.BaseStream.Position;
				writer.BaseStream.Seek(startPos, SeekOrigin.Begin);
				writer.Write((int)endPos - nodeblockoffset);
				writer.BaseStream.Seek(endPos, SeekOrigin.Begin);
			}
		}
	}

	public class LUtfNode
	{
		public string Name;
		public List<LUtfNode> Children;
		public LUtfNode Parent;
		public byte[] Data;

		public IEnumerable<LUtfNode> IterateAll()
		{
			yield return this;
			if (Children != null)
			{
				foreach (var child in Children)
					foreach (var c in child.IterateAll())
						yield return c;
			}
		}

		public LUtfNode MakeCopy()
		{
			var copy = new LUtfNode();
			copy.Name = Name;
			copy.Data = Data;
			if (Children != null)
			{
				copy.Children = new List<LUtfNode>(Children.Count);
				foreach (var child in Children)
					copy.Children.Add(child.MakeCopy());
			}
			return copy;
		}
	}
}
