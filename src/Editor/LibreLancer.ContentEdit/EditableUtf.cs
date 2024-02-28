// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using LL = LibreLancer.Utf;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace LibreLancer.ContentEdit
{
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
        [StructLayout(LayoutKind.Sequential)]
        struct SmallData
        {
            public int Length;
            public long Data1;
            public long Data2;
            public long Data3;
            public long Data4;
            public long Data5;
            public long Data6;
            public long Data7;
            public long Data8;
            public int Offset;
            public unsafe SmallData(byte[] src)
            {
                Length = src.Length;
                Data1 = Data2 = Data3 = Data4 =
                    Data5 = Data6 = Data7 = Data8 = 0;
                Offset = 0;
                fixed(long *ptr = &Data1) {
                    var bytes = (byte*)ptr;
                    for (int i = 0; i < src.Length; i++)
                        bytes[i] = src[i];
                }
            }
            public bool Match(ref SmallData b)
            {
                if (Length != b.Length) return false;
                if (Data1 != b.Data1) return false;
                if (Data2 != b.Data2) return false;
                if (Data3 != b.Data3) return false;
                if (Data4 != b.Data4) return false;
                if (Data5 != b.Data5) return false;
                if (Data6 != b.Data6) return false;
                if (Data7 != b.Data7) return false;
                if (Data8 != b.Data8) return false;
                return true;
            }
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

        public EditResult<bool> Save(string filename, int version)
        {
            foreach (var node in Root.IterateAll())
            {
                if(node.Children == null && node.Data == null)
                {

                    return EditResult<bool>.Error($"{GetUtfPath(node)} is empty. Can't write UTF");
                }
            }
            if (version == 0)
                return SaveV1(filename);
            else
                return SaveV2(filename);
        }

        static byte[] CompressDeflate(byte[] input)
        {
            using (var mem = new MemoryStream())
            {
                using (var comp = new DeflateStream(mem, CompressionLevel.Optimal, true))
                {
                    comp.Write(input);
                }
                return mem.ToArray();
            }
        }

        EditResult<bool> SaveV2(string filename)
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
                    {
                        node.Write = node.Data.Length > 8;
                        if (node.Data.Length > 8)
                            dataSection.AddNode(node);
                    }
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
                writer.Write(nodeCount * 17);
                writer.Write(dataSection.Length);
                //write strings
                writer.Write(stringBlock);
                stringBlock = null;
                //node block
                int index = 0;
                WriteNodeV2(Root, writer, stringOffsets, dataSection, ref index, true);
                //data block
                foreach (var node in Root.IterateAll())
                {
                    if (node.Write && node.Data != null && node.Data.Length > 8)
                    {
                        writer.Write(node.Data);
                    }
                }
            }
            return true.AsResult();
        }

        static void WriteNodeV2(LUtfNode node, BinaryWriter writer, Dictionary<string, int> strOff,
            DataSection data, ref int myIdx, bool last)
        {
            writer.Write(strOff[node.Name]); //nameOffset
            if (node.Data != null)
            {
                myIdx++;
                if (!last)
                    writer.Write(myIdx);
                else
                    writer.Write((uint) 0);
                if (node.Data.Length > 8)
                {
                    writer.Write((byte) 1); //Type 1: Data Offset + Size
                    writer.Write(data.GetOffset(node));
                    writer.Write(node.Data.Length);
                }
                else
                {
                    writer.Write((byte)(node.Data.Length + 1)); //Type 2-9: Embedded Data
                    writer.Write(node.Data);
                    for (int i = node.Data.Length; i < 8; i++)
                    {
                        writer.Write((byte) 0); //padding
                    }
                }
            }
            else
            {
                long indexPos = writer.BaseStream.Position;
                writer.Write((uint) 0); //sibling index
                writer.Write((byte) 0); //folder
                myIdx++;
                writer.Write(myIdx);
                writer.Write((uint) 0); //padding
                for (int i = 0; i < node.Children.Count; i++)
                {
                    WriteNodeV2(node.Children[i], writer, strOff, data,ref myIdx, i == (node.Children.Count - 1));
                }
                if (!last)
                {
                    //write sibling index
                    var mPos = writer.BaseStream.Position;
                    writer.BaseStream.Seek(indexPos, SeekOrigin.Begin);
                    writer.Write(myIdx);
                    writer.BaseStream.Seek(mPos, SeekOrigin.Begin);
                }
            }
        }



        class DataSection
        {
            public int BytesSaved = 0;

            private Dictionary<LUtfNode, int> dataOffsets = new Dictionary<LUtfNode, int>();
            private int currentDataOffset = 0;

            public int Length => currentDataOffset;

            private List<(byte[] Data, ulong Hash, int Offset)> allocated =
                new List<(byte[] Data, ulong Hash, int Offset)>();

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
                int i;
                for (i = 0; i < allocated.Count; i++)
                {
                    if (allocated[i].Data == node.Data) //Compare by reference first
                        break;
                    if (allocated[i].Hash == hash && node.Data.Length == allocated[i].Data.Length)
                    {
                        int j;
                        for (j = 0; j < node.Data.Length; j++)
                        {
                            if (node.Data[j] != allocated[i].Data[j])
                                break;
                        }
                        if (j == node.Data.Length)
                            break;
                    }
                }
                if (i == allocated.Count) {
                    allocated.Add(( node.Data, hash, currentDataOffset));
                    dataOffsets[node] = currentDataOffset;
                    currentDataOffset += dataAlloc;
                }
                else
                {
                    dataOffsets[node] = allocated[i].Offset;
                    BytesSaved += node.Data.Length;
                    node.Write = false;
                }
            }

            public int GetOffset(LUtfNode node) => dataOffsets[node];
        }


        EditResult<bool> SaveV1(string filename)
		{
            Dictionary<string, int> stringOffsets = new Dictionary<string, int>();
			List<string> strings = new List<string>();
			using (var writer = new BinaryWriter(File.Create(filename)))
			{
                var dataSection = new DataSection(false);
				foreach (var node in Root.IterateAll())
				{
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
                        return res;
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
                stringBlock = null;
                nodeBlock = null;
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
			}
            return true.AsResult();
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

            if(node.Children == null ||
               node.Children.Count == 0)
                return EditResult<bool>.Error("Cannot save empty node " + node.Name);

			long startPos = writer.BaseStream.Position;
			writer.Write((int)0); //peerOffset
			writer.Write(strOff[node.Name]);
			writer.Write((int)LL.NodeFlags.Intermediate); //intermediateNode
			writer.Write((int)0); //padding
			writer.Write((int)(writer.BaseStream.Position + 28)); //children start immediately after node
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
		public string Name;
        public string ResolvedName;
		public List<LUtfNode> Children;
		public LUtfNode Parent;
		public byte[] Data;
        internal byte[] CompressedData;
        internal bool Write = true;

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
