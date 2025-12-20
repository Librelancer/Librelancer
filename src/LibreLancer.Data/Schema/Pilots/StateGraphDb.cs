using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Pilots
{
    public record StateGraphDescription(string Name, string Type);

    public class StateGraph
    {
        public StateGraph(StateGraphDescription desc)
        {
            Description = desc;
        }
        public StateGraphDescription Description;
        public List<float[]> Data = new List<float[]>();
    }

    public class StateGraphDb
    {
        public int StateGraphCount;
        public int BehaviorCount;

        public Dictionary<StateGraphDescription, StateGraph> Tables = new Dictionary<StateGraphDescription, StateGraph>();

        static bool TryParseFloats(string s, out float[] f)
        {
            var split = s.Split(new[] {' ', '\t'},
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            f = new float[split.Length];
            for (int i = 0; i < split.Length; i++)
            {
                if (!float.TryParse(split[i], NumberStyles.Any, CultureInfo.InvariantCulture, out f[i]))
                    return false;
            }

            return true;
        }

        public StateGraphDb()
        {
            BehaviorCount = 21;
        }

        public StateGraphDb(string path, FileSystem vfs)
        {
            using (var reader = new StreamReader(vfs?.Open(path) ?? File.OpenRead(path)))
            {
                string ln;
                string nameLine = null;

                StateGraphDescription currentDescription = null;
                StateGraph currentGraph = null;

                int currentLine = 0;

                while ((ln = reader.ReadLine()) != null)
                {
                    currentLine++;
                    if (string.IsNullOrWhiteSpace(ln)) continue;
                    ln = ln.Trim();
                    if (ln.StartsWith("state_graph_count", StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(ln.Substring("state_graph_count".Length).Trim(), out int x))
                            StateGraphCount = x;
                        else
                            FLLog.Warning("state_graph.db", "Invalid state_graph_count");
                    }
                    else if (ln.StartsWith("behavior_count", StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(ln.Substring("behavior_count".Length).Trim(), out int x))
                            BehaviorCount = x;
                        else
                            FLLog.Warning("state_graph.db", "Invalid behavior_count");
                    }
                    else if (char.IsDigit(ln[0]))
                    {
                        if (TryParseFloats(ln, out var floats))
                        {
                            if (BehaviorCount > 0 && floats.Length != BehaviorCount)
                            {
                                FLLog.Warning("state_graph.db",
                                    $"Line {currentLine} has {floats.Length} values, not behavior_count {BehaviorCount}");
                            }

                            currentGraph.Data.Add(floats);
                        }
                        else
                        {
                            FLLog.Warning("state_graph.db", $"Line {currentLine} has invalid numbers");
                        }
                    }
                    else
                    {
                        if (nameLine == null)
                        {
                            nameLine = ln;
                        }
                        else
                        {
                            if (currentDescription != null)
                            {
                                Tables[currentDescription] = currentGraph;
                            }
                            currentDescription = new StateGraphDescription(nameLine, ln);
                            currentGraph = new StateGraph(currentDescription);
                            nameLine = null;
                        }
                    }
                }
                if (currentDescription != null && currentGraph != null)
                    Tables[currentDescription] = currentGraph;
            }

            if (Tables.Count != StateGraphCount)
                FLLog.Warning("state_graph.db",
                    $"Total state graphs {Tables.Count} != state_graph_count ({StateGraphCount})");
        }
    }
}
