// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{

    [WattleScriptUserData]
    public class LuaCompatibleDictionary
    {
        public Dictionary<string, object> Storage = new Dictionary<string, object>();
        public object Get(string key) => Storage[key];
        public void Set(string key, object value) => Storage[key] = value;
    }
}