// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;

namespace LibreLancer;

public class WeightedRandomCollection<T>
{
    private T[] items;
    private float[] cutoffs = null!;
    private int[] weights = null!;
    private float max;

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
        this.items = items.ShallowCopy() ?? [];
    }

    private void CalculateCutoffs(int[] inWeights)
    {
        weights = inWeights;
        max = inWeights.Sum();
        float current = 0;
        cutoffs = new float[inWeights.Length];
        for (var i = 0; i < inWeights.Length; i++)
        {
            cutoffs[i] = current + inWeights[i];
            current += inWeights[i];
        }
    }

    private WeightedRandomCollection(WeightedRandomCollection<T> collection)
    {
        items = collection.items.ShallowCopy() ?? [];
        cutoffs = collection.cutoffs.ShallowCopy() ?? [];
        max = collection.max;
        weights = collection.weights;
    }

    public T GetNext(Random random)
    {
        var val = (float)(random.NextDouble() * max);
        for (var i = 0; i < cutoffs.Length; i++)
        {
            if (val < cutoffs[i])
                return items[i];
        }
        return items[^1];
    }

    public WeightedRandomCollection<T> Clone() => new(this);
}
