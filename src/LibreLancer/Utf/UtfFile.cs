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
 * The Original Code is Starchart code (http://flapi.sourceforge.net/).
 * Data structure from Freelancer UTF Editor by Cannon & Adoxa, continuing the work of Colin Sanby and Mario 'HCl' Brito (http://the-starport.net)
 * 
 * The Initial Developer of the Original Code is Malte Rupprecht (mailto:rupprema@googlemail.com).
 * Portions created by the Initial Developer are Copyright (C) 2011, 2012
 * the Initial Developer. All Rights Reserved.
 */

using System;
using System.IO;
using System.Text;

namespace LibreLancer.Utf
{
    public abstract class UtfFile
    {
        public const string FILE_TYPE = "UTF ";
        const int TAG_LEN = 4;
        public const int FILE_VERSION = 257;

        protected static IntermediateNode parseFile(string path)
        {
            if (path == null) throw new ArgumentNullException("path");
            byte[] nodeBlock;
            string stringBlock;
            byte[] dataBlock;

            using (BinaryReader reader = new BinaryReader(VFS.Open(path)))
            {
                byte[] buffer = new byte[TAG_LEN];
                reader.Read(buffer, 0, TAG_LEN);
                string fileType = Encoding.ASCII.GetString(buffer);
                if (fileType != FILE_TYPE)
                    throw new FileFormatException(path, fileType, FILE_TYPE);

                int formatVersion = reader.ReadInt32();
                if (formatVersion != FILE_VERSION)
                    throw new FileVersionException(path, fileType, formatVersion, FILE_VERSION);


                int nodeBlockOffset = reader.ReadInt32();
                if (nodeBlockOffset > reader.BaseStream.Length)
                    throw new FileContentException(fileType, "The node block offset was out of range: " + nodeBlockOffset);

                int nodeBlockSize = reader.ReadInt32();
                if (nodeBlockOffset + nodeBlockSize > reader.BaseStream.Length)
                    throw new FileContentException(fileType, "The node block size was out of range: " + nodeBlockSize);

                //int zero = reader.ReadInt32();
                //int headerSize = reader.ReadInt32();
                reader.BaseStream.Seek(2 * sizeof(int), SeekOrigin.Current);

                int stringBlockOffset = reader.ReadInt32();
                if (stringBlockOffset > reader.BaseStream.Length)
                    throw new FileContentException(fileType, "The string block offset was out of range: " + stringBlockOffset);

                int stringBlockSize = reader.ReadInt32();
                if (stringBlockOffset + stringBlockSize > reader.BaseStream.Length)
                    throw new FileContentException(fileType, "The string block size was out of range: " + stringBlockSize);

                //int unknown = reader.ReadInt32();
                reader.BaseStream.Seek(sizeof(int), SeekOrigin.Current);

                int dataBlockOffset = reader.ReadInt32();
                if (dataBlockOffset > reader.BaseStream.Length)
                    throw new FileContentException(fileType, "The data block offset was out of range: " + dataBlockOffset);


                nodeBlock = new byte[nodeBlockSize];
                reader.BaseStream.Seek(nodeBlockOffset, SeekOrigin.Begin);
                reader.Read(nodeBlock, 0, nodeBlockSize);

                Array.Resize<byte>(ref buffer, stringBlockSize);
                reader.BaseStream.Seek(stringBlockOffset, SeekOrigin.Begin);
                reader.Read(buffer, 0, stringBlockSize);
                stringBlock = Encoding.ASCII.GetString(buffer);

                dataBlock = new byte[(int)(reader.BaseStream.Length - dataBlockOffset)];
                reader.BaseStream.Seek(dataBlockOffset, SeekOrigin.Begin);
                reader.Read(dataBlock, 0, dataBlock.Length);
            }

            IntermediateNode root = null;

            using (BinaryReader reader = new BinaryReader(new MemoryStream(nodeBlock)))
            {
                root = Node.FromStream(reader, 0, stringBlock, dataBlock) as IntermediateNode;
                if (root == null)
                    throw new FileContentException(UtfFile.FILE_TYPE, "The root node doesn't have any child nodes.");
            }

            return root;
        }
    }
}
