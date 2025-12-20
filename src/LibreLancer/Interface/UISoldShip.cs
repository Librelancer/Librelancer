// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Data.GameData;
using LibreLancer.Net;
using LibreLancer.Net.Protocol;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [WattleScriptUserData]

    public class UISoldShip
    {
        public int IdsName;
        public int IdsInfo;
        public int ShipClass;
        public string Icon;
        public string Model;
        public double Price;
        [WattleScriptHidden]
        public NetSoldShip Server;
        [WattleScriptHidden]
        public Ship Ship;
    }
}
