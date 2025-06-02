using Xunit;

namespace LibreLancer.Tests;

public class MathHelperTests
{
    [Fact]
    public void ClampF()
    {
        Assert.Equal(3f, MathHelper.Clamp(2f,3f,5f));
        Assert.Equal(4f, MathHelper.Clamp(4f,3f,5f));
        Assert.Equal(5f, MathHelper.Clamp(9f,3f,5f));

    }
}
