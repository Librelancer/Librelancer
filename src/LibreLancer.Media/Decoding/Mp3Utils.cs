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
	class Mp3Utils
	{
		public static byte[] DecodeAll(Stream input, out int channels, out int freq)
		{
			channels = -1;
			freq = -1;
			using (var stream = new MP3Sharp.MP3Stream(input))
			{
				freq = stream.Frequency;
				if (stream.Format == MP3Sharp.SoundFormat.Pcm16BitMono)
					channels = 1;
				else
					channels = 2;
				using (var mem = new MemoryStream())
				{
					stream.CopyTo(mem);
					return mem.ToArray();
				}
			}
		}
	}
}

