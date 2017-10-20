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
 * Portions created by the Initial Developer are Copyright (C) 2013-2017
 * the Initial Developer. All Rights Reserved.
 */
using System;
using LibreLancer.Infocards;
using LibreLancer;
using System.Text;

namespace LancerEdit
{
	public class InfocardTools
	{
		static int[] fontSizes = new int[] {
			11,
			16,
			12,
			18,
			26,
			12,
			20,
			11
		}; //TODO: Translate font indices into fonts properly (or at least provide an approximation)

		public static string InfocardToMarkup(string infocard)
		{
			if (infocard == null) return "<span color=\"#FFFFFF\"><b>IDS??</b></span>";
			var parsed = RDLParse.Parse(infocard);
			var builder = new StringBuilder();
			foreach (var node in parsed.Nodes)
			{
				if (node is InfocardParagraphNode)
				{
					builder.AppendLine();
					continue;
				}
				var mk = (InfocardTextNode)node;
				if (mk.Bold) builder.Append("<b>");
				if (mk.Italic) builder.Append("<i>");
				if (mk.Underline) builder.Append("<u>");
				builder.AppendFormat("<span color=\"{0}\">", getHexColor(mk.Color));
				builder.Append(mk.Contents);
				builder.Append("</span>");
				if (mk.Underline) builder.Append("</u>");
				if (mk.Italic) builder.Append("</i>");
				if (mk.Bold) builder.Append("</b>");
			}
			return builder.ToString();
		}

		static string getHexColor(Color4 c)
		{
			var r = (byte)(c.R * 255);
			var g = (byte)(c.G * 255);
			var b = (byte)(c.B * 255);

			return string.Format("#{0:X2}{1:X2}{2:X2}", r, g, b);
		}
	}
}
