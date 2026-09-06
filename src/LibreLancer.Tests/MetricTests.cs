using LibreLancer.Interface;
using Xunit;

namespace LibreLancer.Tests;

public class MetricTests
{
    [Fact]
    public void ShouldParseMetrics()
    {
        Assert.Equal(new Metric(MetricUnit.Point, 10, 0), Metric.Parse("10"));
        Assert.Equal(new Metric(MetricUnit.Percent, 0.5f, 0), Metric.Parse("50%"));
        Assert.Equal(new Metric(MetricUnit.Percent, 0.5f, 10), Metric.Parse("50% + 10"));
        Assert.Equal(new Metric(MetricUnit.Percent, 0.5f, -10), Metric.Parse("50% - 10"));
        Assert.Equal(new Metric(MetricUnit.PercentWidth, 0.5f, 5), Metric.Parse("50%w + 5"));
        Assert.Equal(new Metric(MetricUnit.PercentHeight, 0.255f, -3.25f), Metric.Parse("25.5%h - 3.25"));
    }
}
