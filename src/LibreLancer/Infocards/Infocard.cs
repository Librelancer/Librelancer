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
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.Text;
using System.Collections.Generic;
namespace LibreLancer.Infocards
{
	public class Infocard
	{
		public List<InfocardNode> Nodes;

		public string ExtractText()
		{
			var b = new StringBuilder();
			foreach (var n in Nodes)
			{
				if (n is InfocardParagraphNode) b.AppendLine();
				if (n is InfocardTextNode) b.Append((n as InfocardTextNode).Contents);
			}
			return b.ToString();
		}

		public override string ToString()
		{
			return ExtractText();
		}
	}
}

