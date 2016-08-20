using System;

namespace LibreLancer
{
    class AsmMethodAttribute : Attribute
    {
        public string UnixName;
        public string WindowsName;
        public AsmMethodAttribute(string unixname, string windowsname)
        {
            UnixName = unixname;
            WindowsName = windowsname;
        }
    }
}
