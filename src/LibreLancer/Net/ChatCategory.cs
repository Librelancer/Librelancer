namespace LibreLancer.Net;

public enum ChatCategory
{
    Local,
    System,
    Console,
    MAX
}

public static class ChatCategoryExtensions
{
    public static Color4 GetColor(this ChatCategory category)
    {
        switch (category)
        {
            case ChatCategory.Local:
                return Color4.CornflowerBlue;
            case ChatCategory.Console:
                return Color4.Green;
            case ChatCategory.System:
                return Color4.LightBlue;
            default:
                return Color4.White;
        }
    }
}