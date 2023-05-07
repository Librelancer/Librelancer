// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using LibreLancer.Dll;

namespace LibreLancer.Data
{
	public class InfocardManager
	{
        public List<ResourceDll> Dlls;
		public InfocardManager (List<ResourceDll> res)
        {
            Dlls = res ?? new List<ResourceDll>();
        }

        protected virtual IEnumerable<KeyValuePair<int, string>> IterateStrings()
        {
            for (int i = 0; i < Dlls.Count; i++) {
                foreach (var str in Dlls[i].Strings.OrderBy(x => x.Key)) {
                    yield return new KeyValuePair<int, string>(i * 65536 + str.Key, str.Value);
                }
            }
        }
        protected virtual IEnumerable<KeyValuePair<int, string>> IterateXml()
        {
            for (int i = 0; i < Dlls.Count; i++) {
                foreach (var info in Dlls[i].Infocards.OrderBy(x => x.Key)) {
                    yield return new KeyValuePair<int, string>(i * 65536 + info.Key, info.Value);
                }
            }
        }

        public IEnumerable<int> StringIds => IterateStrings().Select(x => x.Key);
        public IEnumerable<int> InfocardIds => IterateXml().Select(x => x.Key);

        public IEnumerable<KeyValuePair<int, string>> AllStrings => IterateStrings();
        public IEnumerable<KeyValuePair<int, string>> AllXml => IterateXml();

        protected HashSet<int> MissingStrings = new HashSet<int>();
        protected HashSet<int> MissingXml = new HashSet<int>();
        
        public virtual string GetStringResource(int id)
		{
            if (id <= 0) return "";
            var (x, y) = (id >> 16, id & 0xFFFF);
            if (x < Dlls.Count && Dlls[x].Strings.TryGetValue(y, out var s))
            {
                return s;
            } 
            else {
                if (!MissingStrings.Contains(id))
                {
                    FLLog.Warning("Strings", "Not Found: " + id);
                    MissingStrings.Add(id);
                }
				return "";
			}
		}


		public virtual string GetXmlResource(int id)
		{
            if (id <= 0) return null;
            var (x, y) = (id >> 16, id & 0xFFFF);
			if (x < Dlls.Count && Dlls[x].Infocards.TryGetValue(y, out var s))
            {
                return s;
            } 
            else {
                if (!MissingXml.Contains(id))
                {
                    FLLog.Warning("Infocards", "Not Found: " + id);
                    MissingXml.Add(id);
                }
				return null;
			}
		}
	}
}

