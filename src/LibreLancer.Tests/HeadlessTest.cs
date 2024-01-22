using System.IO;
using LancerEdit;
using LibreLancer.ContentEdit;
using Xunit;

namespace LibreLancer.Tests;

public class HeadlessTest
{
    [Fact]
    public void ShouldLaunchWithUtfOpen()
    {
        var tempFile = Path.GetTempFileName();
        var emptyUtf = new EditableUtf();
        emptyUtf.Root.Children.Add(new LUtfNode() { Name="yoohoo", Parent = emptyUtf.Root, StringData = "HELLO WORLD!"});
        var result = emptyUtf.Save(tempFile, 0);
        Assert.True(result.IsSuccess, result.AllMessages());
        int tickNo = 0;
        LancerEdit.MainWindow window = null;
        var config = GameConfiguration.HeadlessTest()
            .WithTick(() =>
            {
                File.Delete(tempFile);
                tickNo++;
                Assert.True(window.TabControl.Tabs.Count == 1);
                window.Exit();
            })
            .WithMaxIterations(1000);
        window = new MainWindow(config) { InitOpenFile = new[] { tempFile } };
        window.Run();
        //Did it run at all?
        Assert.True(tickNo > 0);
    }
}
