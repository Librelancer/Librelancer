using LibreLancer.Thorn;
using LibreLancer.Thorn.VM;
using Xunit;

namespace LibreLancer.Tests.Thorn;

public class ThornRuntimeTests
{
    object RunString(string str)
    {
        var thorn = new ThornRuntime();
        thorn.SetBuiltins();
        thorn.MaxInstructions = 10_000; //Smaller limit for faster tests
        return thorn.DoString(str);
    }

    [Fact]
    public void TableAccess()
    {
        var result = RunString(@"
    MathTable = {}
    MathTable.pow = pow
    local powstr = ""pow""
    return MathTable[powstr](2,4)
");
        Assert.Equal(16f, result);
    }

    [Fact]
    public void DotAccess()
    {
        var result = RunString(@"
    MathTable = {}
    MathTable.pow = pow

    return MathTable.pow(2,4)
");
        Assert.Equal(16f, result);
    }

    [Fact]
    public void Upvalues()
    {
        var result = RunString(@"
    local a = 5
    local b = 9
    function useupvalues()
        return %b - %a
    end
    local c = useupvalues()
    return c
");
        Assert.Equal(4f, result);
    }

    [Fact]
    public void FibonacciExample()
    {
        //Uses tailcall at the end
        var result = RunString(@"
function fib(n)
	if n<2 then
		return n
	else
		return fib(n-1)+fib(n-2)
	end
end
return fib(7)");

        Assert.Equal(13f, result);
    }

    [Fact]
    public void InfiniteLoopShouldError()
    {
        Assert.Throws<ThornLimitsExceededException>(() => RunString(@"
            local i = 0
            while 1 do
                i = i + 1
            end
        "));
    }

    [Fact]
    public void InfiniteRecursionShouldError()
    {
        Assert.Throws<ThornLimitsExceededException>(() => RunString(@"
            function b()
                print('aaaa')
            end
            function a()
                a()
                b()
            end
            a()
        "));
    }


    enum MyFlags
    {
        A = (1 << 0),
        B = (1 << 1)
    }
    [Fact]
    public void AddEnums()
    {
        var thorn = new ThornRuntime();
        thorn.Env["FlagA"] = MyFlags.A;
        thorn.Env["FlagB"] = MyFlags.B;
        Assert.Equal((float)(MyFlags.A | MyFlags.B), thorn.DoString("return FlagA + FlagB"));
    }


    [Fact]
    public void MultipleReturn()
    {
        var result = RunString(@"
function multret()
    return 2, 5, 7
end
local a, b, c = multret()
return a * b - c
");
        Assert.Equal(3f, result);
    }

    [Fact]
    public void MultipleReturnWithArgs()
    {
        var result = RunString(@"
function multret(argc)
    return 2, 5, argc
end
local a, b, c = multret(7)
return a * b - c
");
        Assert.Equal(3f, result);
    }

    [Fact]
    public void Printing()
    {
        string x = "";
        var thorn = new ThornRuntime();
        thorn.OnStdout += a => x += a;
        thorn.SetBuiltins();
        thorn.DoString("print(\"hello world!\")");
        Assert.Equal("hello world!\n", x);
    }

    [Fact]
    public void ArrayCreate()
    {
        var res = RunString("return { 2, 4, 6 }");
        Assert.IsType<ThornTable>(res);
        var table = (ThornTable)res;
        Assert.Equal(2f, table.Get(1));
        Assert.Equal(4f, table.Get(2));
        Assert.Equal(6f, table.Get(3));
        Assert.Equal(3, table.Length);
    }

    [Fact]
    public void Pow()
    {
        var res = RunString("return 2^3");
        Assert.Equal(8f, res);
    }

    [Fact]
    public void ArrayCreateLarge()
    {
        var res = RunString(@"return {
1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39,
40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59,
60, 61, 62, 63, 64, 65, 66, 67, 68
}");
        Assert.IsType<ThornTable>(res);
        var table = (ThornTable)res;
        Assert.Equal(68f, table.Get(68));
    }

    [Fact]
    public void TableCreateMap()
    {
        var res = RunString(@"return { a = ""hello"", b = 7, c = 12 }");
        Assert.IsType<ThornTable>(res);
        var table = (ThornTable)res;
        Assert.Equal("hello", table["a"]);
        Assert.Equal(7f, table["b"]);
        Assert.Equal(12f, table["c"]);
    }

    [Fact]
    public void TableSet()
    {
        var res = RunString(@"
local result = {}
result.a = ""hello""
result.b = 7
return result
");
        Assert.IsType<ThornTable>(res);
        var table = (ThornTable)res;
        Assert.Equal("hello", table.Get("a"));
        Assert.Equal(7f, table.Get("b"));
    }

    [Fact]
    public void Multiplication()
    {
        Assert.Equal(16f, RunString("return 4 * 4"));
    }

    [Fact]
    public void Division()
    {
        Assert.Equal(2f, RunString("return 4 / 2"));
    }

    [Fact]
    public void Addition()
    {
        Assert.Equal(10f, RunString("return 2 + 8"));
    }

    [Fact]
    public void Subtraction()
    {
        Assert.Equal(8f, RunString("return 10 - 2"));
    }

    [Fact]
    public void LocalLoop()
    {
        Assert.Equal(10f, RunString("local n=1\n while n < 11 do n=n+1 end; n=n-1\n return n"));
    }

    // Uses a local to avoid the tailcall op
    [Fact]
    public void FunctionCall()
    {
        Assert.Equal(10f, RunString(@"
function add(a, b)
    return(a + b)
end
local result = add(7, 3)
return result
"));
    }

    [Fact]
    public void LocalNumber()
    {
        Assert.Equal(9f, RunString(@"
local x = 7
local y = 9
local z = x + y
z = z - x
return z
"));
    }


}
