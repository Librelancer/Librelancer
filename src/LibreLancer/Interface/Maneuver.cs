// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer
{
    [WattleScript.Interpreter.WattleScriptUserData]
    public class Maneuver
	{
		public string Action;
		public string InfocardA;
		public string InfocardB;
		public string ActiveModel;
		public string InactiveModel;
	}
}
