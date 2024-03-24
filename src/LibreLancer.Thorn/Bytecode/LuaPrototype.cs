// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Thorn.Bytecode
{
	class LuaPrototype
	{
		public int LinesDefined;
		public string Source;
		public byte[] Code;
		public LuaLocal[] Locals;
		public LuaObject[] Constants;
		public string DescriptionText ()
		{
			return "Lines Defined: " + LinesDefined + "\n" +
			"Source: " + Source + "\n" +
			"Code Length: " + Code.Length + "\n" +
				"Locals: \n" + GetLocalsString () + "\n" +
				"Constants: \n" + GetConstantsString () + "\n";
		}
		string GetLocalsString()
		{
			if (Locals == null)
				return "\tNone";
			string s = "";
			for (int i = 0; i < Locals.Length; i++) {
				s += Locals [i].ToStringIndented () + "\n";
			}
			return s;
		}
		string GetConstantsString()
		{
			if (Constants == null)
				return "";
			string s = "";
			for (int i = 0; i < Constants.Length; i++) {
				s += Constants [i].ToStringIndented () + "\n";
			}
			return s;
		}
	}
}

