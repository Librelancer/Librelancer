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
namespace LibreLancer.Media
{
	static class SoundLoader
	{
		const uint MAGIC_RIFF = 0x46464952;
		const uint MAGIC_OGG = 0x5367674F; //Future reference

		public static StreamingSound Open(Stream stream)
		{
			var reader = new BinaryReader(stream);
			uint magic = reader.ReadUInt32();
			reader.BaseStream.Seek(-4, SeekOrigin.Current);
			switch (magic)
			{
				case MAGIC_RIFF:
					return RiffLoader.GetSound(stream);
				case MAGIC_OGG:
					//TODO: Opus
					return VorbisLoader.GetSound(stream);
				default:
					return Mp3Utils.GetSound(stream, null);
			}
		}
	}
}

