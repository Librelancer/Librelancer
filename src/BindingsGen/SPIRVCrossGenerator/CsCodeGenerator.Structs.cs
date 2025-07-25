// This code has been based from the sample repository "Vortice.Vulkan": https://github.com/amerkoleci/Vortice.Vulkan
// Copyright (c) Amer Koleci and contributors.
// Copyright (c) 2020 - 2021 Faber Leonardo. All Rights Reserved. https://github.com/FaberSanZ
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

// HEAVILY Modified for Librelancer

using System;
using System.IO;
using System.Text;
using CppAst;

namespace Generator
{
    public static partial class CsCodeGenerator
    {
        private static bool generateSizeOfStructs = false;

        static void Handle(CodeWriter writer, string t)
        {
            writer.WriteLine("[StructLayout(LayoutKind.Sequential, Pack = 1)]");
            using (writer.PushBlock($"public record struct {t}(uint Value)"))
            {
                writer.WriteLine($"public static implicit operator uint({t} self) => self.Value;");
                writer.WriteLine($"public static implicit operator {t}(uint self) => new(self);");
            }
        }

        private static void GenerateStructAndUnions(CppCompilation compilation, string outputPath)
        {
            // Generate Structures
            using var writer = new CodeWriter(Path.Combine(outputPath, "Structures.cs"),
                "System",
                "System.Runtime.InteropServices"
                );
            
            writer.WriteLine("[StructLayout(LayoutKind.Sequential, Pack = 1)]");
            using (writer.PushBlock("public record struct spvc_bool(byte Value)"))
            {
                writer.WriteLine("public static implicit operator byte(spvc_bool self) => self.Value;");
                writer.WriteLine("public static implicit operator spvc_bool(byte self) => new(self);");
                writer.WriteLine("public static implicit operator bool(spvc_bool self) => self.Value != 0;");
                writer.WriteLine("public static implicit operator spvc_bool(bool self) => self ? new(1) : new(0);");
            }
            
            Handle(writer, "spvc_constant_id");
            Handle(writer, "spvc_variable_id");
            Handle(writer, "spvc_type_id");
            Handle(writer, "spvc_hlsl_binding_flags");

            // Print All classes, structs
            foreach (CppClass? cppClass in compilation.Classes)
            {
                if (cppClass.ClassKind == CppClassKind.Class ||
                    cppClass.SizeOf == 0 ||
                    cppClass.Name.EndsWith("_T"))
                {
                    continue;
                }

    

                bool isUnion = cppClass.ClassKind == CppClassKind.Union;
  

                string csName = cppClass.Name;
                if (isUnion)
                {
                    writer.WriteLine("[StructLayout(LayoutKind.Explicit)]");
                }
                else
                {
                    writer.WriteLine("[StructLayout(LayoutKind.Sequential)]");
                }

                bool isReadOnly = false;
                string modifier = "partial";
                if (csName == "VkClearDepthStencilValue")
                {
                    modifier = "readonly partial";
                    isReadOnly = true;
                }

                using (writer.PushBlock($"public {modifier} struct {csName}"))
                {
                    if (generateSizeOfStructs && cppClass.SizeOf > 0)
                    {
                        writer.WriteLine("/// <summary>");
                        writer.WriteLine($"/// The size of the <see cref=\"{csName}\"/> type, in bytes.");
                        writer.WriteLine("/// </summary>");
                        writer.WriteLine($"public static readonly int SizeInBytes = {cppClass.SizeOf};");
                        writer.WriteLine();
                    }

                    foreach (CppField cppField in cppClass.Fields)
                    {
                        WriteField(writer, cppField, isUnion, isReadOnly);
                    }
                }

                writer.WriteLine();
            }
        }

        private static void WriteField(CodeWriter writer, CppField field, bool isUnion = false, bool isReadOnly = false)
        {
            string csFieldName = NormalizeFieldName(field.Name);

            if (isUnion)
            {
                writer.WriteLine("[FieldOffset(0)]");
            }

            if (field.Type is CppArrayType arrayType)
            {
                bool canUseFixed = false;
                if (arrayType.ElementType is CppPrimitiveType)
                {
                    canUseFixed = true;
                }
                else if (arrayType.ElementType is CppTypedef typedef
                    && typedef.ElementType is CppPrimitiveType)
                {
                    canUseFixed = true;
                }

                if (canUseFixed)
                {
                    string csFieldType = GetCsTypeName(arrayType.ElementType);
                    writer.WriteLine($"public unsafe fixed {csFieldType} {csFieldName}[{arrayType.Size}];");
                }
                else
                {
                    string unsafePrefix = string.Empty;
                    string csFieldType = GetCsTypeName(arrayType.ElementType);
                    if (csFieldType.EndsWith('*'))
                    {
                        unsafePrefix = "unsafe ";
                    }

                    for (int i = 0; i < arrayType.Size; i++)
                    {
                        writer.WriteLine($"public {unsafePrefix}{csFieldType} {csFieldName}_{i};");
                    }
                }
            }
            else
            {
                // VkAllocationCallbacks members
                if (field.Type is CppTypedef typedef &&
                    typedef.ElementType is CppPointerType pointerType &&
                    pointerType.ElementType is CppFunctionType functionType)
                {
                    StringBuilder builder = new();
                    foreach(CppParameter parameter in functionType.Parameters)
                    {
                        string paramCsType = GetCsTypeName(parameter.Type);
                        // Otherwise we get interop issues with non blittable types
         
                        builder.Append(paramCsType).Append(", ");
                    }

                    string returnCsName = GetCsTypeName(functionType.ReturnType);


                    builder.Append(returnCsName);

                    return;
                }

                string csFieldType = GetCsTypeName(field.Type);




                string fieldPrefix = isReadOnly ? "readonly " : string.Empty;
                if (csFieldType.EndsWith('*'))
                {
                    fieldPrefix += "unsafe ";
                }

                writer.WriteLine($"public {fieldPrefix}{csFieldType} {csFieldName};");
            }
        }
    }
}
