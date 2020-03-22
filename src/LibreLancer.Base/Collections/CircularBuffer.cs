// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

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
