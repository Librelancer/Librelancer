//// This code has been based from the sample repository "Vortice.Vulkan": https://github.com/amerkoleci/Vortice.Vulkan
// Copyright (c) Amer Koleci and contributors.
// Copyright (c) 2020 - 2021 Faber Leonardo. All Rights Reserved. https://github.com/FaberSanZ
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

// HEAVILY Modified for Librelancer



using System.IO;
using CppAst;

namespace Generator
{
    public static partial class CsCodeGenerator
    {
        private static void GenerateHandles(CppCompilation compilation, string outputPath)
        {
            // Generate Functions
            using var writer = new CodeWriter(Path.Combine(outputPath, "Handles.cs"),
                "System",
                "System.Diagnostics"
                );

            foreach (CppTypedef typedef in compilation.Typedefs)
            {
                if (!(typedef.ElementType is CppPointerType))
                {
                    continue;
                }

                var isDispatchable = true ;

                var csName = typedef.Name;
                string handleType = "IntPtr";
                string nullValue = "0";
                
                using (writer.PushBlock($"public record struct {csName}({handleType} Handle)"))
                {
                    writer.WriteLine($"public bool IsNull => Handle == 0;");

                    writer.WriteLine($"public static {csName} Null => new {csName}({nullValue});");
                    writer.WriteLine($"public static implicit operator {csName}({handleType} handle) => new (handle);");
                    writer.WriteLine($"public static implicit operator {handleType}({csName} handle) => handle.Handle;");
                }

                s_nativeNameMappings[csName] = handleType; // This probably shouldn't modify at runtime for this specific table

                writer.WriteLine();
            }
        }
    }
}
