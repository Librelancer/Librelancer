// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Utf.Ale
{
	public class AleParameter
	{
		public AleProperty Name;
		public object? Value;
		public AleParameter ()
		{
		}

        public AleParameter(AleProperty name, object value)
        {
            Name = name;
            Value = value;
        }

		public override string ToString ()
		{
			return $"[{Name}: {Value}]";
		}
	}
}

