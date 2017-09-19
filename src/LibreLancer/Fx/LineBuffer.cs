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
 * Portions created by the Initial Developer are Copyright (C) 2013-2017
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
		public int Capacity { get; private set; }
		LinePointer[] backing;
		int head;
		int tail;

		public LineBuffer(int capacity)
		{
			backing = new LinePointer[capacity];
			Capacity = capacity;
			Count = 0;
			head = capacity - 1;
		}

		public int Count { get; private set; }

		public bool Enqueue(LinePointer item)
		{
			head = (head + 1) % Capacity;
			backing[head] = item;
			if (Count == Capacity) {
				tail = (tail + 1) % Capacity;
				return false;
			}
			Count++;
			return true;
		}

		public LinePointer Peek()
		{
			return backing[tail];
		}

		public LinePointer Dequeue()
		{
			var dequeued = backing[tail];
			tail = (tail + 1) % Capacity;
			Count--;
			return dequeued;
		}

		public void Clear()
		{
			head = Capacity - 1;
			tail = 0;
			Count = 0;
		}

		public LinePointer this[int index]
		{
			get
			{
				if (index < 0 || index >= Count)
					throw new ArgumentOutOfRangeException("index");
				return backing[(tail + index) % Capacity];
			}
			set
			{
				if (index < 0 || index >= Count)
					throw new ArgumentOutOfRangeException("index");
				backing[(tail + index) % Capacity] = value;
			}
		}

	}
}
