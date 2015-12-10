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
				//CheckALError ();
			}
		}
		internal static void CheckALError()
		{
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

