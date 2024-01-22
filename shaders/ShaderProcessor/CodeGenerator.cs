// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CSharp;

namespace ShaderProcessor;

public class CodeGenerator
{
    public static string AllShaders(CodeGenOptions opts, StringBuilder codeBuilder, IEnumerable<EffectFile> files)
    {
        var compileUnit = new CodeCompileUnit();
        var nsroot = new CodeNamespace(opts.Namespace);
        nsroot.Imports.Add(new CodeNamespaceImport("System"));
        compileUnit.Namespaces.Add(nsroot);
        var genclass = new CodeTypeDeclaration("AllShaders");
        if (!opts.Public) genclass.TypeAttributes &= ~TypeAttributes.Public;
        nsroot.Types.Add(genclass);
        var iscompiled = new CodeMemberField(typeof(bool), "iscompiled");
        iscompiled.Attributes = MemberAttributes.Private | MemberAttributes.Static;
        iscompiled.InitExpression = new CodePrimitiveExpression(false);
        genclass.Members.Add(iscompiled);

        var vsrc = new CodeMemberField(typeof(byte[]), "shader_bytes");
        vsrc.Attributes = MemberAttributes.Private | MemberAttributes.Static;
        vsrc.InitExpression = ByteArray(Compress.GetBytes(codeBuilder.ToString(), opts.Brotli));
        genclass.Members.Add(vsrc);

        var compile = new CodeMemberMethod();
        compile.Name = "Compile";
        compile.Attributes = MemberAttributes.Public | MemberAttributes.Static;
        if (opts.DeviceParameter != null)
        {
            compile.Parameters.Add(new CodeParameterDeclarationExpression(opts.DeviceParameter, "device"));
        }
        //Once only
        compile.Statements.Add(new CodeConditionStatement(new CodeFieldReferenceExpression(null, "iscompiled"),
            new CodeMethodReturnStatement()));
        compile.Statements.Add(new CodeAssignStatement(
            new CodeFieldReferenceExpression(null, "iscompiled"),
            new CodePrimitiveExpression(true)
        ));
        //Log
        if (opts.Log)
            compile.Statements.Add(
                new CodeMethodInvokeExpression(null, opts.LogMethod,
                    new CodePrimitiveExpression("Compiling all shaders"))
            );
        //Decompress source
        compile.Statements.Add(new CodeVariableDeclarationStatement(typeof(string), "shadersrc"));
        compile.Statements.Add(new CodeAssignStatement(
                new CodeVariableReferenceExpression("shadersrc"),
                new CodeMethodInvokeExpression(null, "ShCompHelper.FromArray",
                    new CodeFieldReferenceExpression(null, "shader_bytes"))
            )
        );
        //Compile
        foreach (var fx in files)
        {
            CodeExpression[] compileParams;
            if (opts.DeviceParameter != null)
                compileParams = new[]
                {
                    new CodeVariableReferenceExpression("device"),
                    new CodeVariableReferenceExpression("shadersrc")
                };
            else
                compileParams = new[] { new CodeVariableReferenceExpression("shadersrc") };
            compile.Statements.Add(new CodeMethodInvokeExpression(null, $"{fx.Name}.Compile", compileParams));
        }
        genclass.Members.Add(compile);
        return GenCodeUnit(compileUnit);
    }

    public static string CreateEnum(CodeGenOptions opts, IEnumerable<string> enums)
    {
        var compileUnit = new CodeCompileUnit();
        var nsroot = new CodeNamespace(opts.Namespace);
        nsroot.Imports.Add(new CodeNamespaceImport("System"));
        compileUnit.Namespaces.Add(nsroot);
        var genclass = new CodeTypeDeclaration("ShaderFeatures");
        genclass.CustomAttributes.Add(new CodeAttributeDeclaration("Flags"));
        genclass.IsEnum = true;
        if (!opts.Public) genclass.TypeAttributes &= ~TypeAttributes.Public;
        var zeroField = new CodeMemberField(typeof(int), "None");
        zeroField.InitExpression = new CodePrimitiveExpression(0);
        genclass.Members.Add(zeroField);
        var i = 0;
        foreach (var e in enums.OrderBy(x => x))
        {
            var field = new CodeMemberField(typeof(int), e);
            field.InitExpression = new CodePrimitiveExpression(1 << i++);
            genclass.Members.Add(field);
        }

        nsroot.Types.Add(genclass);
        return GenCodeUnit(compileUnit);
    }

