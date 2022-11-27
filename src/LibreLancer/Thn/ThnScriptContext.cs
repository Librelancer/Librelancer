// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Client.Components;
using LibreLancer.World;

namespace LibreLancer.Thn
{
    public class ThnScriptContext
    {
        public ThnScript SetScript;
        public GameObject PlayerShip;
        public CEngineComponent PlayerEngine;
        public GameObject MainObject;
        public Dictionary<string,string> Substitutions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public ThnScriptContext(ThnScript set)
        {
            SetScript = set;
        }
    }
}