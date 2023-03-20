using System.Runtime.CompilerServices;

namespace LibreLancer.ContentEdit;

public enum EditorMessageKind
{
    Warning,
    Error
}

public class EditorMessage
{
    public EditorMessageKind Kind;
    public string Message;
    
    public static EditorMessage Error(string msg) => new() {Kind = EditorMessageKind.Error, Message = msg};
    public static EditorMessage Warning(string msg) => new() {Kind = EditorMessageKind.Warning, Message = msg};
}