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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;

namespace LibreLancer.Media
{
	public class AudioManager
	{
		//TODO: Heuristics to determine max number of sources
		const int MAX_SOURCES = 32;
		internal AudioContext context;
		internal bool ready = false;
		bool createContext;
		bool running = true;
		//ConcurrentQueues to avoid threading errors
		ConcurrentQueue<int> toRemove = new ConcurrentQueue<int> ();
		ConcurrentQueue<int> toAdd = new ConcurrentQueue<int> ();
		ConcurrentQueue<int> freeSources = new ConcurrentQueue<int>();
		List<int> sfxInstances = new List<int>();
		int musicSource;

		public AudioManager(bool createContext = true)
		{
			this.createContext = createContext;
			for (int i = 0; i < MAX_SOURCES; i++)
			{
				freeSources.Enqueue(AL.GenSource());
			}
			AllocateSource(out musicSource);
			new Thread (new ThreadStart (UpdateThread)).Start ();

		}

		bool AllocateSource(out int source)
		{
			if (freeSources.Count > 0)
			{
				return freeSources.TryDequeue(out source);
			}
			else
			{
				source = -1;
				return false;
			}
		}
		public bool CreateInstance(out SoundEffectInstance instance)
		{
			instance = null;
			int source;
			if (AllocateSource(out source))
			{
				instance = new SoundEffectInstance(this, source);
				return true;
			}
			else
				return false;
		}
		void UpdateThread()
		{
			if(createContext)
				context = new AudioContext ();
			ready = true;
			while (running) {
				//remove from items to update
				while (toRemove.Count > 0) {
					int item;
					if (toRemove.TryDequeue (out item))
						sfxInstances.Remove (item);
				}
				//insert into items to update
				while (toAdd.Count > 0) {
					int item;
					if (toAdd.TryDequeue (out item))
						sfxInstances.Add(item);
				}
				//update
				for (int i = sfxInstances.Count; i >= 0; i--) {
					var state = AL.GetSourceState(sfxInstances[i]);
					if (state != ALSourceState.Playing)
					{
						freeSources.Enqueue(sfxInstances[i]);
						sfxInstances.RemoveAt(i);
						i--;
					}
				}
				Thread.Sleep (5);
			}
		}
		internal static void ALFunc(Action act)
		{
			act ();
			ALError error;
			if ((error = AL.GetError()) != ALError.NoError)
				throw new InvalidOperationException(AL.GetErrorString(error));
		}
		internal void PlayInternal(int sid)
		{
			ALFunc(() => AL.SourcePlay(sid));
			toAdd.Enqueue(sid);
		}
		public void Dispose()
		{
			running = false;
		}
	}
}

