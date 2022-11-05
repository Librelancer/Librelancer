// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Ini
{
	public interface IValue
	{
		bool ToBoolean();
		int ToInt32();
        bool TryToInt32(out int result);
        long ToInt64();
        float ToSingle(string propertyName = null);
		StringKeyValue ToKeyValue();
	}
}