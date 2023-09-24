using Xunit;

namespace LibreLancer.Tests;

public class IdPoolTests
{
    [Fact]
    public void CanEnumerate()
    {
        var pool = new IdPool(4, false);
        for (int i = 0; i < 128; i++)
        {
            pool.TryAllocate(out _);
        }
        var ba = new BitArray128();
        foreach (var allocated in pool.GetAllocated())
        {
            ba[allocated] = true;
        }
        Assert.True(ba.All());
    }
}
