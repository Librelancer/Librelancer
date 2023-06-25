namespace LibreLancer.Server;

public class ServerPerformance
{
    public const int MAX_TIMING_ENTRIES = 6 * 60;
    public CircularBuffer<float> Timings = new (MAX_TIMING_ENTRIES);
    private IUIThread thread;
    public ServerPerformance(IUIThread thread)
    {
        this.thread = thread;
    }
    public void AddEntry(float f) => thread.QueueUIThread(() => Timings.Enqueue(f));
}