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
 * The Original Code is FLApi code (http://flapi.sourceforge.net/).
 * 
 * The Initial Developer of the Original Code is Malte Rupprecht (mailto:rupprema@googlemail.com).
 * Portions created by the Initial Developer are Copyright (C) 2012
 * the Initial Developer. All Rights Reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;


//using FLParser;
//using FLParser.Utf;

//using FLApi.Universe;
using LibreLancer.Utf.Anm;

namespace LibreLancer.Utf
{
    public class ConstructCollection : IList<AbstractConstruct>
    {
        private List<AbstractConstruct> constructs = new List<AbstractConstruct>();

        public void AddNode(IntermediateNode root)
        {
            foreach (LeafNode conNode in root)
            {
                using (BinaryReader reader = new BinaryReader(new MemoryStream(conNode.ByteArrayData)))
                {
                    switch (conNode.Name.ToLowerInvariant())
                    {
                        case "fix":
                             while (reader.BaseStream.Position < reader.BaseStream.Length) constructs.Add(new FixConstruct(reader, this));
                            break;
                        case "loose":
                            while (reader.BaseStream.Position < reader.BaseStream.Length) constructs.Add(new LooseConstruct(reader, this));
                            break;
                        case "rev":
                            while (reader.BaseStream.Position < reader.BaseStream.Length) constructs.Add(new RevConstruct(reader, this));
                            break;
                        case "pris":
                            while (reader.BaseStream.Position < reader.BaseStream.Length) constructs.Add(new PrisConstruct(reader, this));
                            break;
                        case "sphere":
                            while (reader.BaseStream.Position < reader.BaseStream.Length) constructs.Add(new SphereConstruct(reader, this));
                            break;
                        default: throw new Exception("Invalid node in " + root.Name + ": " + conNode.Name);
                    }
                }
            }
        }

        public AbstractConstruct Find(string name)
        {
            return constructs.Find(p => p.ChildName.Equals(name, StringComparison.OrdinalIgnoreCase));
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
                throw new NotSupportedException();
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
