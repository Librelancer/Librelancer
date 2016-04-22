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
	public class AudioDevice
	{
		internal AudioContext context;
		internal bool ready = false;
		bool createContext;
		bool running = true;
		//ConcurrentQueues to avoid threading errors
		ConcurrentQueue<StreamingAudio> toRemove = new ConcurrentQueue<StreamingAudio> ();
		ConcurrentQueue<StreamingAudio> toAdd = new ConcurrentQueue<StreamingAudio> ();
		List<StreamingAudio> instances = new List<StreamingAudio> ();
		public AudioDevice(bool createContext = true)
		{
			this.createContext = createContext;
			new Thread (new ThreadStart (UpdateThread)).Start ();
		}

		void UpdateThread()
		{
			if(createContext)
				context = new AudioContext ();
			ready = true;
			while (running) {
				//remove from items to update
				while (toRemove.Count > 0) {
					StreamingAudio item;
					if (toRemove.TryDequeue (out item))
						instances.Remove (item);
				}
				//insert into items to update
				while (toAdd.Count > 0) {
					StreamingAudio item;
					if (toAdd.TryDequeue (out item))
						instances.Add(item);
				}
				//update
				for (int i = 0; i < instances.Count; i++) {
					instances [i].Update ();
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
		internal void Add (StreamingAudio audio)
		{
			toAdd.Enqueue (audio);
		}
		internal void Remove(StreamingAudio audio)
		{
			toRemove.Enqueue (audio);
		}
		public void Dispose()
		{
			running = false;
		}
	}
}

