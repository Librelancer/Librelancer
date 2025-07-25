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
    public static partial class CsCodeGenerator
    {
        private static readonly HashSet<string> s_keywords = new HashSet<string>
        {
            "object",
            "event",
        };
        
        private static readonly Dictionary<string, string> s_nativeNameMappings = new Dictionary<string, string>()
        {
            { "uint8_t", "byte" },
            { "uint16_t", "ushort" },
            { "uint32_t", "uint" },
            { "uint64_t", "ulong" },
            { "int8_t", "sbyte" },
            { "int32_t", "int" },
            { "int16_t", "short" },
            { "int64_t", "long" },
            { "int64_t*", "long*" },
            { "char", "byte" },
            { "size_t", "nuint" },
            // Handles
            { "spvc_bool", "byte" },
            { "spvc_constant_id", "uint" },
            { "spvc_variable_id", "uint" },
            { "spvc_type_id", "uint" },
            { "spvc_hlsl_binding_flags", "uint" },
            { "SpvId", "uint" },
            // Multiple typedefs for same thing
            { "spvc_msl_shader_input", "spvc_msl_shader_interface_var" },
            { "spvc_msl_vertex_format", "spvc_msl_shader_variable_format" }
            
        };

        private static readonly Dictionary<string, string> s_csNameMappings = new Dictionary<string, string>()
        {
            { "uint8_t", "byte" },
            { "uint16_t", "ushort" },
            { "uint32_t", "uint" },
            { "uint64_t", "ulong" },
            { "int8_t", "sbyte" },
            { "int32_t", "int" },
            { "int16_t", "short" },
            { "int64_t", "long" },
            { "int64_t*", "long*" },
            { "char", "byte" },
            { "size_t", "nuint" },
            // SpvId doesn't need type safety
            { "SpvId", "uint" },
            // Multiple typedefs for same thing
            { "spvc_msl_shader_input", "spvc_msl_shader_interface_var" },
            { "spvc_msl_vertex_format", "spvc_msl_shader_variable_format" }
        };

        public static void Generate(CppCompilation compilation, string outputPath)
        {
            GenerateConstants(compilation, outputPath);
            GenerateEnums(compilation, outputPath);
            GenerateHandles(compilation, outputPath);
            GenerateStructAndUnions(compilation, outputPath);
            GenerateCommands(compilation, outputPath);
        }

        public static void AddCsMapping(string typeName, string csTypeName)
        {
            s_csNameMappings[typeName] = csTypeName;
        }

        private static void GenerateConstants(CppCompilation compilation, string outputPath)
        {
            
            
        }

        private static string NormalizeFieldName(string name)
        {
            if (s_keywords.Contains(name))
                return "@" + name;

            return name;
        }

        private static string GetCsCleanName(string name, bool nativeClean = false)
        {
            if ((nativeClean ? s_nativeNameMappings : s_csNameMappings).TryGetValue(name, out string? mappedName))
            {
                return GetCsCleanName(mappedName, nativeClean);
            }
            else if (name.StartsWith("PFN"))
            {
                return "IntPtr";
            }

            return name;
        }

        private static string NamePointer(string name, int pointer) => 
            pointer == 0 ? name : $"{name}{new string('*', pointer)}";
        

        private static string GetCsTypeName(CppType? type, int pointer = 0, bool nativeCall = false)
        {
            if (type is CppPrimitiveType primitiveType)
            {
                return GetCsPrimitiveTypeName(primitiveType, pointer);
            }

            if (type is CppQualifiedType qualifiedType)
            {
                return GetCsTypeName(qualifiedType.ElementType, pointer, nativeCall);
            }

            if (type is CppEnum enumType)
            {
                var enumCsName = GetCsCleanName(enumType.Name, nativeCall);
                return NamePointer(enumCsName, pointer);
            }

            if (type is CppTypedef typedef)
            {
                var typeDefCsName = GetCsCleanName(typedef.Name, nativeCall);
                return NamePointer(typeDefCsName, pointer);
            }

            if (type is CppClass @class)
            {
                var className = GetCsCleanName(@class.Name, nativeCall);
                return NamePointer(className, pointer);
            }

            if (type is CppPointerType pointerType)
            {
                return GetCsPointerTypeName(pointerType, pointer);
            }

            if (type is CppArrayType arrayType)
            {
                return GetCsTypeName(arrayType.ElementType, pointer++, nativeCall);
            }

            return string.Empty;
        }

        private static string GetCsPrimitiveTypeName(CppPrimitiveType primitiveType, int pointer)
        {
            switch (primitiveType.Kind)
            {
                case CppPrimitiveKind.Void:
                    return NamePointer("void", pointer);

                case CppPrimitiveKind.Char:
                    return NamePointer("byte", pointer);

                case CppPrimitiveKind.Bool:
                    break;
                case CppPrimitiveKind.WChar:
                    break;
                case CppPrimitiveKind.Short:
                    return NamePointer("short", pointer);
                case CppPrimitiveKind.Int:
                    return NamePointer("int", pointer);

                case CppPrimitiveKind.LongLong:
                    return NamePointer("long", pointer);
                case CppPrimitiveKind.UnsignedChar:
                    return NamePointer("byte", pointer);
                case CppPrimitiveKind.UnsignedShort:
                    return NamePointer("ushort", pointer);
                case CppPrimitiveKind.UnsignedInt:
                    return NamePointer("uint", pointer);
                case CppPrimitiveKind.UnsignedLongLong:
                    return NamePointer("ulong", pointer);
                case CppPrimitiveKind.Float:
                    return NamePointer("float", pointer);
                case CppPrimitiveKind.Double:
                    return NamePointer("double", pointer);
                case CppPrimitiveKind.LongDouble:
                    break;
                default:
                    return string.Empty;
            }
            return string.Empty;
        }

        private static string GetCsPointerTypeName(CppPointerType pointerType, int depth)
        {
            if (pointerType.ElementType is CppQualifiedType qualifiedType)
            {
                if (qualifiedType.ElementType is CppPrimitiveType primitiveType)
                {
                    return GetCsPrimitiveTypeName(primitiveType, depth + 1);
                }
                else if (qualifiedType.ElementType is CppClass @classType)
                {
                    return GetCsTypeName(@classType, depth + 1);
                }
                else if (qualifiedType.ElementType is CppPointerType subPointerType)
                {
                    return GetCsTypeName(subPointerType, depth + 1);
                }
                else if (qualifiedType.ElementType is CppTypedef typedef)
                {
                    return GetCsTypeName(typedef, depth + 1);
                }
                else if (qualifiedType.ElementType is CppEnum @enum)
                {
                    return GetCsTypeName(@enum, depth + 1);
                }

                return GetCsTypeName(qualifiedType.ElementType, depth + 1);
            }

            return GetCsTypeName(pointerType.ElementType, depth + 1);
        }
    }
}
