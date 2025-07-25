// This code has been based from the sample repository "Vortice.Vulkan": https://github.com/amerkoleci/Vortice.Vulkan
// Copyright (c) Amer Koleci and contributors.
// Copyright (c) 2020 - 2021 Faber Leonardo. All Rights Reserved. https://github.com/FaberSanZ
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

// HEAVILY Modified for Librelancer



using System;
using System.Collections.Generic;
using System.IO;
using CppAst;

namespace Generator
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            string outputPath = Path.Combine(AppContext.BaseDirectory, "Generated");
            if (args.Length > 0)
            {
                outputPath = args[0];
            }
            

            if (!Path.IsPathRooted(outputPath))
            {
                outputPath = Path.Combine(AppContext.BaseDirectory, outputPath);
            }

            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            string? headerFile = Path.Combine(AppContext.BaseDirectory, "spirv", "spirv_cross_c.h");
            var options = new CppParserOptions
            {
                ParseMacros = true,
            };

            var compilation = CppParser.ParseFile(headerFile, options);

            // Print diagnostic messages
            if (compilation.HasErrors)
            {
                foreach (var message in compilation.Diagnostics.Messages)
                {
                    if (message.Type == CppLogMessageType.Error)
                    {
                        var currentColor = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(message);
                        Console.ForegroundColor = currentColor;
                    }
                }

                return 0;
            }

            CsCodeGenerator.Generate(compilation, outputPath);
            return 0;
        }


    }
}
