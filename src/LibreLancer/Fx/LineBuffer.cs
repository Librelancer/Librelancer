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
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
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
