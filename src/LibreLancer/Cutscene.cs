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
using System.Collections.Generic;
namespace LibreLancer
{
	public class Cutscene
	{
		double currentTime = 0;
		Queue<ThnEvent> events = new Queue<ThnEvent>();
		List<Func<double, bool>> coroutines = new List<Func<double,bool>>();
		ThnScript thn;

		public Cutscene(ThnScript script)
		{
			thn = script;
			foreach (var ev in thn.Events)
				events.Enqueue(ev);
		}

		public void Update(double delta)
		{
			currentTime += delta;
			for (int i = (coroutines.Count - 1); i >= 0; i--)
			{
				if(!coroutines[i](delta))
				{
					coroutines.RemoveAt(i);
					i--;
				}
			}
			while (events.Peek().Time >= currentTime)
			{
				var ev = events.Dequeue();
				ProcessEvent(ev);
			}
		}

		void ProcessEvent(ThnEvent ev)
		{

		}
	}
}

