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
	public class CircularBuffer<T>
	{
		public int Capacity { get; private set; }
		T[] backing;
		int head;
		int tail;

		public CircularBuffer(int capacity)
		{
			backing = new T[capacity];
			Capacity = capacity;
			Count = 0;
			head = capacity - 1;
		}

		public int Count { get; private set; }

		public bool Enqueue(T item)
		{
			head = (head + 1) % Capacity;
			backing[head] = item;
			if (Count == Capacity)
			{
				tail = (tail + 1) % Capacity;
				return false;
			}
			Count++;
			return true;
		}

		public T Peek()
		{
			return backing[tail];
		}

		public T Dequeue()
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

		public T this[int index]
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
