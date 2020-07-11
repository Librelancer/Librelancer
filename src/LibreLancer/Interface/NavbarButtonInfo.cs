namespace LibreLancer.Interface
{
    [MoonSharp.Interpreter.MoonSharpUserData]
    public class NavbarButtonInfo
    {
        public string IDS;
        public string IconName;

        public NavbarButtonInfo(string ids, string iconName)
        {
            IDS = ids;
            IconName = iconName;
        }
    }
}