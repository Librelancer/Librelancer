using System.Runtime.CompilerServices;

namespace LibreLancer.ContentEdit;

public enum EditorMessageKind
{
    Warning,
    Error
}

public class EditMessage
{
    public EditorMessageKind Kind;
    public string Message;
    
    public static EditMessage Error(string msg) => new() {Kind = EditorMessageKind.Error, Message = msg};
    public static EditMessage Warning(string msg) => new() {Kind = EditorMessageKind.Warning, Message = msg};
}