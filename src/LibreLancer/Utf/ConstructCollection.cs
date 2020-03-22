// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;

namespace LibreLancer.Utf
{
    public class ConstructCollection : IList<AbstractConstruct>
    {
        private List<AbstractConstruct> constructs = new List<AbstractConstruct>();

        public void AddNode(IntermediateNode root)
        {
            foreach (LeafNode conNode in root)
            {
                using (BinaryReader reader = new BinaryReader(conNode.DataSegment.GetReadStream()))
                {
                    switch (conNode.Name.ToLowerInvariant())
                    {
                        case "fix":
                             while (reader.BaseStream.Position < reader.BaseStream.Length) constructs.Add(new FixConstruct(reader));
                            break;
                        case "loose":
                            while (reader.BaseStream.Position < reader.BaseStream.Length) constructs.Add(new LooseConstruct(reader));
                            break;
                        case "rev":
                            while (reader.BaseStream.Position < reader.BaseStream.Length) constructs.Add(new RevConstruct(reader));
                            break;
                        case "pris":
                            while (reader.BaseStream.Position < reader.BaseStream.Length) constructs.Add(new PrisConstruct(reader));
                            break;
                        case "sphere":
                            while (reader.BaseStream.Position < reader.BaseStream.Length) constructs.Add(new SphereConstruct(reader));
                            break;
                        default: throw new Exception("Invalid node in " + root.Name + ": " + conNode.Name);
                    }
                }
            }
        }

		public ConstructCollection CloneAll()
		{
			var newcol = new ConstructCollection();
			foreach (var c in constructs) {
				newcol.constructs.Add(c.Clone());
			}
			return newcol;
		}

        public AbstractConstruct Find(string name)
        {
            for(int i = 0; i < constructs.Count; i++)
            {
                if (constructs[i].ChildName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return constructs[i];
            }
            return null;
        }

        public int IndexOf(AbstractConstruct item)
        {
            return constructs.IndexOf(item);
        }

        public void Insert(int index, AbstractConstruct item)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        public AbstractConstruct this[int index]
        {
            get
            {
                return constructs[index];
            }
            set
            {
                constructs[index] = value;
            }
        }

        public void Add(AbstractConstruct item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(AbstractConstruct item)
        {
            return constructs.Contains(item);
        }

        public void CopyTo(AbstractConstruct[] array, int arrayIndex)
        {
            constructs.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return constructs.Count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool Remove(AbstractConstruct item)
        {
            throw new NotSupportedException();
        }

        public IEnumerator<AbstractConstruct> GetEnumerator()
        {
            return constructs.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return constructs.GetEnumerator();
        }
    }
}
