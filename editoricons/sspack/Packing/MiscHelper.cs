#region MIT License

/*
 * Copyright (c) 2009-2010 Nick Gravelyn (nick@gravelyn.com), Markus Ewald (cygon@nuclex.org)
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a 
 * copy of this software and associated documentation files (the "Software"), 
 * to deal in the Software without restriction, including without limitation 
 * the rights to use, copy, modify, merge, publish, distribute, sublicense, 
 * and/or sell copies of the Software, and to permit persons to whom the Software 
 * is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all 
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
 * PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 
 * 
 */

#endregion

using System.IO;

namespace sspack
{
	public static class MiscHelper
	{
		// the valid extensions for images
		public static readonly string[] AllowedImageExtensions = new[] { "png", "jpg", "bmp", "gif" };

		// determines if a file is an image we accept
		public static bool IsImageFile(string file)
		{
			if (!File.Exists(file))
				return false;

			// ToLower for string comparisons
			string fileLower = file.ToLower();

			// see if the file ends with one of our valid extensions
			foreach (var ext in AllowedImageExtensions)
				if (fileLower.EndsWith(ext))
					return true;
			return false;
		}

		// stolen from http://en.wikipedia.org/wiki/Power_of_two#Algorithm_to_find_the_next-highest_power_of_two
		public static int FindNextPowerOfTwo(int k)
		{
			k--;
			for (int i = 1; i < sizeof(int) * 8; i <<= 1)
				k = k | k >> i;
			return k + 1;
		}
	}
}
