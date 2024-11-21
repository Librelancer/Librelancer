namespace LibreLancer.Data;

public struct ValidationError
{
    public ValidationSeverity severity;
    public string message;
}

public enum ValidationSeverity
{
    Error,
    Warning
}
