using System.Collections.Concurrent;

namespace LibreLancer.Server;

public class ServerPerformance
{
    public const int MAX_TIMING_ENTRIES = 6 * 60;
    public CircularBuffer<float> Timings = new (MAX_TIMING_ENTRIES);

    private readonly ConcurrentQueue<float> pending = new();

    public void AddEntry(float f)
    {
        pending.Enqueue(f);
    }

    public void Update()
    {
        // UI thread: drain queue
        while (pending.TryDequeue(out var value))
            Timings.Enqueue(value);
    }
}
