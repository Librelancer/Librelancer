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

namespace LibreLancer.Media
{
	public class AudioManager
	{
		//TODO: Heuristics to determine max number of sources
		const int MAX_SOURCES = 32;
		internal bool Ready = false;
		bool running = true;
		//ConcurrentQueues to avoid threading errors
		ConcurrentQueue<uint> toAdd = new ConcurrentQueue<uint> ();
		ConcurrentQueue<uint> freeSources = new ConcurrentQueue<uint>();
		ConcurrentQueue<uint> streamingSources = new ConcurrentQueue<uint>();
		List<uint> sfxInstances = new List<uint>();
		List<StreamingAudio> streamingInstances = new List<StreamingAudio>();
		ConcurrentQueue<StreamingAudio> toAddStreaming = new ConcurrentQueue<StreamingAudio>();
		ConcurrentQueue<StreamingAudio> toRemoveStreaming = new ConcurrentQueue<StreamingAudio>();
		public MusicPlayer Music { get; private set; }

		public AudioManager()
		{
			Music = new MusicPlayer(this);

			new Thread (new ThreadStart (UpdateThread)).Start ();

		}

		bool AllocateSource(out uint source)
		{
			while (!Ready) { }
			if (freeSources.Count > 0)
			{
				return freeSources.TryDequeue(out source);
			}
			else
			{
				source = uint.MaxValue;
				return false;
			}
		}

		internal bool AllocateSourceStreaming(out uint source)
		{
			while (!Ready) { }
			if (freeSources.Count > 0)
			{
				return streamingSources.TryDequeue(out source);
			}
			else
			{
				source = uint.MaxValue;
				return false;
			}
		}

		internal void RelinquishSourceStreaming(uint source)
		{
			streamingSources.Enqueue(source);
		}

		public bool CreateInstance(out SoundEffectInstance instance, SoundData data)
		{
			instance = null;
			uint source;
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
			return new SoundData(Al.GenBuffer());
		}
		void UpdateThread()
		{
			//Init context
			IntPtr dev = Alc.alcOpenDevice(null);
			IntPtr ctx = Alc.alcCreateContext(dev, IntPtr.Zero);
			Alc.alcMakeContextCurrent(ctx);

			for (int i = 0; i < MAX_SOURCES; i++)
			{
				freeSources.Enqueue(Al.GenSource());
			}
			uint musicSource;
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
					uint item;
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
					int state;
					Al.alGetSourcei(sfxInstances[i], Al.AL_SOURCE_STATE, out state);
					if (state != Al.AL_PLAYING)
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
			//Delete context
			Alc.alcMakeContextCurrent(IntPtr.Zero);
			Alc.alcDestroyContext(ctx);
			Alc.alcCloseDevice(ctx);
		}
		internal static void ALFunc(Action act)
		{
			act ();
			int error;
			if ((error = Al.alGetError()) != Al.AL_NO_ERROR)
				throw new InvalidOperationException(Al.GetString(error));
		}
		internal void PlayInternal(uint sid)
		{
			ALFunc(() => Al.alSourcePlay(sid));
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

