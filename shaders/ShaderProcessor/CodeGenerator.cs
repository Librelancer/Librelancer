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

namespace ShaderProcessor
{
    public class CodeGenerator
    {
        public static string AllShaders(CodeGenOptions opts, IEnumerable<EffectFile> files)
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
            
            var compile = new CodeMemberMethod();
            compile.Name = "Compile";
            compile.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            //Once only
            compile.Statements.Add(new CodeConditionStatement(new CodeFieldReferenceExpression(null, "iscompiled"),
                new CodeMethodReturnStatement()));
            compile.Statements.Add(new CodeAssignStatement(
                new CodeFieldReferenceExpression(null, "iscompiled"),
                new CodePrimitiveExpression(true)
            ));
            //Log
            if (opts.Log)
            {
                compile.Statements.Add(
                    new CodeMethodInvokeExpression(null, opts.LogMethod,
                        new CodePrimitiveExpression($"Compiling all shaders"))
                );
            }
            //Compile
            foreach (var fx in files)
            {
                compile.Statements.Add(new CodeMethodInvokeExpression(null, $"{fx.Name}.Compile"));
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
            int i = 0;
            foreach (var e in enums.OrderBy(x => x))
            {
                var field = new CodeMemberField(typeof(int), e);
                field.InitExpression = new CodePrimitiveExpression(1 << i++);
                genclass.Members.Add(field);
            }
            nsroot.Types.Add(genclass);
            return GenCodeUnit(compileUnit);
        }
        static CodeExpression ByteArray(byte[] src)
        {
            var group = src.Select((e, i) => new {Item = e.ToString(), Grouping = (i / 30)})
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
        public static string Generate(CodeGenOptions opts,EffectFile fx, Dictionary<string,int> enumVals)
        {
            var compileUnit = new CodeCompileUnit();
            var nsroot = new CodeNamespace(opts.Namespace);
            nsroot.Imports.Add(new CodeNamespaceImport("System"));
            foreach(var import in opts.Imports)
                nsroot.Imports.Add(new CodeNamespaceImport(import));
            compileUnit.Namespaces.Add(nsroot);
            var genclass = new CodeTypeDeclaration(fx.Name);
            if(!opts.Public) genclass.TypeAttributes = TypeAttributes.Class;
            nsroot.Types.Add(genclass);
            var vsrc = new CodeMemberField(typeof(byte[]), "vertex_bytes");
            vsrc.Attributes = MemberAttributes.Private | MemberAttributes.Static;
            vsrc.InitExpression = ByteArray(Compress.GetBytes(fx.VertexSource, opts.Brotli));
            genclass.Members.Add(vsrc);
            
            var fsrc = new CodeMemberField(typeof(byte[]), "fragment_bytes");
            fsrc.Attributes = MemberAttributes.Private | MemberAttributes.Static;
            fsrc.InitExpression = ByteArray(Compress.GetBytes(fx.FragmentSource, opts.Brotli));
            genclass.Members.Add(fsrc);

            
            var variants = new CodeMemberField(new CodeTypeReference(opts.ShaderType, 1), "variants");
            variants.Attributes = MemberAttributes.Static;
            genclass.Members.Add(variants);

            var iscompiled = new CodeMemberField(typeof(bool), "iscompiled");
            iscompiled.Attributes = MemberAttributes.Private | MemberAttributes.Static;
            iscompiled.InitExpression = new CodePrimitiveExpression(false);
            genclass.Members.Add(iscompiled);

            int mask = 0;
            foreach (var feature in fx.Features)
                mask |= enumVals[feature];
            
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
            int idx = 1;
            foreach (var permutation in FeatureHelper.Permute("", fx.Features))
            {
                int flag = 0;
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
            getShader.Name = "Get";
            getShader.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            getShader.ReturnType = new CodeTypeReference(opts.ShaderType);
            getShader.Parameters.Add(new CodeParameterDeclarationExpression("ShaderFeatures", "features"));
            var retval = new CodeArrayIndexerExpression(new CodeFieldReferenceExpression(null, "variants"),
                new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(null, "GetIndex"),
                    new CodeArgumentReferenceExpression("features")));
            if (fx.Lazy)
            {
                var stmt = new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(null, "Compile"));
                getShader.Statements.Add(stmt);
            }
            getShader.Statements.Add(new CodeMethodReturnStatement(retval));
            genclass.Members.Add(getShader);

            var getZero = new CodeMemberMethod();
            getZero.Name = "Get";
            getZero.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            getZero.ReturnType = new CodeTypeReference(opts.ShaderType);
            if (fx.Lazy)
            {
                var stmt = new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(null, "Compile"));
                getZero.Statements.Add(stmt);
            }
            var zeroRet = new CodeArrayIndexerExpression(new CodeFieldReferenceExpression(null, "variants"),
                new CodePrimitiveExpression(0));
            getZero.Statements.Add(new CodeMethodReturnStatement(zeroRet));
            genclass.Members.Add(getZero);
            
