// This code has been based from the sample repository "Vortice.Vulkan": https://github.com/amerkoleci/Vortice.Vulkan
// Copyright (c) Amer Koleci and contributors.
// Copyright (c) 2020 - 2021 Faber Leonardo. All Rights Reserved. https://github.com/FaberSanZ
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

// HEAVILY Modified for Librelancer

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CppAst;

namespace Generator
{
    public static partial class CsCodeGenerator
    {
        private static readonly HashSet<string> s_instanceFunctions = new HashSet<string>
        {

        };

        private static readonly HashSet<string> s_outReturnFunctions = new HashSet<string>
        {

        };


        static bool IsCString(CppType cppType)
        {
            if (cppType is not CppPointerType pointerType)
                return false;
            if (pointerType.ElementType is not CppQualifiedType qualified)
                return false;
            if (qualified.Qualifier != CppTypeQualifier.Const)
                return false;
            if (qualified.ElementType is not CppPrimitiveType primitiveType)
                return false;
            return primitiveType.Kind == CppPrimitiveKind.Char;
        }

        private static void GenerateCommands(CppCompilation compilation, string outputPath)
        {
            // Generate Functions
            using var writer = new CodeWriter(Path.Combine(outputPath, "Commands.cs"),
                "System", "System.Buffers",
                "System.Runtime.InteropServices", "System.Text"
                );


            var commands = new Dictionary<string, CppFunction>();
            var instanceCommands = new Dictionary<string, CppFunction>();
            var deviceCommands = new Dictionary<string, CppFunction>();
            foreach (CppFunction? cppFunction in compilation.Functions)
            {
                string? returnType = GetCsTypeName(cppFunction.ReturnType);
                bool canUseOut = s_outReturnFunctions.Contains(cppFunction.Name);
                string? csName = cppFunction.Name;

                commands.Add(csName, cppFunction);

                if (cppFunction.Parameters.Count > 0)
                {
                    var firstParameter = cppFunction.Parameters[0];
                    if (firstParameter.Type is CppTypedef typedef)
                    {


                        deviceCommands.Add(csName, cppFunction);

                    }
                }
            }

            using (writer.PushBlock($"unsafe partial class Spvc"))
            {
                writer.WriteLine("const string LIB = \"spirv-cross-c-shared\";");
                writer.WriteLine(@"
ref struct CStr
{
    private byte[] poolArray;
    private Span<byte> bytes;
    private Span<byte> utf8z;
    public CStr(Span<byte> initialBuffer, ReadOnlySpan<char> value)
    {
        poolArray = null;
        bytes = initialBuffer;
        int maxSize = Encoding.UTF8.GetMaxByteCount(value.Length) + 1;
        if (bytes.Length < maxSize) {
            poolArray = ArrayPool<byte>.Shared.Rent(maxSize);
            bytes = new Span<byte>(poolArray);
        }
        int byteCount = Encoding.UTF8.GetBytes(value, bytes);
        bytes[byteCount] = 0;
        utf8z = bytes.Slice(0, byteCount + 1);
    }

    public Span<byte> Bytes() => utf8z;

    public void Dispose()
    {
        byte[] toReturn = poolArray;
        if (toReturn != null)
        {
            poolArray = null;
            ArrayPool<byte>.Shared.Return(toReturn);
        }
    }
}
");

                foreach (KeyValuePair<string, CppFunction> command in commands)
                {
                    CppFunction cppFunction = command.Value;


                    var returnIsString = IsCString(cppFunction.ReturnType);

                    string returnCsName = returnIsString ? "string" : GetCsTypeName(cppFunction.ReturnType, 0);
                    string nativeReturn = returnIsString ? "IntPtr" : GetCsTypeName(cppFunction.ReturnType, 0, true);

                    bool canUseOut = s_outReturnFunctions.Contains(cppFunction.Name);
                    var (signature, stringParams) = GetParameterSignature(cppFunction, canUseOut);

                    writer.WriteLine($"[LibraryImport(LIB, EntryPoint=\"{cppFunction.Name}\")]");
                    writer.WriteLine($"private static partial {nativeReturn} _{command.Key}({ GetParameterSignature(cppFunction, canUseOut, true).Signature});");


                    var funcCsName = cppFunction.Name.StartsWith("spvc_") ?
                        cppFunction.Name.Substring(5) : cppFunction.Name;
                    using (writer.PushBlock($"public static {returnCsName} {funcCsName}({signature})"))
                    {
                        List<IDisposable> blocks = new List<IDisposable>();

                        foreach (var s in stringParams)
                        {
                            string paramCsName = GetParameterName(s);
                            writer.WriteLine($"using var _{paramCsName}_cstr = new CStr(stackalloc byte[256], {paramCsName});");
                        }

                        foreach (var s in stringParams)
                        {
                            string paramCsName = GetParameterName(s);
                            blocks.Add(writer.PushBlock($"fixed(byte* _{paramCsName}_ptr = _{paramCsName}_cstr.Bytes())"));
                        }
                        if (returnCsName != "void")
                        {
                            writer.Write("return ");
                        }

                        if (returnIsString)
                        {
                            writer.Write("Marshal.PtrToStringUTF8(");
                        }

                        writer.Write($"_{command.Key}(");
                        int index = 0;
                        foreach (CppParameter cppParameter in cppFunction.Parameters)
                        {
                            string paramCsName = GetParameterName(cppParameter.Name);

                            if (canUseOut && CanBeUsedAsOutput(cppParameter.Type, out CppTypeDeclaration? cppTypeDeclaration))
                            {
                                writer.Write("out ");
                            }

                            if(stringParams.Contains(cppParameter.Name))
                                writer.Write($"_{paramCsName}_ptr");
                            else
                                writer.Write($"{paramCsName}");

                            if (index < cppFunction.Parameters.Count - 1)
                            {
                                writer.Write(", ");
                            }

                            index++;
                        }

                        if (returnIsString)
                        {
                            writer.Write(")");
                        }

                        writer.WriteLine(");");

                        foreach (var b in blocks)
                            b.Dispose();
                    }

                    writer.WriteLine();
                }
            }
        }





        public static (string Signature, HashSet<string> Strings) GetParameterSignature(CppFunction cppFunction, bool canUseOut, bool nativeCall = false)
        {
            return GetParameterSignature(cppFunction.Parameters, canUseOut, nativeCall);
        }

        private static (string Signature, HashSet<string> Strings) GetParameterSignature(IList<CppParameter> parameters, bool canUseOut,
            bool nativeCall = false)
        {
            var argumentBuilder = new StringBuilder();
            int index = 0;

            var strings = new HashSet<string>();

            foreach (CppParameter cppParameter in parameters)
            {
                string direction = string.Empty;
                bool isString = !nativeCall && IsCString(cppParameter.Type);
                var paramCsTypeName = isString ? "string" : GetCsTypeName(cppParameter.Type, 0, nativeCall);
                if (paramCsTypeName == "")
                {
                    Console.WriteLine($"error mapping {cppParameter.Type} {cppParameter.Name}");
                }

                if (isString)
                {
                    strings.Add(cppParameter.Name);
                }

                var paramCsName = GetParameterName(cppParameter.Name);

                if (canUseOut && CanBeUsedAsOutput(cppParameter.Type, out CppTypeDeclaration? cppTypeDeclaration))
                {
                    argumentBuilder.Append("out ");
                    paramCsTypeName = GetCsTypeName(cppTypeDeclaration, 0, nativeCall);
                }

                argumentBuilder.Append(paramCsTypeName).Append(' ').Append(paramCsName);
                if (index < parameters.Count - 1)
                {
                    argumentBuilder.Append(", ");
                }

                index++;
            }

            return (argumentBuilder.ToString(), strings);
        }

        private static string GetParameterName(string name)
        {
            if (name == "event")
                return "@event";

            if (name == "object")
                return "@object";

            if (name.StartsWith('p')
                && char.IsUpper(name[1]))
            {
                name = char.ToLower(name[1]) + name.Substring(2);
                return GetParameterName(name);
            }

            return name;
        }

        private static bool CanBeUsedAsOutput(CppType type, out CppTypeDeclaration? elementTypeDeclaration)
        {
            if (type is CppPointerType pointerType)
            {
                if (pointerType.ElementType is CppTypedef typedef)
                {
                    elementTypeDeclaration = typedef;
                    return true;
                }
                else if (pointerType.ElementType is CppClass @class
                    && @class.ClassKind != CppClassKind.Class
                    && @class.SizeOf > 0)
                {
                    elementTypeDeclaration = @class;
                    return true;
                }
                else if (pointerType.ElementType is CppEnum @enum
                    && @enum.SizeOf > 0)
                {
                    elementTypeDeclaration = @enum;
                    return true;
                }
            }

            elementTypeDeclaration = null;
            return false;
        }
    }
}
