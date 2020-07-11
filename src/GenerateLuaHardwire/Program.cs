using System;
using System.IO;
using System.Reflection;
using LibreLancer;
using MoonSharp.Interpreter;
using MoonSharp.Hardwire;
using MoonSharp.Hardwire.Languages;

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
        static void Main(string[] args)
        {
            UserData.RegisterAssembly(typeof(FreelancerGame).Assembly);
            foreach (var t in typeof(FreelancerGame).Assembly.GetTypes())
            {
                if (t.GetCustomAttribute<LibreLancer.Interface.UiLoadableAttribute>() != null)
                {
                    UserData.RegisterType(t);
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
            src = src.Replace("public abstract class LuaContext_Hardwire", "abstract class LuaContext_Hardwire");
            string path = "LuaContext_Hardwire.cs";
            if (args.Length > 0) path = args[0];
            File.WriteAllText(path, src);

        }
    }
}