            getShader.ReturnType = new CodeTypeReference(opts.ShaderType);
            var compile = new CodeMemberMethod();
            compile.Name = "Compile";
            compile.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            //Once only
            compile.Statements.Add(new CodeConditionStatement(new CodeFieldReferenceExpression(null, "iscompiled"),
                new CodeMethodReturnStatement()));
            compile.Statements.Add(new CodeAssignStatement(
                new CodeFieldReferenceExpression(null, "iscompiled"),
                new CodePrimitiveExpression(true)
            ));
            //Log
            if (opts.Log)
            {
                compile.Statements.Add(
                    new CodeMethodInvokeExpression(null, opts.LogMethod,
                        new CodePrimitiveExpression($"Compiling {fx.Name}"))
                );
            }
            //Decompress code
            compile.Statements.Add(new CodeVariableDeclarationStatement(typeof(string), "vertsrc"));
            compile.Statements.Add(new CodeVariableDeclarationStatement(typeof(string), "fragsrc"));
            compile.Statements.Add(new CodeAssignStatement(
                    new CodeVariableReferenceExpression("vertsrc"),
                new CodeMethodInvokeExpression(null, "ShCompHelper.FromArray",
                    new CodeFieldReferenceExpression(null, "vertex_bytes"))
                )
            );
            compile.Statements.Add(new CodeAssignStatement(
                new CodeVariableReferenceExpression("fragsrc"),
                new CodeMethodInvokeExpression(null, "ShCompHelper.FromArray",
                    new CodeFieldReferenceExpression(null, "fragment_bytes"))
                )
            );
            var vertRef = new CodeVariableReferenceExpression("vertsrc");
            var fragRef = new CodeVariableReferenceExpression("fragsrc");
            var compMeth = new CodeMethodReferenceExpression(null, opts.ShaderCompileMethod);
            //Init array
            compile.Statements.Add(new CodeAssignStatement(
                new CodeFieldReferenceExpression(null, "variants"), 
                new CodeArrayCreateExpression(opts.ShaderType, idx))
            );
            //Compile null variant
            compile.Statements.Add(new CodeAssignStatement(
                new CodeArrayIndexerExpression(new CodeFieldReferenceExpression(null, "variants"), new CodePrimitiveExpression(0)),
                new CodeMethodInvokeExpression(compMeth, vertRef, fragRef, new CodePrimitiveExpression(""))
            ));
            //Compile all variants
            idx = 1;
            foreach (var permutation in FeatureHelper.Permute(null, fx.Features))
            {
                var builder = new StringBuilder();
                builder.AppendLine();
                foreach (var def in permutation)
                    builder.Append("#define ").AppendLine(def);
                builder.AppendLine("#line 1");
                compile.Statements.Add(new CodeAssignStatement(
                    new CodeArrayIndexerExpression(new CodeFieldReferenceExpression(null, "variants"), new CodePrimitiveExpression(idx++)),
                    new CodeMethodInvokeExpression(compMeth, vertRef, fragRef, new CodePrimitiveExpression(builder.ToString()))
                ));
            }
            genclass.Members.Add(compile);
            return GenCodeUnit(compileUnit);
        }
        
        static string GenCodeUnit(CodeCompileUnit compileUnit)
        {
            var provider = new CSharpCodeProvider();
            using (var sw = new StringWriter())
            {
                IndentedTextWriter tw = new IndentedTextWriter(sw, "    ");
                var opts = new CodeGeneratorOptions();
                opts.BlankLinesBetweenMembers = false;
                opts.BracingStyle = "C";
                opts.ElseOnClosing = false;

                provider.GenerateCodeFromCompileUnit(compileUnit, tw, opts);
                return sw.GetStringBuilder().ToString();
            }
        }
    }
}