    private static CodeExpression ByteArray(byte[] src)
    {
        var group = src.Select((e, i) => new {Item = e.ToString(), Grouping = i / 30})
            .GroupBy(e => e.Grouping)
            .Select(x => string.Join(", ", x.Select(y => y.Item)));

        var b = new StringBuilder();
        b.Append("new byte[");
        b.Append(src.Length);
        b.AppendLine("] {");
        b.AppendLine(string.Join(",\n", group));
        b.Append("}");
        return new CodeSnippetExpression(b.ToString());
    }

    public static string Generate(CodeGenOptions opts, StringBuilder glslBuilder, Dictionary<string,int> codeOffsets, EffectFile fx,
        Dictionary<string, int> enumVals)
    {
        var compileUnit = new CodeCompileUnit();
        var nsroot = new CodeNamespace(opts.Namespace);
        nsroot.Imports.Add(new CodeNamespaceImport("System"));
        foreach (var import in opts.Imports)
            nsroot.Imports.Add(new CodeNamespaceImport(import));
        compileUnit.Namespaces.Add(nsroot);
        var genclass = new CodeTypeDeclaration(fx.Name);
        if (!opts.Public) genclass.TypeAttributes = TypeAttributes.Class;
        nsroot.Types.Add(genclass);

        var variants = new CodeMemberField(new CodeTypeReference(opts.ShaderType, 1), "variants");
        variants.Attributes = MemberAttributes.Static;
        genclass.Members.Add(variants);

        var iscompiled = new CodeMemberField(typeof(bool), "iscompiled");
        iscompiled.Attributes = MemberAttributes.Private | MemberAttributes.Static;
        iscompiled.InitExpression = new CodePrimitiveExpression(false);
        genclass.Members.Add(iscompiled);

        var mask = 0;
        foreach (var feature in fx.Features)
            mask |= enumVals[feature];

        var idx = 1;

        if (fx.Features.Length > 0)
        {
            var getIdx = new CodeMemberMethod();
            getIdx.Name = "GetIndex";
            getIdx.Attributes = MemberAttributes.Private | MemberAttributes.Static;
            getIdx.ReturnType = new CodeTypeReference(typeof(int));
            getIdx.Parameters.Add(new CodeParameterDeclarationExpression("ShaderFeatures", "features"));
            //Mask out invalid flags
            getIdx.Statements.Add(new CodeVariableDeclarationStatement(
                "ShaderFeatures", "masked",
                new CodeBinaryOperatorExpression(new CodeArgumentReferenceExpression("features"),
                    CodeBinaryOperatorType.BitwiseAnd,
                    new CodeCastExpression("ShaderFeatures", new CodePrimitiveExpression(mask)))));
            //Continue
            foreach (var permutation in FeatureHelper.Permute("", fx.Features))
            {
                var flag = 0;
                foreach (var s in permutation)
                    flag |= enumVals[s];
                var expr = new CodeCastExpression("ShaderFeatures", new CodePrimitiveExpression(flag));
                var cond = new CodeConditionStatement(new CodeBinaryOperatorExpression(
                        new CodeArgumentReferenceExpression("masked"),
                        CodeBinaryOperatorType.ValueEquality, expr),
                    new CodeMethodReturnStatement(new CodePrimitiveExpression(idx++)));
                getIdx.Statements.Add(cond);
            }

            getIdx.Statements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(0)));
            genclass.Members.Add(getIdx);

