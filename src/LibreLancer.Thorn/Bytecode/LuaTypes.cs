// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Thorn.Bytecode
{
    enum LuaTypes
	{
		UserData = 0,
		Number = -1,
		String = -2,
		Array = -3,
		Proto = -4, //lua functions
		CProto = -5, //c functions
		Nil = -6, // last pre-defined tag
		Closure = -7,
		ClMark = -8, //closure mark
		PMark = -9, //lua prototype mark
		CMark = -10, //c prototype mark
		Line = -11
	}
}

