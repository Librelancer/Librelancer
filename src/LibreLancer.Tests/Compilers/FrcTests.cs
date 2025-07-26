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

        var resourceDll = FrcCompiler.Compile(strings, fileName);

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

        var resourceDll = FrcCompiler.Compile(strings, fileName);

        Assert.NotNull(resourceDll);

        Assert.Equal(expected: WrapInfocard("<TRA bold=\"true\"/><TEXT>Bold </TEXT><TRA bold=\"false\"/><TEXT>Not Bold</TEXT><PARA/>"),
            actual: resourceDll.Infocards[1]);
        Assert.Equal(expected: WrapInfocard("<TEXT>MultiLine</TEXT><PARA/><TEXT>String</TEXT><PARA/>"), actual: resourceDll.Infocards[2]);
        Assert.Equal(expected: WrapInfocard("<TEXT>Two</TEXT><PARA/><TEXT>Lines</TEXT><PARA/>"), actual: resourceDll.Infocards[3]);
        Assert.Equal(expected: WrapInfocard("<TEXT>One Line</TEXT><PARA/>"), actual: resourceDll.Infocards[4]);
        Assert.Equal(expected: WrapInfocard("<TEXT> Spacing </TEXT>"), actual: resourceDll.Infocards[5]);
        Assert.Equal(expected: WrapInfocard("<TEXT>Separate Line</TEXT><PARA/>"), actual: resourceDll.Infocards[6]);
        Assert.Equal(expected: WrapInfocard("<TEXT>“Read My Quote”</TEXT><PARA/>"), actual: resourceDll.Infocards[7]);
    }

    [Theory]
    [MemberData(nameof(ValidInfocardFrcStrings))]
    public void ValidFrcStringsShouldTransformToTheCorrectXml(string input, string expected)
    {
        var resourceDll = FrcCompiler.Compile(input, "TEST");

        Assert.NotNull(resourceDll);
        Assert.Equal(expected: WrapInfocard(expected), actual: resourceDll.Infocards[1]);
    }

    [Theory]
    [InlineData("ReD")]
    [InlineData("Black")]
    [InlineData("DarkBlue")]
    public void BadColoursShouldThrowExceptions(string colour)
    {
        Assert.Throws<CompileErrorException>(() =>
        {
            _ = FrcCompiler.Compile($"I 1 \\c{colour} ", "TEST");
        });
    }

    [Theory]
    [InlineData("-12")]
    [InlineData("0")]
    public void BadHeightShouldThrowExceptions(string height)
    {
        Assert.Throws<CompileErrorException>(() =>
        {
            _ = FrcCompiler.Compile($"I 1 \\h{height}", "TEST");
        });
    }

    [Theory]
    [InlineData("-12")]
    [InlineData("0")]
    public void BadFontShouldThrowExceptions(string font)
    {
        Assert.Throws<CompileErrorException>(() => { _ = FrcCompiler.Compile($"I 1 \\f{font}", "TEST"); });
    }

    [Theory]
    [InlineData(1, 1, -1)]
    [InlineData(65537, 1, 1)]
    [InlineData(131071, 65535, 1)]
    [InlineData(131071, 65535, -1)]
    public void FrcShouldAllowAbsoluteIndexes(int absoluteId, int mappedId, int index)
    {
        var input = $"S {absoluteId} some text";

        var resourceDll = FrcCompiler.Compile(input, "TEST", index);

        Assert.Contains(resourceDll.Strings, x => x.Key == mappedId);
    }

    [Fact]
    public void BadResourceIndexShouldThrowExceptions()
    {
        Assert.Throws<CompileErrorException>(() => { _ = FrcCompiler.Compile($"I 131071 EEE", "TEST", 0); });
    }

    [Fact]
    public void LeadingCommentsShouldBeIgnored()
    {
        const string input = @"; S 1 Some Text\nS 2 Some Text";
        var resourceDll = FrcCompiler.Compile(input, "TEST");

        Assert.DoesNotContain(resourceDll.Strings, x => x.Key == 1);
    }

    [Fact]
    public void TrailingCommentsShouldBeIgnored()
    {
        const string input = @"S 1 Some Text ; Comment";
        var resourceDll = FrcCompiler.Compile(input, "TEST");

        Assert.Contains(resourceDll.Strings, x => x.Value == "Some Text");
    }

    [Fact]
    public void BlockCommentsShouldBeIgnored()
    {
        const string input = @";+ S 1 Some Text ; Comment\nsomsada\n\nasdasd ;-";
        var resourceDll = FrcCompiler.Compile(input, "TEST");

        Assert.Empty(resourceDll.Strings);
    }

    public static TheoryData<string, string> ValidInfocardFrcStrings { get; } = new()
    {
        { @"I 1 \bxyz", "<TRA bold=\"true\"/><TEXT>xyz</TEXT><PARA/>" },
        { @"I 1 \Bxyz", "<TRA bold=\"false\"/><TEXT>xyz</TEXT><PARA/>" },
        { @"I 1 \f12xyz", "<TRA font=\"12\"/><TEXT>xyz</TEXT><PARA/>" },
        { @"I 1 \f9a\Fxyz", "<TRA font=\"9\"/><TEXT>a</TEXT><TRA font=\"default\"/><TEXT>xyz</TEXT><PARA/>" },
        { @"I 1 \ixyz", "<TRA italic=\"true\"/><TEXT>xyz</TEXT><PARA/>" },
        { @"I 1 \Ixyz", "<TRA italic=\"false\"/><TEXT>xyz</TEXT><PARA/>" },
        { @"I 1 \uxyz", "<TRA underline=\"true\"/><TEXT>xyz</TEXT><PARA/>" },
        { @"I 1 \Uxyz", "<TRA underline=\"false\"/><TEXT>xyz</TEXT><PARA/>" },
        { @"I 1 \nxyz", "<PARA/><TEXT>xyz</TEXT><PARA/>" },
        { @"I 1 \rxyz", "<JUST loc=\"r\"/><TEXT>xyz</TEXT><PARA/>" },
        { @"I 1 \mxyz", "<JUST loc=\"c\"/><TEXT>xyz</TEXT><PARA/>" },
        { @"I 1 \lxyz", "<JUST loc=\"l\"/><TEXT>xyz</TEXT><PARA/>" },
        { @"I 1 \h150xyz", "<POS h=\"150\" relH=\"true\"/><TEXT>xyz</TEXT><PARA/>" },
        { @"I 1 \cWhitea\Cxyz", "<TRA color=\"white\"/><TEXT>a</TEXT><TRA color=\"default\"/><TEXT>xyz</TEXT><PARA/>" },
        { @"I 1 \cWhitexyz", "<TRA color=\"white\"/><TEXT>xyz</TEXT><PARA/>" },
        { @"I 1 \cFFFFFFxyz", "<TRA color=\"white\"/><TEXT>xyz</TEXT><PARA/>" },
        { @"I 1 \cF3AABDxyz", "<TRA color=\"#F3AABD\"/><TEXT>xyz</TEXT><PARA/>"},
        { @"I 1 \crxyz", "<TRA color=\"#FF0000\"/><TEXT>xyz</TEXT><PARA/>" },
        { @"I 1 \clrxyz", "<TRA color=\"#C00000\"/><TEXT>xyz</TEXT><PARA/>" },
        { @"I 1 \chgxyz", "<TRA color=\"#008000\"/><TEXT>xyz</TEXT><PARA/>" },
        { @"I 1 \cdwxyz", "<TRA color=\"#404040\"/><TEXT>xyz</TEXT><PARA/>" },
        { """I 1 \<TRA bold="true"/>""", "<TRA bold=\"true\"/><PARA/>" },
        { @"I 1 <>&", "<TEXT>&lt;&gt;&amp;</TEXT><PARA/>" },
        { @"I 1 \1", "<TEXT>\u2081</TEXT><PARA/>" },
        { @"I 1 \9", "<TEXT>\u2079</TEXT><PARA/>" },
        { @"I 1 Hello\.", "<TEXT>Hello</TEXT>" },
        { @"I 1 \b{Hello} World", "<TRA bold=\"true\"/><TEXT>Hello</TEXT><TRA bold=\"default\"/><TEXT> World</TEXT><PARA/>"},
        { @"I 1 \i\b\uHello World", "<TRA bold=\"true\" italic=\"true\" underline=\"true\"/><TEXT>Hello World</TEXT><PARA/>" }
    };

    private string WrapInfocard(string infocard) => $"{FrcCompiler.InfocardStart}{infocard}{FrcCompiler.InfocardEnd}";
}
