using System.Collections.Generic;
using System.Linq;

namespace LibreLancer.ContentEdit;

public class EditorResult<T>
{
    public T Result;
    public List<EditorMessage> Messages = new List<EditorMessage>();
    
    public EditorResult(T result)
    {
        Result = result;
    }
    
    public static EditorResult<T> FromMessages(params EditorMessage[] messages) =>
        new(default(T)) {  Messages = new List<EditorMessage>(messages) };
    public static EditorResult<T> FromMessages(IEnumerable<EditorMessage> messages) =>
        new(default(T)) {  Messages = new List<EditorMessage>(messages) };

    public bool IsError => Messages.Any(x => x.Kind == EditorMessageKind.Error);
    
    public bool IsSuccess => Messages.All(x => x.Kind != EditorMessageKind.Error);
}