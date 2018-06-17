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

                byte[] nodeBlock;
                using(var mem = new MemoryStream())
                {
                    WriteNode(Root, new BinaryWriter(mem), stringOffsets, dataOffsets, true);
                    nodeBlock = mem.ToArray();
                }
                /*write signature*/
                writer.Write((byte)'U');
                writer.Write((byte)'T');
                writer.Write((byte)'F');
                writer.Write((byte)' ');
                writer.Write(LibreLancer.Utf.UtfFile.FILE_VERSION);
				writer.Write((int)56); //nodeBlockOffset
				writer.Write((int)nodeBlock.Length); //nodeBlockLength
				writer.Write((int)0); //unused entry offset
				writer.Write((int)44); //entry Size
				writer.Write((int)56 + nodeBlock.Length); //stringBlockOffset
				writer.Write((int)stringBlock.Length); //namesAllocatedSize
				writer.Write((int)stringBlock.Length); //namesUsedSize
				var dataBlockDesc = writer.BaseStream.Position;
				writer.Write((int)(56 + nodeBlock.Length + stringBlock.Length));
                writer.Write((int)0); //unused
                writer.Write((int)0); //unused
                writer.Write((int)0); //filetime
                writer.Write((int)0); //filetime
                writer.Write(nodeBlock);
				writer.Write(stringBlock);
				stringBlock = null;
                nodeBlock = null;
				//write out data block
				foreach (var node in Root.IterateAll())
				{
					if (node.Data != null)
					{
						writer.Write(node.Data);
					}
				}
			}
		}
		void WriteNode(LUtfNode node, BinaryWriter writer, Dictionary<string, int> strOff, Dictionary<LUtfNode, int> datOff, bool last)
		{
			if (node.Data != null)
			{
				if (last)
					writer.Write((int)0); //no siblings
				else
					writer.Write((int)(writer.BaseStream.Position + sizeof(int) * 11)); //peerOffset
				writer.Write(strOff[node.Name]); //nameOffset
				writer.Write(flgs); //leafNode
				writer.Write((int)LL.NodeFlags.Leaf); //sharing options - zero
				writer.Write(datOff[node]); //dataOffset
                writer.Write(node.Data.Length); //allocatedSize (?)
				writer.Write(node.Data.Length); //usedSize
				writer.Write(node.Data.Length); //uncompressedSize

                writer.Write((int)0);
                writer.Write((int)0);
                writer.Write((int)0);
				return;
			}

			long startPos = writer.BaseStream.Position;
			writer.Write((int)0); //peerOffset
			writer.Write(strOff[node.Name]);
			writer.Write((int)LL.NodeFlags.Intermediate); //intermediateNode
			writer.Write((int)0); //padding
			writer.Write((int)(writer.BaseStream.Position + 28)); //children start immediately after node
            writer.Write((int)0); //allocatedSize
            writer.Write((int)0); //usedSize
            writer.Write((int)0); //uncompressedSize

            writer.Write((int)0); //1
            writer.Write((int)0); //2
            writer.Write((int)0); //3
			for (int i = 0; i < node.Children.Count; i++)
			{
				WriteNode(node.Children[i], writer, strOff, datOff, i == (node.Children.Count - 1));
			}
			if (!last) //if there's siblings
			{
				var endPos = writer.BaseStream.Position;
				writer.BaseStream.Seek(startPos, SeekOrigin.Begin);
				writer.Write((int)endPos);
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
				foreach (var child in Children) {
					var newch = child.MakeCopy();
					newch.Parent = copy;
					copy.Children.Add(newch);
				}
			}
			return copy;
		}
	}
}
