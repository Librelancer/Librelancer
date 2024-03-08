// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using FluentAssertions;
using LibreLancer.Ini;
using Xunit;

namespace LibreLancer.Tests.Ini
{
    public class ValueTests
    {
        [Theory]
        [InlineData(true,  true,  1, 1, 1, "True")]
        [InlineData(false, false, 0, 0, 0, "False")]
        public void BooleanValueConversions(bool testValue,
            bool toBoolean, int toInt32, long toInt64, float toSingle, string toString)
        {
            var value = new BooleanValue(testValue);

            value.ToBoolean().Should().Be(toBoolean);
            value.TryToInt32(out int i).Should().BeTrue();
            i.Should().Be(toInt32);
            value.ToInt32().Should().Be(toInt32);
            value.ToInt64().Should().Be(toInt64);
            value.ToSingle().Should().Be(toSingle);
            value.ToString().Should().Be(toString);
        }

        [Theory]
        [InlineData(0,            false, 0,            0,            0,            "0")]
        [InlineData(int.MinValue, true,  int.MinValue, int.MinValue, int.MinValue, "-2147483648")]
        [InlineData(int.MaxValue, true,  int.MaxValue, int.MaxValue, int.MaxValue, "2147483647")]
        public void Int32ValueConversions(int testValue,
            bool toBoolean, int toInt32, long toInt64, float toSingle, string toString)
        {
            var value = new Int32Value(testValue);

            value.ToBoolean().Should().Be(toBoolean);
            value.TryToInt32(out int i).Should().BeTrue();
            i.Should().Be(toInt32);
            value.ToInt32().Should().Be(toInt32);
            value.ToInt64().Should().Be(toInt64);
            value.ToSingle().Should().Be(toSingle);
            value.ToString().Should().Be(toString);
        }

        [Theory]
        [InlineData(0,              null, false, 0,            0,            0,              "0")]
        [InlineData(1,              null, true,  1,            1,            1,              "1")]
        [InlineData(float.MinValue, null, true,  int.MinValue, int.MinValue, float.MinValue, "-3.4028235E+38")]
        [InlineData(float.MinValue, -3,   true,  -3,           -3,           float.MinValue, "-3.4028235E+38")]
        [InlineData(float.MaxValue, null, true,  int.MinValue, int.MinValue, float.MaxValue, "3.4028235E+38")]
        [InlineData(float.MaxValue, 3,    true,  3,            3,            float.MaxValue, "3.4028235E+38")]
        public void SingleValueConversions(float testValue, long? testLongValue,
            bool toBoolean, int toInt32, long toInt64, float toSingle, string toString)
        {
            var value = new SingleValue(testValue, testLongValue);

            value.ToBoolean().Should().Be(toBoolean);
            value.TryToInt32(out int i).Should().BeTrue();
            i.Should().Be(toInt32);
            value.ToInt32().Should().Be(toInt32);
            value.ToInt64().Should().Be(toInt64);
            value.ToSingle().Should().Be(toSingle);
            value.ToString().Should().Be(toString);
        }

        [Theory]
        [InlineData("0",           true, true,  0,            0,            0,            0,            "0")]
        [InlineData("-2147483648", true, true,  int.MinValue, int.MinValue, int.MinValue, int.MinValue, "-2147483648")]
        [InlineData("2147483647",  true, true,  int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, "2147483647")]
        [InlineData("6566123.22",  true, false, -1,           -1,           -1,           6566123.22,   "6566123.22")]
        [InlineData("NaN",         true, false, -1,           -1,           -1,           float.NaN,    "NaN")]
        [InlineData("Nothing",     true, false, -1,           -1,           -1,           0,            "Nothing")]
        public void StringValueConversions(string testValue,
            bool toBoolean, bool tryInt32Return, int tryToInt32, int toInt32, long toInt64, float toSingle, string toString)
        {
            var value = new StringValue(testValue);

            value.ToBoolean().Should().Be(toBoolean);
            value.TryToInt32(out int i).Should().Be(tryInt32Return);
            i.Should().Be(tryToInt32);
            value.ToInt32().Should().Be(toInt32);
            value.ToInt64().Should().Be(toInt64);
            value.ToSingle().Should().Be(toSingle);
            value.ToString().Should().Be(toString);
        }
    }
}
