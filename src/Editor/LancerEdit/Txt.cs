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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Globalization;
using System.Collections.Generic;
using System.Xml.Linq;
namespace LancerEdit
{
	static class Txt
	{
		public static string Override = null;

		static Dictionary<string, string> translations;

		static bool Loaded = false;

		static void Load()
		{
			var clt = Override == null ? CultureInfo.CurrentUICulture : new CultureInfo(Override);
			var a = typeof(Txt).Assembly;
			var streams = a.GetManifestResourceNames();
			while (true)
			{
				var n = "LancerEdit.I18N." + clt.Name + ".xml";
				if (streams.Contains(n))
				{
					translations = new Dictionary<string, string>();
					var t = XElement.Load(a.GetManifestResourceStream(n));
					foreach (var child in t.Descendants())
					{
						var k = child.Attribute(XName.Get("key")).Value;
						var v = child.Attribute(XName.Get("val")).Value;
						translations.Add(k, v);
					}
					break;
				}
				clt = clt.Parent;
				if (clt == CultureInfo.InvariantCulture) break;
			}
			Loaded = true;
		}

		public static string _(string x)
		{
			if (!Loaded) Load();
			if (translations == null) return x;
			string o;
			if (translations.TryGetValue(x, out o)) return o;
			return x;
		}
	}
}
