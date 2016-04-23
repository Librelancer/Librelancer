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
using System.Collections.Generic;
using System.IO;
using System.Xml;
using LibreLancer.Compatibility;
namespace LibreLancer.Dll
{
    public class DllFile
    {
		public string Name;
		ManagedDllProvider provider;

        public DllFile(string path)
        {
			
            if (path == null) 
				throw new ArgumentNullException("path");
			Name = Path.GetFileName (path);
            using (var file = VFS.Open(path))
            {
                provider = new ManagedDllProvider(file);
            }
        }

		public Dictionary<int,string> Strings {
			get {
				return provider.Strings;
			}
		}

		public Dictionary<int, XmlDocument> Infocards {
			get {
				return provider.Infocards;
			}
		}
    }
}