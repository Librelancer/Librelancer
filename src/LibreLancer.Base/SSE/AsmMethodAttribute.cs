// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer
{
    class AsmMethodAttribute : Attribute
    {
        public string UnixName;
        public string WindowsName;
		public string X86Name;
        public AsmMethodAttribute(string unixname, string windowsname, string x86name)
        {
            UnixName = unixname;
            WindowsName = windowsname;
			X86Name = x86name;
        }
    }
}
