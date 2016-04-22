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
namespace LibreLancer.ImageLib
{
	public static class Generic
	{
		public static Texture2D FromFile(string file)
		{
			using(var stream = File.OpenRead(file)) {
				return FromStream (stream);
			}
		}
		public static Texture2D FromStream(Stream stream)
		{
			if (DDS.StreamIsDDS (stream)) {
				return DDS.DDSFromStream2D (stream, 0, true);
			} else if (PNG.StreamIsPng (stream)) {
				return PNG.FromStream (stream);
			} else if (BMP.StreamIsBMP (stream)) {
				return BMP.FromStream (stream);
			} else {
				return TGA.FromStream (stream);
			}
		}
	}
}

