using System.IO;
using System.Linq;
using LibreLancer.ContentEdit.Frc;
using LibreLancer.ContentEdit.RandomMissions;
using Xunit;

namespace LibreLancer.Tests.Compilers;

public class FrcTests
{
    [Fact]
    public void ValidFrcStringsShouldReturnAValidResourceDll()
    {
        const string fileName = "Compilers/FrcFiles/ValidFrcStrings.frc";
        var strings = File.ReadAllText(fileName);

        FrcCompiler compiler = new FrcCompiler();
        var resourceDll = compiler.Compile(strings, fileName);

        Assert.NotNull(resourceDll);

        Assert.Equal(expected: "File Starts Here", actual: resourceDll.Strings[1]);
        Assert.Equal(expected: "MultiLine\nString", actual: resourceDll.Strings[2]);
        Assert.Equal(expected: "Two\nLines", actual: resourceDll.Strings[3]);
        Assert.Equal(expected: "One Line", actual: resourceDll.Strings[4]);
        Assert.Equal(expected: " Spacing ", actual: resourceDll.Strings[5]);
        Assert.Equal(expected: "Separate Line", actual: resourceDll.Strings[6]);
        Assert.Equal(expected: "“Read My Quote”", actual: resourceDll.Strings[7]);
    }

    [Fact]
    public void ValidFrcInfocardsShouldReturnAValidResourceDll()
    {
        const string fileName = "Compilers/FrcFiles/ValidFrcInfocards.frc";
        var strings = File.ReadAllText(fileName);

        FrcCompiler compiler = new FrcCompiler();
        var resourceDll = compiler.Compile(strings, fileName);

        Assert.NotNull(resourceDll);

        Assert.Equal(expected: WrapInfocard("<TRA bold=\"true\"/>Bold <TRA bold=\"false\"/>Not Bold"),
            actual: resourceDll.Infocards[1]);
        Assert.Equal(expected: WrapInfocard("MultiLine<PARA/>String"), actual: resourceDll.Infocards[2]);
        Assert.Equal(expected: WrapInfocard("Two<PARA/>Lines"), actual: resourceDll.Infocards[3]);
        Assert.Equal(expected: WrapInfocard("One Line"), actual: resourceDll.Infocards[4]);
        Assert.Equal(expected: WrapInfocard(" Spacing "), actual: resourceDll.Infocards[5]);
        Assert.Equal(expected: WrapInfocard("Separate Line"), actual: resourceDll.Infocards[6]);
        Assert.Equal(expected: WrapInfocard("“Read My Quote”"), actual: resourceDll.Infocards[7]);
    }

    [Theory]
    [MemberData(nameof(ValidInfocardFrcStrings))]
    public void ValidFrcStringsShouldTransformToTheCorrectXml(string input, string expected)
    {
        FrcCompiler compiler = new FrcCompiler();
        var resourceDll = compiler.Compile(input, "TEST");

        Assert.NotNull(resourceDll);
        Assert.Equal(expected: WrapInfocard(expected), actual: resourceDll.Infocards[1]);
    }

    [Theory]
    [InlineData("white")]
    [InlineData("ReD")]
    [InlineData("Black")]
    [InlineData("DarkBlue")]
    public void BadColoursShouldThrowExceptions(string colour)
    {
        FrcCompiler compiler = new FrcCompiler();
        Assert.Throws<CompileErrorException>(() =>
        {
            _ = compiler.Compile($"I 1 \\c{colour} ", "TEST");
        });
    }

    [Theory]
    [InlineData("1000")]
    [InlineData("-12")]
    [InlineData("0")]
    public void BadHeightShouldThrowExceptions(string height)
    {
        FrcCompiler compiler = new FrcCompiler();
        Assert.Throws<CompileErrorException>(() =>
        {
            _ = compiler.Compile($"I 1 \\h{height}", "TEST");
        });
    }

    [Theory]
    [InlineData("100")]
    [InlineData("-12")]
    [InlineData("0")]
    public void BadFontShouldThrowExceptions(string font)
    {
        FrcCompiler compiler = new FrcCompiler();
        Assert.Throws<CompileErrorException>(() => { _ = compiler.Compile($"I 1 \\f{font}", "TEST"); });
    }

    [Theory]
    [InlineData(1, 1, -1)]
    [InlineData(65537, 1, 1)]
    [InlineData(131071, 65535, 1)]
    [InlineData(131071, 65535, -1)]
    public void FrcShouldAllowAbsoluteIndexes(int absoluteId, int mappedId, int index)
    {
        var input = $"S {absoluteId} some text";

        FrcCompiler compiler = new FrcCompiler();
        var resourceDll = compiler.Compile(input, "TEST", index);

        Assert.Contains(resourceDll.Strings, x => x.Key == mappedId);
    }

    [Fact]
    public void BadResourceIndexShouldThrowExceptions()
    {
        FrcCompiler compiler = new FrcCompiler();
        Assert.Throws<CompileErrorException>(() => { _ = compiler.Compile($"I 131071 EEE", "TEST", 0); });
    }

    public static TheoryData<string, string> ValidInfocardFrcStrings { get; } = new()
    {
        { @"I 1 \b", "<TRA bold=\"true\"/>" },
        { @"I 1 \B", "<TRA bold=\"false\"/>" },
        { @"I 1 \f12", "<TRA font=\"12\"/>" },
        { @"I 1 \F", "<TRA font=\"default\"/>" },
        { @"I 1 \i", "<TRA italic=\"true\"/>" },
        { @"I 1 \I", "<TRA italic=\"false\"/>" },
        { @"I 1 \u", "<TRA underline=\"true\"/>" },
        { @"I 1 \U", "<TRA underline=\"false\"/>" },
        { @"I 1 \n", "<PARA/>" },
        { @"I 1 \r", "<JUST loc=\"r\"/>" },
        { @"I 1 \m", "<JUST loc=\"c\"/>" },
        { @"I 1 \l", "<JUST loc=\"l\"/>" },
        { @"I 1 \h150", "<POS h=\"150\" relH=\"true\"/>" },
        { @"I 1 \C", "<TRA color=\"default\"/>" },
        { @"I 1 \cWhite", "<TRA color=\"white\"/>" },
        { @"I 1 \cFFFFFF", "<TRA color=\"#FFFFFF\"/>" },
    };

    private string WrapInfocard(string infocard) => $"{FrcCompiler.InfocardStart}{infocard}{FrcCompiler.InfocardEnd}";
}
