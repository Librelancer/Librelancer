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
		float[] cutoffs;
        int[] weights;
		float max;
		public WeightedRandomCollection(T[] items, int[] weights)
		{
            if (items.Length < weights.Length)
            {
                weights = weights.Take(items.Length).ToArray();
            }
            else if (items.Length > weights.Length)
            {
                var w2 = new int[items.Length];
                weights.CopyTo(w2, 0);
                weights = w2;
            }
            CalculateCutoffs(weights);
            this.items = items.ShallowCopy();
        }

        void CalculateCutoffs(int[] inWeights)
        {
            this.weights = inWeights;
            max = inWeights.Sum();
            float current = 0;
            cutoffs = new float[inWeights.Length];
            for (int i = 0; i < inWeights.Length; i++)
            {
                cutoffs[i] = current + inWeights[i];
                current += inWeights[i];
            }
        }

        private WeightedRandomCollection()
        {
        }

        public T GetNext(Random random)
		{
			var val = (float)(random.NextDouble() * max);
			for (int i = 0; i < cutoffs.Length; i++)
			{
				if (val < cutoffs[i])
					return items[i];
			}
			return items[items.Length - 1];
		}

        public WeightedRandomCollection<T> Clone() => new WeightedRandomCollection<T>()
        {
            items = items.ShallowCopy(),
            cutoffs = cutoffs.ShallowCopy(),
            max = max,
        };
    }
}

