// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Interface.Reflection;

namespace LibreLancer.Interface
{
    internal class UiRecreateHandle
    {
        internal UiLoadedObject loaded;

        internal UiRecreateHandle(UiLoadedObject loaded)
        {
            this.loaded = loaded;
        }
        public void Refill(object o)
        {
            loaded.Fill(o);
        }
    }
}