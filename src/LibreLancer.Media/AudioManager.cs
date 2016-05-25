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
		internal bool Ready = false;
		bool createContext;
		bool running = true;
		//ConcurrentQueues to avoid threading errors
		ConcurrentQueue<int> toAdd = new ConcurrentQueue<int> ();
		ConcurrentQueue<int> freeSources = new ConcurrentQueue<int>();
		ConcurrentQueue<int> streamingSources = new ConcurrentQueue<int>();
		List<int> sfxInstances = new List<int>();
		List<StreamingAudio> streamingInstances = new List<StreamingAudio>();
		ConcurrentQueue<StreamingAudio> toAddStreaming = new ConcurrentQueue<StreamingAudio>();
		ConcurrentQueue<StreamingAudio> toRemoveStreaming = new ConcurrentQueue<StreamingAudio>();
		public MusicPlayer Music { get; private set; }

		public AudioManager(bool createContext = true)
		{
			this.createContext = createContext;
			Music = new MusicPlayer(this);

			new Thread (new ThreadStart (UpdateThread)).Start ();

		}

		bool AllocateSource(out int source)
		{
			while (!Ready) { }
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

		internal bool AllocateSourceStreaming(out int source)
		{
			while (!Ready) { }
			if (freeSources.Count > 0)
			{
				return streamingSources.TryDequeue(out source);
			}
			else
			{
				source = -1;
				return false;
			}
		}

		internal void RelinquishSourceStreaming(int source)
		{
			streamingSources.Enqueue(source);
		}

		public bool CreateInstance(out SoundEffectInstance instance, SoundData data)
		{
			instance = null;
			int source;
			if (AllocateSource(out source))
			{
				instance = new SoundEffectInstance(this, source, data);
				return true;
			}
			else
				return false;
		}
		public SoundData AllocateData()
		{
			while (!Ready) { }
			return new SoundData(AL.GenBuffer());
		}
		void UpdateThread()
		{
			if(createContext)
				context = new AudioContext ();
			for (int i = 0; i < MAX_SOURCES; i++)
			{
				freeSources.Enqueue(AL.GenSource());
			}
			int musicSource;
			for (int i = 0; i < 2; i++)
			{
				while (!freeSources.TryDequeue(out musicSource)) {}
				streamingSources.Enqueue(musicSource);
			}
			Console.WriteLine("Ready");
			Ready = true;
			while (running) {
				
				//insert into items to update
				while (toAdd.Count > 0) {
					int item;
					if (toAdd.TryDequeue (out item))
						sfxInstances.Add(item);
				}
				while (toAddStreaming.Count > 0)
				{
					StreamingAudio item;
					if (toAddStreaming.TryDequeue(out item))
						streamingInstances.Add(item);
				}
				//remove items
				while (toRemoveStreaming.Count > 0)
				{
					StreamingAudio item;
					if (toRemoveStreaming.TryDequeue(out item))
					{
						streamingInstances.Remove(item);
					}
				}
				//update SFX
				for (int i = sfxInstances.Count - 1; i >= 0; i--) {
					var state = AL.GetSourceState(sfxInstances[i]);
					if (state != ALSourceState.Playing)
					{
						freeSources.Enqueue(sfxInstances[i]);
						sfxInstances.RemoveAt(i);
						i--;
					}
				}
				//update Streaming
				foreach (var item in streamingInstances)
					item.Update();
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
		internal void Add(StreamingAudio audio)
		{
			toAddStreaming.Enqueue(audio);
		}
		internal void Remove(StreamingAudio audio)
		{
			toRemoveStreaming.Enqueue(audio);
		}
		public void Dispose()
		{
			running = false;
		}
	}
}

