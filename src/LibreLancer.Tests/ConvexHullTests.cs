using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
using Xunit;
using LibreLancer.ContentEdit.Model;
using System.Text.Json;
namespace LibreLancer.Tests;

public class ConvexHullTests
{
    public static IEnumerable<object[]> GetTestCases()
    {
        return Directory.GetFiles("Hulls", "*.json", SearchOption.AllDirectories)
            .Select(x => new object[] { x });
    }

    public class Vector3Json : JsonConverter<Vector3>
    {
        public override Vector3 Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)

        {
            Vector3 v = Vector3.Zero;
            int i = 0;
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException();
            }
            reader.Read();
            while (reader.TokenType == JsonTokenType.Number)
            {
                v[i++] = reader.GetSingle();
                reader.Read();
            }
            if (reader.TokenType != JsonTokenType.EndArray ||
                i != 3)
            {
                throw new JsonException();
            }
            return v;
        }

        public override void Write(
            Utf8JsonWriter writer,
            Vector3 vector,
            JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(vector.X);
            writer.WriteNumberValue(vector.Y);
            writer.WriteNumberValue(vector.Z);
            writer.WriteEndArray();
        }

    }
    public static Vector3[] ReadTestHull(string path)
    {
        var serializeOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new Vector3Json()
            }
        };
        return JsonSerializer.Deserialize<Vector3[]>(File.ReadAllText(path), serializeOptions)!;
    }

    [Theory]
    [MemberData(nameof(GetTestCases))]
    public void TestHullValidity(string test)
    {
        var input = ReadTestHull(test);
        var h = HullData.Calculate(input);
        FLLog.Flush();
        Assert.False(h.IsError, h.AllMessages());
        Assert.True(HullData.IsConvexHull(h.Data.Vertices, h.Data.Indices));
        if (File.Exists(Path.ChangeExtension(test, ".txt")))
        {
            var count = int.Parse(File.ReadAllText(Path.ChangeExtension(test, ".txt")).Trim());
            Assert.Equal(count, h.Data.Indices.Length);
        }
    }
}
