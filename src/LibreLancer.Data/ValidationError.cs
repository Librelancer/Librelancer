using System;

namespace LibreLancer.Data;

public record struct ValidationError(ValidationSeverity Severity, string Message)
{
    public static ValidationError Warning(string message) => new ValidationError
        {Severity = ValidationSeverity.Warning, Message = message};

    public static ValidationError Error(string message) => new ValidationError
        {Severity = ValidationSeverity.Error, Message = message};
}

public enum ValidationSeverity
{
    Error,
    Warning
}
