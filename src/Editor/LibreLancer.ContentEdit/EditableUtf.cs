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
            Source = parseFile(filename);
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

        public bool Save(string filename, int version, ref string error)
        {
            foreach (var node in Root.IterateAll())
            {
                if(node.Children == null && node.Data == null)
                {
                    error = string.Format("{0} is empty. Can't write UTF",GetUtfPath(node));
                    return false;
                }
            }
            if (version == 0)
                return SaveV1(filename, ref error);
            else
                return SaveV2(filename, ref error);
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
        
        bool SaveV2(string filename, ref string error)
        {
            Dictionary<string, int> stringOffsets = new Dictionary<string, int>();
            Dictionary<LUtfNode, int> dataOffsets = new Dictionary<LUtfNode, int>();
            List<string> strings = new List<string>();
            using (var writer = new BinaryWriter(File.Create(filename)))
            {
                int currentDataOffset = 0;
                int bytesSaved = 0;
                List<SmallData> smallDatas = new List<SmallData>();
                int nodeCount = 0;
                foreach (var node in Root.IterateAll())
                {
                    nodeCount++;
                    if(!strings.Contains(node.Name)) strings.Add(node.Name);
                    if (node.Data != null)
                    {
                        node.Write = node.Data.Length > 8;
                        if (node.Data.Length > 8 && node.Data.Length <= 64)
                        {
                            var small = new SmallData(node.Data);
                            int idx = -1;
                            for (int i = 0; i < smallDatas.Count; i++) {
                                if(smallDatas[i].Match(ref small)) {
                                    idx = i;
                                    break;
                                }
                            }
                            if(idx == -1) {
                                small.Offset = currentDataOffset;
                                smallDatas.Add(small);
                                dataOffsets.Add(node, currentDataOffset);
                                currentDataOffset += node.Data.Length;
                            } else {
                                node.Write = false;
                                bytesSaved += smallDatas[idx].Length;
                                dataOffsets.Add(node, smallDatas[idx].Offset);
                            }
                        } else if (node.Data.Length > 8)
                        {
                            dataOffsets.Add(node, currentDataOffset);
                            if (node.Data.Length > 128)
                            {
                                var compressed = CompressDeflate(node.Data);
                                if (compressed.Length < (node.Data.Length * 0.9))
                                {
                                    node.CompressedData = compressed;
                                    currentDataOffset += node.CompressedData.Length;
                                } 
                                else
                                {
                                    currentDataOffset += node.Data.Length;
                                }
                            }
                            else
                            {
                                currentDataOffset += node.Data.Length;
                            }
                        }
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
                ushort flags = 0;
                var stringsComp = CompressDeflate(stringBlock);
                if (stringsComp.Length < (stringBlock.Length / 2))
                {
                    flags = 0x1; //deflate compress strings
                    stringBlock = stringsComp;
                }
                //sig
                writer.Write((byte)'X');
                writer.Write((byte)'U');
                writer.Write((byte)'T');
                writer.Write((byte)'F');
                //v1
                writer.Write((byte) 1);
                //no flags
                writer.Write(flags);
                //sizes
                writer.Write(stringBlock.Length);
                writer.Write(nodeCount * 17);
                writer.Write(currentDataOffset);
                //write strings
                writer.Write(stringBlock);
                stringBlock = null;
                //node block
                int index = 0;
                WriteNodeV2(Root, writer, stringOffsets, dataOffsets, ref index, true);
                //data block
                foreach (var node in Root.IterateAll())
                {
                    if (node.Write && node.Data != null)
                    {
                        writer.Write(node.CompressedData ?? node.Data);
                        node.CompressedData = null;
                    }
                }
            }
            return true;
        }

        static void WriteNodeV2(LUtfNode node, BinaryWriter writer, Dictionary<string, int> strOff,
            Dictionary<LUtfNode, int> datOff, ref int myIdx, bool last)
        {
            writer.Write(strOff[node.Name]); //nameOffset
            if (node.Data != null)
            {
                myIdx++;
                if (!last)
                    writer.Write(myIdx);
                else
                    writer.Write((uint) 0);
                if (node.CompressedData != null)
                {
                    writer.Write((byte) 10); //Type 10: compressed deflate
                    writer.Write(datOff[node]);
                    writer.Write(node.CompressedData.Length);
                }
                else if (node.Data.Length > 8)
                {
                    writer.Write((byte) 1); //Type 1: Data Offset + Size
                    writer.Write(datOff[node]);
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
                    WriteNodeV2(node.Children[i], writer, strOff, datOff,ref myIdx, i == (node.Children.Count - 1));
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
        bool SaveV1(string filename, ref string error)
		{
            Dictionary<string, int> stringOffsets = new Dictionary<string, int>();
			Dictionary<LUtfNode, int> dataOffsets = new Dictionary<LUtfNode, int>();
			List<string> strings = new List<string>();
			using (var writer = new BinaryWriter(File.Create(filename)))
			{
				int currentDataOffset = 0;
                int bytesSaved = 0;
                List<SmallData> smallDatas = new List<SmallData>();
				foreach (var node in Root.IterateAll())
				{
					if (!strings.Contains(node.Name)) strings.Add(node.Name);
					if (node.Data != null)
					{
                        int dataAlloc = node.Data.Length + 3 & ~3;
                        node.Write = true;
                        //De-duplicate data up to 64 bytes
                        if (node.Data.Length <= 64)
                        {
                            var small = new SmallData(node.Data);
                            int idx = -1;
                            for (int i = 0; i < smallDatas.Count; i++) {
                                if(smallDatas[i].Match(ref small)) {
                                    idx = i;
                                    break;
                                }
                            }
                            if(idx == -1) {
                                small.Offset = currentDataOffset;
                                smallDatas.Add(small);
                                dataOffsets.Add(node, currentDataOffset);
                                currentDataOffset += dataAlloc;
                            } else {
                                node.Write = false;
                                bytesSaved += smallDatas[idx].Length;
                                dataOffsets.Add(node, smallDatas[idx].Offset);
                            }
                        }
                        else
                        {
                            dataOffsets.Add(node, currentDataOffset);
                            currentDataOffset += dataAlloc;
                        }
					}
				}
                LibreLancer.FLLog.Info("UTF", "Bytes Saved: " + bytesSaved);
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
                    WriteNode(Root, new BinaryWriter(mem), stringOffsets, dataOffsets, true);
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
            return true;
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
				writer.Write((int)LL.NodeFlags.Leaf); //leafNode
                writer.Write((int)0); //padding
				writer.Write(datOff[node]); //dataOffset
                int dataAlloc = node.Data.Length + 3 & ~3;
                writer.Write(dataAlloc); //allocatedSize (?)
				writer.Write(node.Data.Length); //usedSize
				writer.Write(node.Data.Length); //uncompressedSize
    
                writer.Write(-2037297339);
                writer.Write(-2037297339);
                writer.Write(-2037297339);
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
            writer.Write(-2037297339);
            writer.Write(-2037297339);
            writer.Write(-2037297339);
            //There should be 3 more DWORDS here but we can safely not write them for FL
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
        public string ResolvedName;
		public List<LUtfNode> Children;
		public LUtfNode Parent;
		public byte[] Data;
        internal byte[] CompressedData;
        internal bool Write = true;

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
