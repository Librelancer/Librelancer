// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
namespace LibreLancer
{
	public class WeightedRandomCollection<T>
	{
		T[] items;
		float[] weights;
		float max;
		public WeightedRandomCollection(T[] items, int[] weights)
		{
			if (items.Length != weights.Length)
			{
				throw new InvalidOperationException();
			}
			max = weights.Sum();
			float current = 0;
			this.weights = new float[weights.Length];
			for (int i = 0; i < weights.Length; i++)
			{
				this.weights[i] = current + weights[i];
				current += weights[i];
			}
            this.items = items.ShallowCopy();
        }

        private WeightedRandomCollection()
        {
        }

        public T GetNext(Random random)
		{
			var val = (float)(random.NextDouble() * max);
			for (int i = 0; i < weights.Length; i++)
			{
				if (val < weights[i])
					return items[i];
			}
			return items[items.Length - 1];
		}

        public WeightedRandomCollection<T> Clone() => new WeightedRandomCollection<T>()
        {
            items = items.ShallowCopy(),
            weights = weights.ShallowCopy(),
            max = max,
        };
    }
}

