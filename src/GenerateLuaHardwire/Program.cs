using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using LibreLancer;
using Microsoft.CSharp;
using MoonSharp.Interpreter;
using MoonSharp.Hardwire;
using MoonSharp.Hardwire.Languages;
using MoonSharp.Interpreter.Compatibility;

namespace GenerateLuaHardwire
{
    class Logger : ICodeGenerationLogger
    {
        public void LogError(string message)
        {
            Console.Error.WriteLine("[E] {0}", message);
        }

        public void LogWarning(string message)
        {
           Console.Error.WriteLine("[W] {0}", message);
        }

        public void LogMinor(string message)
        {
            Console.Error.WriteLine("[I] {0}", message);
        }
    }
    class Program
    {
        static HashSet<Type> processed = new HashSet<Type>();
        private static HashSet<Type> registered = new HashSet<Type>();
        static void TryRegisterLists(Type t)
        {
            if (processed.Contains(t)) return;
            processed.Add(t);
            if (t.IsGenericType)
            {
                return;
            }
            foreach (var f in t.GetFields())
            {
                TryRegisterType(f.FieldType);
            }
            foreach (var p in t.GetProperties())
            {
                TryRegisterType(p.PropertyType);
            }

            foreach (var m in t.GetMethods())
            {
                TryRegisterType(m.ReturnType);
            }
        }

        static void TryRegisterType(Type t)
        {
            if (registered.Contains(t)) return;
            registered.Add(t);
            if (Framework.Do.IsGenericType(t))
            {
                Type generic = t.GetGenericTypeDefinition();
                if ((generic == typeof(List<>))
                    || (generic == typeof(IList<>))
                    || (generic == typeof(ICollection<>))
                    || (generic == typeof(IEnumerable<>)) 
                    || (generic == typeof(Dictionary<,>))
                    || (generic == typeof(IDictionary<,>)))
                {
                    UserData.RegisterType(t);
                }
            }
            
        }
        static void Main(string[] args)
        {
            UserData.RegisterAssembly(typeof(FreelancerGame).Assembly);
            foreach (var t in typeof(FreelancerGame).Assembly.GetTypes())
            {
                if (t.GetCustomAttribute<MoonSharpUserDataAttribute>() != null)
                {
                    TryRegisterLists(t);
                }
            }
            foreach (var t in typeof(FreelancerGame).Assembly.GetTypes())
            {
                if (t.GetCustomAttribute<LibreLancer.Interface.UiLoadableAttribute>() != null)
                {
                    UserData.RegisterType(t);
                    TryRegisterLists(t);
                }
            }
            UserData.RegisterType<LibreLancer.Interface.LuaCompatibleDictionary<string, bool>>();
            HardwireGeneratorRegistry.RegisterPredefined();
            var dump = UserData.GetDescriptionOfRegisteredTypes(true);
            var hw = new MoonSharp.Hardwire.HardwireGenerator("LibreLancer.Lua", "LuaContext_Hardwire", new Logger(),
                HardwireCodeGenerationLanguage.CSharp);
            hw.AllowInternals = true;
            hw.BuildCodeModel(dump);
            var src = hw.GenerateSourceCode();
            
            src = src.Replace("public abstract class LuaContext_Hardwire", "abstract partial class LuaContext_Hardwire");
            var srcbuilder = new StringBuilder();
            srcbuilder.AppendLine(src);
            srcbuilder.AppendLine("namespace LibreLancer.Lua {");
            srcbuilder.AppendLine("    abstract partial class LuaContext_Hardwire {");
            srcbuilder.AppendLine("        public static void GenerateTypeTable(MoonSharp.Interpreter.Table table) {");
            foreach (var type in UserData.GetRegisteredTypes())
            {
                var t = GetFriendlyName(type);
                if(t.StartsWith("MoonSharp")) continue;
                if(t.Contains("<,") || t.Contains("<>")) continue;
                srcbuilder.AppendLine($"            table[{GetSanitizedString(t)}] = typeof({t});");
            }
            srcbuilder.AppendLine("        }");
            srcbuilder.AppendLine("    }");
            srcbuilder.AppendLine("}");
            string path = "LuaContext_Hardwire.cs";
            if (args.Length > 0) path = args[0];
            File.WriteAllText(path, srcbuilder.ToString());
        }

        static string GetSanitizedString(string t)
        {
            return "\"" +
                   t.Replace(".", "_").Replace(",", "__").Replace("<", "___").Replace(">", "___")
                   + "\"";
        }
        
        static string GetFriendlyName(Type type)
        {
            string friendlyName = type.FullName;
            if (type.IsGenericType)
            {
                int iBacktick = friendlyName.IndexOf('`');
                if (iBacktick > 0)
                {
                    friendlyName = friendlyName.Remove(iBacktick);
                }
                friendlyName += "<";
                Type[] typeParameters = type.GetGenericArguments();
                for (int i = 0; i < typeParameters.Length; ++i)
                {
                    string typeParamName = GetFriendlyName(typeParameters[i]);
                    friendlyName += (i == 0 ? typeParamName : "," + typeParamName);
                }
                friendlyName += ">";
            }

            return friendlyName?.Replace("+", ".");
        }

    }
}