/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace LibreLancer
{
    //TODO: Translate shaders to GLES properly
    static class GLSLUtils
    {
        public static void GLESVertex(ref string input)
        {
            var shader = new StringBuilder(input);
            shader.Replace("#version 140", "");
            shader.Replace("#version 150", "");
            shader.Replace("in ", "attribute ");
            shader.Replace("out ", "varying ");
            shader.Replace("mat4x4", "mat4");
            input = shader.ToString();
        }
        static Regex outputRegex = new Regex(@"out\s*vec4\s*([^\s;]*)", RegexOptions.Compiled);
        const string FRAGMENT_PREAMBLE =
             "precision highp float;\n" +
                "vec4 texture(sampler2D sampler, vec2 coords) {\n" +
                "return texture2D(sampler, coords);}\n" +
                "#line 2";
        public static void GLESFragment(ref string input)
        {
            var shader = new StringBuilder(input);
            //Find output variable
            var outvar = outputRegex.Matches(input);
            if(outvar.Count != 1)
            {
                throw new Exception("Shader must only have one output");
            }
            string outputvariable = outvar[0].Groups[1].Captures[0].Value;
            shader.Replace("#version 140", FRAGMENT_PREAMBLE);
            shader.Replace("#version 150", FRAGMENT_PREAMBLE);
            shader.Replace("mat4x4", "mat4");
            shader.Replace("in ", "varying ");
            shader.Replace("out ", "");
            shader.Replace("void main", "void _main");
            shader.AppendLine("void main() {");
            shader.AppendLine("_main();");
            shader.Append("gl_FragColor = ").Append(outputvariable).AppendLine(";");
            shader.AppendLine("}");
            input = shader.ToString();
        }
       
    }
}
