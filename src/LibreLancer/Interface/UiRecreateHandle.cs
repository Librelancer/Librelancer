// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Xml.Linq;

namespace LibreLancer.Interface
{
    internal class UiRecreateHandle
    {
        internal UiXmlLoader Loader;
        internal XElement Element;
        internal object Object;
        public void Refill()
        {
            Loader.ReinitObject(Object, Element);
        }
    }
}