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
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.Linq;
namespace LibreLancer
{
	public class WeightedRandomCollection<T>
	{
		Random random;
		T[] items;
		float[] weights;
		float max;
		public WeightedRandomCollection(T[] items, int[] weights)
		{
			if (items.Length != weights.Length)
			{
				throw new InvalidOperationException();
			}
			random = new Random();
			max = weights.Sum();
			float current = 0;
			this.weights = new float[weights.Length];
			for (int i = 0; i < weights.Length; i++)
			{
				this.weights[i] = current + weights[i];
				current += weights[i];
			}
			this.items = items;
		}
		public T GetNext()
		{
			var val = (float)(random.NextDouble() * max);
			for (int i = 0; i < weights.Length; i++)
			{
				if (val < weights[i])
					return items[i];
			}
			return items[items.Length - 1];
		}
	}
}

