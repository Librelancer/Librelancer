using System;
using LibreLancer.ContentEdit;
using LibreLancer.Data;
using LibreLancer.Data.Dll;
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
    public void VariantOutOfRangeFaction()
    {
        var dll = new ResourceDll();
        dll.Strings[5] = "Liberty Rogues";
        dll.Strings[6] = "Rogues";

        var result = IdsFormatting.Format("There is a %F0v1 base out there.",
            new InfocardManager([dll]), new IdsFormatItem('F', 0, 5));

        Assert.Equal("There is a Rogues base out there.", result);
    }

    static EditableInfocardManager EmptyInfocards() => new EditableInfocardManager([
        new ResourceDll(), new ResourceDll(), new ResourceDll(), new ResourceDll(), new ResourceDll(),
        new ResourceDll()]);


    [Fact]
    public void VariantInRangeFaction()
    {
        var ic = EmptyInfocards();
        ic.SetStringResource(196893, "Testers"); //F
        ic.SetStringResource(328727, "Testers"); //F0v0
        ic.SetStringResource(328827, "the Testers");
        ic.SetStringResource(328927, "Tester");
        ic.SetStringResource(329027, "The Testers");

        var fmt = "%F0v3 have been bad. Send a message to %F0v1 by blowing up a %F0v2 ship.";
        var exp = "The Testers have been bad. Send a message to the Testers by blowing up a Tester ship.";
        var result = IdsFormatting.Format(fmt, ic, new IdsFormatItem('F', 0, 196893));
        Assert.Equal(exp, result);
    }

    [Fact]
    public void VariantInRangeZone()
    {
        var ic = EmptyInfocards();
        ic.SetStringResource(261208, "Big City Zone");
        ic.SetStringResource(331681, "Big City Zone");
        ic.SetStringResource(331881, "the Big City Zone");
        var result = IdsFormatting.Format("Go to %Z0v1.",
            ic, new IdsFormatItem('Z', 0, 261208));
        Assert.Equal("Go to the Big City Zone.", result);
    }

    [Fact]
    public void VariantOutOfRangeZone()
    {
        var ic = EmptyInfocards();
        ic.SetStringResource(12, "Big City Zone");
        ic.SetStringResource(13, "the Big City Zone");
        var result = IdsFormatting.Format("Go to %Z0v1.",
            ic, new IdsFormatItem('Z', 0, 12));
        Assert.Equal("Go to the Big City Zone.", result);
    }

    [Fact]
    public void RemovesM()
    {
        var result = IdsFormatting.Format("Hello%M", null, []);
        Assert.Equal("Hello", result);
    }

}
