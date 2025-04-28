using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace GLBindingsGen
{
    unsafe class Program
    {
        class GLFunction
        {
            public string function { get; set; }
            public string name { get; set; }
            public bool manual { get; set; }
            
            public string glesname { get; set; }


            public string ReturnType;
            public string NativeName;

            public (string type, string name)[] Arguments;

            static (string type, string name) ParseName(string name)
            {
                //Normalize
                name = string.Join(' ',
                    name.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
                name = name.Replace(" *", "*");
                //Split name and type
                int idxPointer = name.IndexOf('*');
                if (idxPointer != -1)
                {
                    return (name.Substring(0, idxPointer+1).Trim(), name.Substring(idxPointer + 1).Trim());
                }
                else
                {
                    var idxSpace = name.LastIndexOf(' ');
                    return (name.Substring(0, idxSpace).Trim(), name.Substring(idxSpace + 1).Trim());
                }
            }

            static IEnumerable<(string type, string name)> ParseArgs(string arglist)
            {
                var args = arglist.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                foreach (var e in args)
                    yield return ParseName(e);
            }
            public void Parse()
            {
                int openParen = function.IndexOf('(');
                int closeParen = function.IndexOf(')');
                var name_and_ret = function.Substring(0, openParen);
                (ReturnType, NativeName) = ParseName(name_and_ret);

                var arglist = function.Substring(openParen + 1, closeParen - openParen - 1);
                Arguments = ParseArgs(arglist).ToArray();
            }

            public string Invocation(Func<(string type, string name),string> nameMap)
            {
                return $"_{NativeName}({string.Join(", ", Arguments.Select(nameMap))})";
            }

            public string GetDelegateType(Func<string,string> mapArgTypes)
            {
                string argList = string.Join(',', Arguments.Select((x) => mapArgTypes(x.type)));
                return $"delegate* unmanaged<{argList}{(Arguments.Length > 0 ? "," : "")}{mapArgTypes(ReturnType)}>";
            }
        }

        class IndentedWriter
        {
            private TextWriter backing;
            public IndentedWriter(TextWriter writer)
            {
                backing = writer;
            }
            private int indent = 0;
            public void Indent() => indent++;
            public void Unindent() => indent--;

            public void WriteLine(string line)
            {
                for(int i = 0; i < indent;i++)
                    backing.Write("    ");
                backing.WriteLine(line);
            }
        }

        static Dictionary<string, string> types = new Dictionary<string, string>()
        {
            { "string", "byte*" },
            { "bool", "int" }
        };

        static string TypeMap(string type)
        {
            if (types.TryGetValue(type, out var m)) return m;
            if (type.StartsWith("out ") || type.StartsWith("ref ") || type.EndsWith("[]"))
            {
                var fixedType = type.Replace("out ", "").Replace("ref ", "").Replace("[]", "");
                return fixedType + "*";
            }
            return type;
        }

        
        static void Main(string[] args)
        {
            var functions = JsonSerializer.Deserialize<GLFunction[]>(File.ReadAllText("bindings.json"));
            foreach (var f in functions)
            {
                f.Parse();
            }
            
            var sw = new StringWriter();
            var indented = new IndentedWriter(sw);
            indented.WriteLine("// AUTOMATICALLY GENERATED");
            indented.WriteLine("using System;");
            indented.WriteLine("using System.Numerics;");
            indented.WriteLine("using System.Runtime.InteropServices;");
            indented.WriteLine("");
            indented.WriteLine("namespace LibreLancer.Graphics.Backends.OpenGL");
            indented.WriteLine("{");
            indented.Indent();
            indented.WriteLine("static unsafe partial class GL");
            indented.WriteLine("{");
            indented.Indent();
            foreach (var f in functions)
            {
                indented.WriteLine($"private static {f.GetDelegateType(TypeMap)} _{f.NativeName};");
            }
            indented.WriteLine("");
            indented.WriteLine("public static void Load(Func<string,IntPtr> getProcAddress, bool isGles)");
            indented.WriteLine("{");
            indented.Indent();
            foreach (var f in functions)
            {
                string fname;
                if (!string.IsNullOrWhiteSpace(f.glesname))
                    fname = $"isGles ? \"{f.glesname}\" : \"{f.NativeName}\"";
                else
                    fname = $"\"{f.NativeName}\"";
                indented.WriteLine($"_{f.NativeName} = ({f.GetDelegateType(TypeMap)})getProcAddress({fname});");
            }
            indented.Unindent();
            indented.WriteLine("}");
            bool hasStringAlloc = false;
            //
            foreach (var f in functions)
            {
                if (f.manual) continue;
                indented.WriteLine($"public static {f.ReturnType} {f.name}({string.Join(", ", f.Arguments.Select((x) => x.type + " " + x.name))})");
                indented.WriteLine("{");
                indented.Indent();
                if(f.ReturnType != "void")
                    indented.WriteLine($"{f.ReturnType} retval;");
                foreach (var a in f.Arguments) {

                    if (a.type == "string")
                    {
                        indented.WriteLine($"Span<byte> _{a.name}_stack = stackalloc byte[256];");
                        indented.WriteLine($"using var _{a.name}_utf8 = new UTF8ZHelper(_{a.name}_stack, {a.name});");
                        indented.WriteLine($"fixed (byte* _{a.name}_ptr = _{a.name}_utf8.ToUTF8Z())");
                        indented.WriteLine("{");
                        indented.Indent();
                    }
                    if (a.type.StartsWith("out ") ||
                                                     a.type.StartsWith("ref ") ||
                                                     a.type.EndsWith("[]"))
                    {
                        var fixedType = a.type.Replace("out ", "").Replace("ref ", "").Replace("[]", "");
                        if (a.type.EndsWith("[]"))
                        {
                            indented.WriteLine($"fixed ({fixedType}* _{a.name}_ptr = {a.name})");
                        }
                        else
                        {
                            indented.WriteLine($"fixed ({fixedType}* _{a.name}_ptr = &{a.name})");
                        }
                        indented.WriteLine("{");
                        indented.Indent();
                    }
                }
                static string MapName((string type, string name) arg)
                {
                    if (arg.type == "string" || arg.type.StartsWith("out ") ||
                        arg.type.StartsWith("ref ") || arg.type.EndsWith("[]"))
                        return $"_{arg.name}_ptr";
                    if (arg.type == "bool")
                    {
                        return $"({arg.name} ? 1 : 0)";
                    }
                    return arg.name;
                }
                if (f.ReturnType == "string") {
                    indented.WriteLine($"var _retval_ptr = (IntPtr){f.Invocation(MapName)};");
                    indented.WriteLine("retval =  Marshal.PtrToStringUTF8(_retval_ptr);");
                }
                else if (f.ReturnType == "bool")
                {
                    indented.WriteLine($"retval = ({f.Invocation(MapName)} != 0);");
                }
                else if (f.ReturnType != "void") {
                    indented.WriteLine($"retval = {f.Invocation(MapName)};");
                }
                else {
                    indented.WriteLine($"{f.Invocation(MapName)};");
                }
                foreach (var a in f.Arguments) {
                    if (a.type.StartsWith("string") ||
                        a.type.StartsWith("out ") ||
                        a.type.StartsWith("ref ") ||
                        a.type.EndsWith("[]"))
                    {
                        indented.Unindent();
                        indented.WriteLine("}");
                    }
                }
                if(f.NativeName != "glGetError")
                    indented.WriteLine("ErrorCheck();");
                if(f.ReturnType != "void")
                    indented.WriteLine("return retval;");
                indented.Unindent();
                indented.WriteLine("}");
            }
            
            indented.Unindent();
            indented.WriteLine("}");
            indented.Unindent();
            indented.WriteLine("}");
            
            Console.WriteLine(sw.ToString());
        }
    }
}
