// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LL = LibreLancer.Utf;
using System.IO;
using System.Text;
using System.Threading;

namespace LibreLancer.ContentEdit
{
    public record struct UtfStatistics(
        int NodeCount,
        int StringBlockSize,
        int NodeBlockSize,
        int DataBlockSize,
        int DeduplicatedSize)
    {
        public override string ToString() =>
            $"Node Count: {NodeCount}, String Block: {DebugDrawing.SizeSuffix(StringBlockSize)}, Node Block: {DebugDrawing.SizeSuffix(NodeBlockSize)}, Data Block: {DebugDrawing.SizeSuffix(DataBlockSize)}, Deduplicated: {DebugDrawing.SizeSuffix(DeduplicatedSize)}";
    }

	public class EditableUtf : LL.UtfFile
	{
		public LUtfNode Root;
        public LL.IntermediateNode Source;
		public EditableUtf()
		{
			Root = new LUtfNode();
            Root.Name = "/";
			Root.Children = new List<LUtfNode>();
		}

		public EditableUtf(string filename) : this()
        {
            Source = parseFile(filename, File.OpenRead(filename));
			foreach (var node in Source)
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

        public static LL.IntermediateNode NodeToEngine(LUtfNode node)
        {
            return ExportNode(node) as LL.IntermediateNode;
        }

		static LL.Node ExportNode(LUtfNode n)
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

        string GetUtfPath(LUtfNode n)
        {
            List<string> strings = new List<string>();
            LUtfNode node = n;
            while (node.Name != "/" && node.Name != "\\")
            {
                strings.Add(node.Name);
                node = node.Parent;
            }
            strings.Reverse();
            var path = "/" + string.Join("/", strings);
            return path;
        }

		//Write the nodes out to a file

        public EditResult<UtfStatistics> Save(string filename, int version)
        {
            try
            {
                foreach (var node in Root.IterateAll())
                {
                    if(node.Children == null && node.Data == null)
                    {
                        return EditResult<UtfStatistics>.Error($"{GetUtfPath(node)} is empty. Can't write UTF");
                    }
                }
                if (version == 0)
                    return SaveV1(filename);
                else
                    return SaveV2(filename);
            }
            catch (Exception e)
            {
               return EditResult<UtfStatistics>.Error(e.ToString());
            }
        }

        EditResult<UtfStatistics> SaveV2(string filename)
        {
            Dictionary<string, int> stringOffsets = new Dictionary<string, int>();
            var dataSection = new DataSection(true);
            List<string> strings = new List<string>();
            using (var writer = new BinaryWriter(File.Create(filename)))
            {
                int nodeCount = 0;
                foreach (var node in Root.IterateAll())
                {
                    nodeCount++;
                    if(!strings.Contains(node.Name)) strings.Add(node.Name);
                    if (node.Data != null)
                        dataSection.AddNode(node);
                }
                //string block
                byte[] stringBlock;
                using (var mem = new MemoryStream())
                {
                    foreach (var str in strings)
                    {
                        stringOffsets.Add(str, (int)mem.Position);
                        var strx = str;
                        if (strx == "/")
                            strx = "\\";
                        var strb = Encoding.UTF8.GetBytes(strx);
                        mem.Write(BitConverter.GetBytes((short) strb.Length));
                        mem.Write(strb, 0, strb.Length);
                    }
                    strings = null;
                    stringBlock = mem.ToArray();
                }
                //sig
                writer.Write((byte)'X');
                writer.Write((byte)'U');
                writer.Write((byte)'T');
                writer.Write((byte)'F');
                //v1
                writer.Write((byte) 1);
                //sizes
                writer.Write(stringBlock.Length);
                var nodeLenPos = writer.BaseStream.Position;
                writer.Write((int)0);
                writer.Write(dataSection.Length);
                //write strings
                writer.Write(stringBlock);
                //node block
                var nodeLen = writer.BaseStream.Position;
                WriteNodeV2(Root, writer, stringOffsets, dataSection);
                nodeLen = writer.BaseStream.Position - nodeLen;
                //data block
                foreach (var node in Root.IterateAll())
                {
                    if (node.Write && node.Data != null)
                    {
                        writer.Write(node.Data);
                    }
                }
                writer.BaseStream.Seek(nodeLenPos, SeekOrigin.Begin);
                writer.Write((int)nodeLen);
                return new UtfStatistics(nodeCount, stringBlock.Length, (int)nodeLen, dataSection.Length,
                    dataSection.BytesSaved).AsResult();
            }
        }

        static void WriteNodeV2(LUtfNode node, BinaryWriter writer, Dictionary<string, int> strOff, DataSection data)
        {
            writer.WriteVarUInt64((ulong)strOff[node.Name]); //nameOffset
            if (node.Data != null)
            {
                writer.Write((byte)0);
                writer.WriteVarUInt64((ulong)data.GetOffset(node));
                writer.WriteVarUInt64((ulong)node.Data.Length);
            }
            else
            {
                writer.Write((byte)1);
                writer.WriteVarUInt64((ulong)node.Children.Count);
                for (int i = 0; i < node.Children.Count; i++)
                {
                    WriteNodeV2(node.Children[i], writer, strOff, data);
                }
            }
        }

        class DataSection
        {
            public int BytesSaved = 0;

            private Dictionary<LUtfNode, int> dataOffsets = new();
            private Dictionary<ulong, List<(byte[] Data, int Offset)>> allocated = new();
            private int currentDataOffset = 0;

            public int Length => currentDataOffset;


            private bool v2;
            public DataSection(bool v2)
            {
                this.v2 = v2;
            }

            static ulong FNV1A64(byte[] bytes)
            {
                ulong hash = 14695981039346656037;
                for (var i = 0; i < bytes.Length; i++)
                    hash = (hash ^ bytes[i]) * 0x100000001b3;
                return hash;
            }

            public void AddNode(LUtfNode node)
            {
                if (node.Data == null)
                    return;
                int dataAlloc = v2 ? node.Data.Length : node.Data.Length + 3 & ~3;
                node.Write = true;
                var hash = FNV1A64(node.Data);
                if (allocated.TryGetValue(hash, out var nodeList))
                {
                    int i;
                    for (i = 0; i < nodeList.Count; i++)
                    {
                        if (nodeList[i].Data == node.Data ||
                            node.Data.AsSpan().SequenceEqual(nodeList[i].Data.AsSpan()))
                            break;
                    }
                    if (i == nodeList.Count)
                    {
                        nodeList.Add((node.Data, currentDataOffset));
                        dataOffsets[node] = currentDataOffset;
                        currentDataOffset += dataAlloc;
                    }
                    else
                    {
                        dataOffsets[node] = nodeList[i].Offset;
                        BytesSaved += node.Data.Length;
                        node.Write = false;
                    }
                }
                else
                {
                    nodeList = new List<(byte[] Data, int Offset)>();
                    allocated.Add(hash, nodeList);
                    nodeList.Add((node.Data, currentDataOffset));
                    dataOffsets[node] = currentDataOffset;
                    currentDataOffset += dataAlloc;
                }
            }

            public int GetOffset(LUtfNode node) => dataOffsets[node];
        }


        EditResult<UtfStatistics> SaveV1(string filename)
		{
            Dictionary<string, int> stringOffsets = new Dictionary<string, int>();
			List<string> strings = new List<string>();
			using (var writer = new BinaryWriter(File.Create(filename)))
			{
                var dataSection = new DataSection(false);
                int nodeCount = 0;
				foreach (var node in Root.IterateAll())
                {
                    nodeCount++;
					if (!strings.Contains(node.Name)) strings.Add(node.Name);
					dataSection.AddNode(node);
				}
				byte[] stringBlock;
				using (var mem = new MemoryStream())
				{
					foreach (var str in strings)
					{
						stringOffsets.Add(str, (int)mem.Position);
                        var strx = str;
                        if (strx == "/")
                            strx = "\\";
						var strb = Encoding.ASCII.GetBytes(strx);
						mem.Write(strb, 0, strb.Length);
						mem.WriteByte(0); //null terminate
					}
					strings = null;
					stringBlock = mem.ToArray();
				}

                byte[] nodeBlock;
                using(var mem = new MemoryStream())
                {
                    var res = WriteNode(Root, new BinaryWriter(mem), stringOffsets, dataSection, true);
                    if (res.IsError)
                        return new EditResult<UtfStatistics>(default, res.Messages);
                    nodeBlock = mem.ToArray();
                }

                int strAlloc = stringBlock.Length + 3 & ~3;
                /*write signature*/
                writer.Write((byte)'U');
                writer.Write((byte)'T');
                writer.Write((byte)'F');
                writer.Write((byte)' ');
                writer.Write(LibreLancer.Utf.UtfFile.FILE_VERSION);
				writer.Write((int)56); //nodeBlockOffset
                writer.Write((int)nodeBlock.Length); //nodeBlockLength
				writer.Write((int)0); //unused entry offset
				writer.Write((int)44); //entry Size - Not accurate but FL expects it to be 44
				writer.Write((int)56 + nodeBlock.Length); //stringBlockOffset
				writer.Write((int)strAlloc); //namesAllocatedSize
				writer.Write((int)stringBlock.Length); //namesUsedSize
				var dataBlockDesc = writer.BaseStream.Position;
                writer.Write((int) (56 + nodeBlock.Length + strAlloc));
                writer.Write((int)0); //unused
                writer.Write((int)0); //unused
                writer.Write((ulong) 125596224000000000); //Fake filetime
                writer.Write(nodeBlock);
				writer.Write(stringBlock);
                for(int i = 0; i < (strAlloc - stringBlock.Length); i++)
                    writer.Write((byte)0);
				//write out data block
				foreach (var node in Root.IterateAll())
				{
					if (node.Write && node.Data != null)
					{
						writer.Write(node.Data);
                        int dataAlloc = node.Data.Length + 3 & ~3;
                        for(int i = 0; i < (dataAlloc - node.Data.Length); i++)
                            writer.Write((byte)0);
					}
				}
                return new UtfStatistics(nodeCount, stringBlock.Length, (int)nodeBlock.Length, dataSection.Length,
                    dataSection.BytesSaved).AsResult();
			}
		}
		EditResult<bool> WriteNode(LUtfNode node, BinaryWriter writer, Dictionary<string, int> strOff, DataSection dataSection, bool last)
		{
			if (node.Data != null)
			{
				if (last)
					writer.Write((int)0); //no siblings
				else
					writer.Write((int)(writer.BaseStream.Position + sizeof(int) * 11)); //peerOffset
				writer.Write(strOff[node.Name]); //nameOffset
				writer.Write((int)LL.NodeFlags.Leaf); //leafNode
                writer.Write((int)0); //padding
				writer.Write(dataSection.GetOffset(node)); //dataOffset
                int dataAlloc = node.Data.Length + 3 & ~3;
                writer.Write(dataAlloc); //allocatedSize (?)
				writer.Write(node.Data.Length); //usedSize
				writer.Write(node.Data.Length); //uncompressedSize

                writer.Write(-2037297339);
                writer.Write(-2037297339);
                writer.Write(-2037297339);
				return true.AsResult();
			}

			long startPos = writer.BaseStream.Position;
			writer.Write((int)0); //peerOffset
			writer.Write(strOff[node.Name]);
			writer.Write((int)LL.NodeFlags.Intermediate); //intermediateNode
			writer.Write((int)0); //padding
			writer.Write(node.Children.Count == 0 ? 0 : (int)(writer.BaseStream.Position + 28)); //children start immediately after node
            writer.Write((int)0); //allocatedSize
            writer.Write((int)0); //usedSize
            writer.Write((int)0); //uncompressedSize
            writer.Write(-2037297339);
            writer.Write(-2037297339);
            writer.Write(-2037297339);
            //There should be 3 more DWORDS here but we can safely not write them for FL
			for (int i = 0; i < node.Children.Count; i++)
			{
				var res = WriteNode(node.Children[i], writer, strOff, dataSection, i == (node.Children.Count - 1));
                if (res.IsError)
                    return res;
            }
			if (!last) //if there's siblings
			{
				var endPos = writer.BaseStream.Position;
				writer.BaseStream.Seek(startPos, SeekOrigin.Begin);
				writer.Write((int)endPos);
				writer.BaseStream.Seek(endPos, SeekOrigin.Begin);
			}
            return true.AsResult();
        }
	}

	public class LUtfNode
    {
        private static long _interfaceId = 0;
        public long InterfaceID { get; private set; }

		public string Name;
        public string ResolvedName;
		public List<LUtfNode> Children;
		public LUtfNode Parent;
		public byte[] Data;
        internal bool Write = true;

        public LUtfNode()
        {
            InterfaceID = Interlocked.Increment(ref _interfaceId);
        }

        public static LUtfNode StringNode(LUtfNode parent, string name, string data)
            => new () { Name = name, Parent = parent, StringData = data };

        public static LUtfNode FloatNode(LUtfNode parent, string name, float data) =>
            new() { Name = name, Parent = parent, Data = BitConverter.GetBytes(data) };

        public static LUtfNode IntNode(LUtfNode parent, string name, int data) =>
            new() { Name = name, Parent = parent, Data = BitConverter.GetBytes(data) };

        public string StringData
        {
            get
            {
                if (Data != null)
                    return Encoding.ASCII.GetString(Data).TrimEnd('\0');
                return null;
            }
            set
            {
                Data = Encoding.ASCII.GetBytes(value + "\0");
            }
        }

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
