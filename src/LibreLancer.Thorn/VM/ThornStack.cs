// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Thorn.VM
{
    class ThornStack
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

        public void MoveUp()
        {
            if (stackPtr == -1) {
                Push(null);
                return;
            }
            for (int i = stackPtr + 1; i > 0; i--) {
                stack[i] = stack[i - 1];
            }
            stack[0] = null;
            stackPtr++;
        }
        public void Pop(int n)
        {
            for(int i = 0; i < n; i++) {
                stack[stackPtr--] = null;
            }
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
		public ThornStack(int size)
		{
			stack = new object[size];
		}
		public object this [int idx] {
			get {
				return stack[idx];
			}
            set
            {
                stack[idx] = value;
            }
		}
		public int Count {
			get {
				return stackPtr + 1;
			}
		}
	}
}

