using System;
using LibreLancer.ContentEdit;
using LibreLancer.Data;
using LibreLancer.Dll;
using LibreLancer.Interface;
using Xunit;

namespace LibreLancer.Tests;

public class IdsFormattingTests
{
    [Fact]
    public void CanFormatSingle()
    {
        var result = IdsFormatting.Format("Item: %s0", null, new IdsFormatItem('s', 0, "Hello"));
        Assert.Equal("Item: Hello", result);
    }

    [Fact]
    public void CanFormatMultiple()
    {
        var result = IdsFormatting.Format(
            "Item: %s0 %s1",
            null,
            new IdsFormatItem('s', 0, "Hello"),
            new IdsFormatItem('s', 1, "World")
            );
        Assert.Equal("Item: Hello World", result);
    }

    [Fact]
    public void CanFormatOutOfOrder()
    {
        var result = IdsFormatting.Format("%s0%s1 %d0%d1",
            null,
            new IdsFormatItem('s', 0, "hello"),
            new IdsFormatItem('d', 0, "123"),
            new IdsFormatItem('s', 1, "world"),
            new IdsFormatItem('d', 1, "456"));
        Assert.Equal("helloworld 123456", result);
    }

    [Fact]
    public void CanEscape()
    {
        var result = IdsFormatting.Format("%%s0", null, new IdsFormatItem('s', 0, "bad string"));
        Assert.Equal("%s0", result);
    }

    [Fact]
    public void CanFindVariant()
    {
        var dll = new ResourceDll();
        dll.Strings[5] = "Liberty Rogues";
        dll.Strings[6] = "Rogues";

        var result = IdsFormatting.Format("There is a %F0v1 base out there.",
            new InfocardManager([dll]), new IdsFormatItem('F', 0, 5));

        Assert.Equal("There is a Rogues base out there.", result);
    }



    [Fact]
    public void RemovesM()
    {
        var result = IdsFormatting.Format("Hello%M", null, []);
        Assert.Equal("Hello", result);
    }

}
