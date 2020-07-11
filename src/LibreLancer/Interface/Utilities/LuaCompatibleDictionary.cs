// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;

namespace LibreLancer.Interface
{
    //This class exists to provide the Get method
    //Using indexers with NLua is glacial so this stops that from happening
    [MoonSharp.Interpreter.MoonSharpUserData]
    public class LuaCompatibleDictionary<TKey, TValue>
    {
        public Dictionary<TKey, TValue> Storage = new Dictionary<TKey, TValue>();
        public TValue Get(TKey key) => Storage[key];
        public void Set(TKey key, TValue value) => Storage[key] = value;
    }
}