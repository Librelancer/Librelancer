using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LibreLancer;

public class LoadingTasks
{
    private List<Task> allTasks = new List<Task>();
    public Task Begin(Action loader, params Task[] dependencies)
    {
        var t = Task.Run(async () =>
        {
            if (dependencies is {Length: > 0})
                await Task.WhenAll(dependencies);
            loader();
        });
        allTasks.Add(t);
        return t;
    }
    public void WaitAll()
    {
        Task.WaitAll(allTasks.ToArray());
    }
}