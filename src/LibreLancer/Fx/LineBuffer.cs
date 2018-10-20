// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer
{
	//Circular buffer for storing FLBeamAppearance points
	//Doesn't resize for 0 allocations
	public struct LinePointer
	{
		public int ParticleIndex;
		public bool Active;
	}

    public class LineBuffer
    {
        private LinePointer[] data;
        private int pointer;
        private int count;

        public LineBuffer(int size)
        {
            data = new LinePointer[size];
            pointer = data.GetLowerBound(0);
        }

        private void Increment()
        {
            if (pointer++ == data.GetUpperBound(0))
            {
                pointer = data.GetLowerBound(0);
            }
        }

        public int Count() => count;
        public int Size => data.Length;

        public LinePointer this[int index]
        {
            get
            {
                var i = pointer - index;
                if (i < 0)
                {
                    i += Size;
                }
                return data[i];
            } set {
                var i = pointer - index;
                if (i < 0)
                    i += Size;
                data[i] = value;
            }
        }

        public void Push(LinePointer item)
        {
            Increment();
            data[pointer] = item;
            if (count <= data.GetUpperBound(0))
            {
                count++;
            }
        }
    }
}
