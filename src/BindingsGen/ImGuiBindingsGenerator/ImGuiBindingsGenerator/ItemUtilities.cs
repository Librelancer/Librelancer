namespace ImGuiBindingsGenerator;

public class ItemUtilities
{
    public static string FixIdentifier(string identifier)
    {
        if (identifier == "ref")
            return "reference";
        if (identifier == "out")
            return "output";
        if (identifier == "in")
            return "input";
        if (int.TryParse(identifier, out _))
            return "D" + identifier;
        return identifier;
    }
}