// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Numerics;
using LibreLancer;
using LibreLancer.Thn;
using LibreLancer.Thorn;

namespace thorn2lua
{
    class MainClass
    {
        
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("thorn2lua: input.thn [output.lua]");
                return;
            }
            else if (args.Length == 1)
                Console.WriteLine(ThnDecompile.Decompile(args[0]));
            else
                File.WriteAllText(args[1], ThnDecompile.Decompile(args[0]));

        }
        
    }
}
