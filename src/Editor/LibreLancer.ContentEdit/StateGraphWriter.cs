using System;
using System.Globalization;
using System.IO;
using System.Text;
using LibreLancer.Data.Schema.Pilots;

namespace LibreLancer.ContentEdit;

public static class StateGraphWriter
{
    public static void Write(Stream outStream, StateGraphDb db)
    {
        // Round-trip vanilla state_graph.db file produces
        // file with identical sha256sum :)
        var writer = new StreamWriter(outStream, Encoding.ASCII);
        writer.NewLine = "\r\n";
        writer.WriteLine();
        writer.WriteLine($"state_graph_count {db.Tables.Count}");
        writer.WriteLine($"behavior_count {db.BehaviorCount}");
        foreach (var table in db.Tables)
        {
            writer.WriteLine();
            writer.WriteLine(table.Key.Name);
            writer.WriteLine(table.Key.Type);
            writer.WriteLine();
            for (int i = 0; i < table.Value.Data.Count; i++)
            {
                var line = table.Value.Data[i];
                for (int j = 0; j < line.Length; j++) {
                    writer.Write(line[j].ToString("F2", CultureInfo.InvariantCulture));
                    writer.Write(" ");
                }
                writer.WriteLine();
            }
        }
        writer.WriteLine();
        writer.Flush();
    }
}
