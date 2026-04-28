// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace LibreLancer.Utf
{
    public class IntermediateNode : Node
    {
        public readonly List<Node> Children = [];

        public IntermediateNode(string name, List<Node> children) : base(name)
        {
            Children = children;
        }


        internal IntermediateNode(ref NodeStruct data, string name, byte[] nodeBlock, StringBlock stringBlock,
            byte[] dataBlock) : base(name)
        {
            if (data.ChildOffset <= 0)
            {
                Children = [];
                return;
            }

            int childCount = 0;
            int next = data.ChildOffset;
            do
            {
                childCount++;
                next = Unsafe.As<byte, int>(ref nodeBlock[next]);
                if (Children.Count > 500000) throw new Exception("Node overflow. Broken UTF?");
            } while (next > 0);

            Children = new(childCount);
            next = data.ChildOffset;
            do
            {
                Node n = FromBuffer(nodeBlock, next, stringBlock, dataBlock);
                Children.Add(n);
                next = Unsafe.As<byte, int>(ref nodeBlock[next]);
            } while (next > 0);
        }


        public override string ToString()
        {
            var result = "{Inter: " + base.ToString() + "{";
            result = Children.Aggregate(result, (current, n) => current + (n + ", "));
            return result + "}";
        }
    }
}
