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
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.IO;
using Jitter.LinearMath;
namespace LibreLancer.Sur
{
	//TODO: Sur reader is VERY incomplete & undocumented
	public class SurFile
	{
		const string VERS_TAG = "vers";

		public SurFile (Stream stream)
		{
			using (var reader = new BinaryReader (stream)) {
				if (reader.ReadTag () != VERS_TAG)
					throw new Exception ("Not a sur file");
				reader.ReadSingle (); //vers?
				while (stream.Position < stream.Length) {
					uint meshid = reader.ReadUInt32 ();
					uint tagcount = reader.ReadUInt32 ();
					while (tagcount-- > 0) {
						var tag = reader.ReadTag ();
						if (tag == "surf") {
							uint size = reader.ReadUInt32 (); //TODO: SUR - What is this?
							var surf = new Surface(reader);

						} else if (tag == "exts") {
							//TODO: SUR - What are exts used for?
							var min = new JVector (
								          reader.ReadSingle (),
								          reader.ReadSingle (),
								          reader.ReadSingle ()
							          );
							var max = new JVector (
								          reader.ReadSingle (),
								          reader.ReadSingle (),
								          reader.ReadSingle ()
							          );
						} else if (tag == "!fxd") {
							//TODO: SUR - WTF is this?!
						} else if (tag == "hpid") {
							//TODO: SUR - hpid. What does this do?
							uint count2 = reader.ReadUInt32 ();
							while (count2-- > 0) {
								uint mesh2 = reader.ReadUInt32 ();
							}
						}
					}
				}
			}
		}


	}
}

