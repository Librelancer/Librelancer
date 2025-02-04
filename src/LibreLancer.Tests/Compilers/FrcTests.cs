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

        Assert.Equal(expected: WrapInfocard("<TRA bold=\"true\"/><TEXT>Bold </TEXT><TRA bold=\"false\"/><TEXT>Not Bold</TEXT>"),
            actual: resourceDll.Infocards[1]);
        Assert.Equal(expected: WrapInfocard("<TEXT>MultiLine</TEXT><PARA/><TEXT>String</TEXT>"), actual: resourceDll.Infocards[2]);
        Assert.Equal(expected: WrapInfocard("<TEXT>Two</TEXT><PARA/><TEXT>Lines</TEXT>"), actual: resourceDll.Infocards[3]);
        Assert.Equal(expected: WrapInfocard("<TEXT>One Line</TEXT>"), actual: resourceDll.Infocards[4]);
        Assert.Equal(expected: WrapInfocard("<TEXT> Spacing </TEXT>"), actual: resourceDll.Infocards[5]);
        Assert.Equal(expected: WrapInfocard("<TEXT>Separate Line</TEXT>"), actual: resourceDll.Infocards[6]);
        Assert.Equal(expected: WrapInfocard("<TEXT>“Read My Quote”</TEXT>"), actual: resourceDll.Infocards[7]);
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

    [Fact]
    public void LeadingCommentsShouldBeIgnored()
    {
        FrcCompiler compiler = new FrcCompiler();
        const string input = @"; S 1 Some Text\nS 2 Some Text";
        var resourceDll = compiler.Compile(input, "TEST");

        Assert.DoesNotContain(resourceDll.Strings, x => x.Key == 1);
    }

    [Fact]
    public void TrailingCommentsShouldBeIgnored()
    {
        FrcCompiler compiler = new FrcCompiler();
        const string input = @"S 1 Some Text ; Comment";
        var resourceDll = compiler.Compile(input, "TEST");

        Assert.Contains(resourceDll.Strings, x => x.Value == "Some Text");
    }

    [Fact]
    public void BlockCommentsShouldBeIgnored()
    {
        FrcCompiler compiler = new FrcCompiler();
        const string input = @";+ S 1 Some Text ; Comment\nsomsada\n\nasdasd ;-";
        var resourceDll = compiler.Compile(input, "TEST");

        Assert.Empty(resourceDll.Strings);
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
        { @"I 1 \cr", "<TRA color=\"#FF0000\"/>" },
        { @"I 1 \clr", "<TRA color=\"#C00000\"/>" },
        { @"I 1 \chg", "<TRA color=\"#008000\"/>" },
        { @"I 1 \cdw", "<TRA color=\"#404040\"/>" },
        { """I 1 \<TRA bold="true"/>""", "<TRA bold=\"true\"/>" },
        { @"I 1 <>&", "<TEXT>&lt;&gt;&amp;</TEXT>" },
    };

    private string WrapInfocard(string infocard) => $"{FrcCompiler.InfocardStart}{infocard}{FrcCompiler.InfocardEnd}";
}
