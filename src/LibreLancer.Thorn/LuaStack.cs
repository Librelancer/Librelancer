// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Thorn
{
	public class LuaStack
	{
		object[] stack;
		int stackPtr = -1;
		public object Pop()
		{
			var obj = stack [stackPtr];
			stack [stackPtr] = null;
			stackPtr--;
			return obj;
		}
		public void Push(object obj)
		{
			stackPtr++;
			stack [stackPtr] = obj;
		}
		public object Peek()
		{
			return stack[stackPtr];
		}
		public LuaStack(int size)
		{
			stack = new object[size];
		}
		public object this [int idx] {
			get {
				return stack[idx];
			}
		}
		public int Count {
			get {
				return stackPtr + 1;
			}
		}
	}
}

