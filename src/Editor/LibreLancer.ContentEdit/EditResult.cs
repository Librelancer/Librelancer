using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LibreLancer.ContentEdit;

public static class EditResultExtensions
{
    public static EditResult<T> AsResult<T>(this T self) => new(self);
}

public class EditResult<T>
{
    public T Data;
    public List<EditMessage> Messages = new List<EditMessage>();

    public EditResult(T data)
    {
        Data = data;
    }
    public EditResult(T data, IEnumerable<EditMessage> messages)
    {
        Data = data;
        Messages = new List<EditMessage>(messages);
    }

    public string AllMessages() => string.Join("; ", Messages.Select(x => x.Message));

    public static EditResult<T> Error(params string[] errors) =>
        new(default, errors.Select(EditMessage.Error));

    public static EditResult<T> Error(string error, IEnumerable<EditMessage> other) =>
        new(default, other.Append(EditMessage.Error(error)));

    public bool IsError => Messages.Any(x => x.Kind == EditorMessageKind.Error);

    public bool IsSuccess => Messages.All(x => x.Kind != EditorMessageKind.Error);

    public EditResult<TNext> Then<TNext>(Func<EditResult<T>, EditResult<TNext>> func)
    {
        if (IsSuccess)
        {
            var x = func(this);
            return new(x.Data, Messages.Concat(x.Messages));
        }
        return new(default, Messages);
    }

    public static EditResult<(T, TOther)> Merge<TOther>(EditResult<T> self, EditResult<TOther> other)
    {
        return new((self.Data, other.Data), self.Messages.Concat(other.Messages).ToList());
    }

    public static async Task<EditResult<T>> RunBackground(Func<EditResult<T>> func, CancellationToken cancellation = default)
    {
        try
        {
            return await Task.Run(func, cancellation);
        }
        catch (TaskCanceledException)
        {
            return Error("Operation cancelled");
        }
    }

    public static EditResult<T> TryCatch(Func<T> create)
    {
        try
        {
            return new EditResult<T>(create());
        }
        catch (Exception e)
        {
            return Error(e.ToString());
        }
    }

    public override string ToString()
    {
        return AllMessages();
    }
}