            var getShader = new CodeMemberMethod();
            getShader.ReturnType = new CodeTypeReference(opts.ShaderType);
            getShader.Name = "Get";
            getShader.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            getShader.ReturnType = new CodeTypeReference(opts.ShaderType);
            CodeExpression[] compileParams = new CodeExpression[0];
            if (opts.DeviceParameter != null)
            {
                getShader.Parameters.Add(new CodeParameterDeclarationExpression(opts.DeviceParameter, "device"));
                compileParams = new[] { new CodeVariableReferenceExpression("device") };
            }
            getShader.Parameters.Add(new CodeParameterDeclarationExpression("ShaderFeatures", "features"));
            var retval = new CodeArrayIndexerExpression(new CodeFieldReferenceExpression(null, "variants"),
                new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(null, "GetIndex"),
                    new CodeArgumentReferenceExpression("features")));
            getShader.Statements.Add(new CodeMethodInvokeExpression(null, "AllShaders.Compile", compileParams));
            getShader.Statements.Add(new CodeMethodReturnStatement(retval));
            genclass.Members.Add(getShader);
        }
        else
        {
            var getShader = new CodeMemberMethod();
            getShader.ReturnType = new CodeTypeReference(opts.ShaderType);
            getShader.Name = "Get";
            getShader.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            getShader.ReturnType = new CodeTypeReference(opts.ShaderType);
            CodeExpression[] compileParams = new CodeExpression[0];
            if (opts.DeviceParameter != null)
            {
                getShader.Parameters.Add(new CodeParameterDeclarationExpression(opts.DeviceParameter, "device"));
                compileParams = new[] { new CodeVariableReferenceExpression("device") };
            }
            getShader.Parameters.Add(new CodeParameterDeclarationExpression("ShaderFeatures", "features"));
            getShader.Statements.Add(new CodeMethodInvokeExpression(null, "AllShaders.Compile", compileParams));
            var retval = new CodeArrayIndexerExpression(new CodeFieldReferenceExpression(null, "variants"),
                new CodePrimitiveExpression(0));
            getShader.Statements.Add(new CodeMethodReturnStatement(retval));
            genclass.Members.Add(getShader);
        }

        var getZero = new CodeMemberMethod();
        getZero.Name = "Get";
        getZero.Attributes = MemberAttributes.Public | MemberAttributes.Static;
        getZero.ReturnType = new CodeTypeReference(opts.ShaderType);
        CodeExpression[] compileZeroParams = new CodeExpression[0];
        if (opts.DeviceParameter != null)
        {
            getZero.Parameters.Add(new CodeParameterDeclarationExpression(opts.DeviceParameter, "device"));
            compileZeroParams = new[] { new CodeVariableReferenceExpression("device") };
        }
        var zeroRet = new CodeArrayIndexerExpression(new CodeFieldReferenceExpression(null, "variants"),
            new CodePrimitiveExpression(0));
        getZero.Statements.Add(new CodeMethodInvokeExpression(null, "AllShaders.Compile", compileZeroParams));
        getZero.Statements.Add(new CodeMethodReturnStatement(zeroRet));
        genclass.Members.Add(getZero);

        var compile = new CodeMemberMethod();
        compile.Name = "Compile";
        if (opts.DeviceParameter != null)
        {
            compile.Parameters.Add(new CodeParameterDeclarationExpression(opts.DeviceParameter, "device"));
        }
        compile.Parameters.Add(new CodeParameterDeclarationExpression("System.String", "sourceBundle"));
        compile.Attributes = MemberAttributes.Assembly | MemberAttributes.Static;
        //Once only
        compile.Statements.Add(new CodeConditionStatement(new CodeFieldReferenceExpression(null, "iscompiled"),
            new CodeMethodReturnStatement()));
        compile.Statements.Add(new CodeAssignStatement(
            new CodeFieldReferenceExpression(null, "iscompiled"),
            new CodePrimitiveExpression(true)
        ));
        //Log
        if (opts.Log)
            compile.Statements.Add(
                new CodeMethodInvokeExpression(null, opts.LogMethod,
                    new CodePrimitiveExpression($"Compiling {fx.Name}"))
            );
        var compMeth = new CodeMethodReferenceExpression(null, opts.ShaderCompileMethod);
        //Init array
        compile.Statements.Add(new CodeAssignStatement(
            new CodeFieldReferenceExpression(null, "variants"),
            new CodeArrayCreateExpression(opts.ShaderType, idx))
        );

        var gl3src = new StringBuilder();
        var gl4src = new StringBuilder();

        CodeExpression StringBuilderExpression(string src, bool gl3)
        {
            int vOff;
            int vLen = src.Length;
            if (!codeOffsets.TryGetValue(src, out vOff))
            {
                vOff = glslBuilder.Length;
                glslBuilder.Append(src);
                codeOffsets[src] = vOff;
            }
            else
            {
                Console.WriteLine("Referenced duplicate source");
            }
            if (gl3)
                gl3src.AppendLine(src);
            else
                gl4src.AppendLine(src);
            return new CodeMethodInvokeExpression(
                new CodeArgumentReferenceExpression("sourceBundle"),
                "Substring",
                new CodePrimitiveExpression(vOff),
                new CodePrimitiveExpression(vLen)
            );
        }

        CodeExpression[] ShaderCompile(string defs, bool gl3)
        {
            var ls = new List<CodeExpression>();
            if (opts.DeviceParameter != null) {
                ls.Add(new CodeVariableReferenceExpression("device"));
            }
            ls.Add(StringBuilderExpression(ShaderCompiler.SHCompile(fx.VertexSource, fx.Name, defs, ShaderCompiler.ShaderKind.Vertex), gl3));
            ls.Add(StringBuilderExpression(ShaderCompiler.SHCompile(fx.FragmentSource, fx.Name, defs, ShaderCompiler.ShaderKind.Fragment), gl3));
            if (!string.IsNullOrWhiteSpace(fx.GeometrySource)) {
                ls.Add(StringBuilderExpression(ShaderCompiler.SHCompile(fx.GeometrySource, fx.Name, defs, ShaderCompiler.ShaderKind.Geometry), gl3));
            }
            return ls.ToArray();
        }

        var gl3Statements = new List<CodeStatement>();
        var gl4Statements = new List<CodeStatement>();

        //Compile null variant
        Console.WriteLine($"Compiling (GL3) {fx.Name} (basic)");
        gl3Statements.Add(new CodeAssignStatement(
            new CodeArrayIndexerExpression(new CodeFieldReferenceExpression(null, "variants"),
                new CodePrimitiveExpression(0)),
            new CodeMethodInvokeExpression(compMeth, ShaderCompile("", true))
            )
        );
        Console.WriteLine($"Compiling (GL4) {fx.Name} (basic)");
        gl4Statements.Add(new CodeAssignStatement(
            new CodeArrayIndexerExpression(new CodeFieldReferenceExpression(null, "variants"),
                new CodePrimitiveExpression(0)),
            new CodeMethodInvokeExpression(compMeth, ShaderCompile("#define FEATURES430\n", false))
        ));
        //Compile all variants
        idx = 1;
        foreach (var permutation in FeatureHelper.Permute(null, fx.Features))
        {
            var builder = new StringBuilder();
            builder.AppendLine();
            foreach (var def in permutation)
                builder.Append("#define ").AppendLine(def);
            Console.WriteLine($"Compiling (GL3) {fx.Name} ({string.Join(", ", permutation)})");
            gl3Statements.Add(new CodeAssignStatement(
                new CodeArrayIndexerExpression(new CodeFieldReferenceExpression(null, "variants"),
                    new CodePrimitiveExpression(idx)),
                new CodeMethodInvokeExpression(compMeth, ShaderCompile(builder.ToString(), true))
            ));
            builder.AppendLine("#define FEATURES430");
            Console.WriteLine($"Compiling (GL4) {fx.Name} ({string.Join(", ", permutation)})");
            gl4Statements.Add(new CodeAssignStatement(
                new CodeArrayIndexerExpression(new CodeFieldReferenceExpression(null, "variants"),
                    new CodePrimitiveExpression(idx)),
                new CodeMethodInvokeExpression(compMeth, ShaderCompile(builder.ToString(), false))
            ));
            idx++;
        }

        if (gl3src.ToString() == gl4src.ToString())
        {
            compile.Statements.Add(new CodeCommentStatement("No GL4 variants detected"));
            compile.Statements.AddRange(gl3Statements.ToArray());
        }
        else
        {
            compile.Statements.Add(new CodeConditionStatement(new CodeSnippetExpression(opts.GL430Check),
                gl4Statements.ToArray(),
                gl3Statements.ToArray()
            ));
        }



        genclass.Members.Add(compile);
        return GenCodeUnit(compileUnit);
    }

    private static string GenCodeUnit(CodeCompileUnit compileUnit)
    {
        var provider = new CSharpCodeProvider();
        using (var sw = new StringWriter())
        {
            var tw = new IndentedTextWriter(sw, "    ");
            var opts = new CodeGeneratorOptions();
            opts.BlankLinesBetweenMembers = false;
            opts.BracingStyle = "C";
            opts.ElseOnClosing = false;

            provider.GenerateCodeFromCompileUnit(compileUnit, tw, opts);
            return sw.GetStringBuilder().ToString();
        }
    }
}
