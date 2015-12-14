using System;
using System.Linq;
using System.Collections.Concurrent;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
namespace LibreLancer.Media
{
	delegate bool BufferNeededHandler(StreamingAudio instance, out byte[] buffer); 
	class StreamingAudio
	{
		public event BufferNeededHandler BufferNeeded;
		public event EventHandler<bool> PlaybackFinished;
		ALFormat bufferFormat;
		int sampleRate;
		int sourceId;
		int[] bufferIds;
		PlayState currentState = PlayState.Stopped;
		AudioDevice device;
		float volume = 1f;
		public float Volume {
			get {
				return volume;
			} set {
				if (value != volume) {
					volume = value;
					AudioDevice.ALFunc (() => AL.Source (sourceId, ALSourcef.Gain, volume));
				}
			}
		}
		internal StreamingAudio (AudioDevice device, ALFormat format, int sampleRate)
		{
			bufferFormat = format;
			while (!device.ready)
				;
			AudioDevice.ALFunc (() => { sourceId = AL.GenSource(); });
			AudioDevice.ALFunc(() => { bufferIds = AL.GenBuffers (4); });
			this.device = device;
			this.sampleRate = sampleRate;
		}
		bool finished = false;
		bool threadRunning = false;
		bool userStopped = false;
		internal void Update()
		{
			//manage state
			if (currentState == PlayState.Stopped) {
				AudioDevice.ALFunc (() => AL.SourceStop (sourceId));
				device.Remove (this);
				threadRunning = false;
				if (!userStopped) {
					if (PlaybackFinished != null)
						PlaybackFinished (this, true);
				}
				userStopped = false;
				return;
			}
			ALSourceState state = ALSourceState.Playing;
			AudioDevice.ALFunc(() => { state = AL.GetSourceState (sourceId); });

			if (currentState == PlayState.Paused) {
				if (state != ALSourceState.Paused)
					AudioDevice.ALFunc(() => AL.SourcePause (sourceId));
				return;
			}

			//load buffers
			int processed_count;
			AL.GetSource (sourceId, ALGetSourcei.BuffersProcessed, out processed_count);
			while (processed_count > 0) {
				int bid = 0;
				AudioDevice.ALFunc(() => AL.SourceUnqueueBuffer (sourceId));
				if (bid != 0 && !finished) {
					byte[] buffer;
					finished = !BufferNeeded (this, out buffer);
					if (!finished) {
						AudioDevice.ALFunc(() => 
							AL.BufferData (bid, bufferFormat, buffer, buffer.Length, sampleRate)
						);
						AudioDevice.ALFunc(() => 
							AL.SourceQueueBuffer (sourceId, bid)
						);
					}
				}
				--processed_count;
			}
			//check buffer
			if (state == ALSourceState.Stopped && !finished) {
				AudioDevice.ALFunc(() => AL.SourcePlay (sourceId));
			}
			//are we finished?
			if (finished && state == ALSourceState.Stopped) {
				device.Remove (this);
				currentState = PlayState.Stopped;
				threadRunning = false;
				Empty ();
				if(PlaybackFinished != null)
					PlaybackFinished (this, false);
			}
		}

		public void Play ()
		{
			if (currentState == PlayState.Playing)
				return;
			if (currentState == PlayState.Stopped) {
				finished = false;
				currentState = PlayState.Playing;
				for (int i = 0; i < bufferIds.Length; i++) {
					byte[] buffer;
					BufferNeeded (this, out buffer);
					AudioDevice.ALFunc (() =>
						AL.BufferData (bufferIds [i], bufferFormat, buffer, buffer.Length, sampleRate)
					);
					AudioDevice.ALFunc (() =>
						AL.SourceQueueBuffer (sourceId, bufferIds [i])
					);
					AudioDevice.ALFunc (() =>
						AL.SourcePlay (sourceId)
					);
				}
				device.Add (this);
				threadRunning = true;
			}
			currentState = PlayState.Playing;

		}

		public void Pause ()
		{
			currentState = PlayState.Paused;
		}

		public void Stop ()
		{
			if (currentState == PlayState.Stopped)
				return;
			userStopped = true;
			currentState = PlayState.Stopped;
			while (threadRunning)
				;
			AL.SourceStop (sourceId);
			device.Remove (this);
			Empty ();
		}

		void Empty()
		{
			int queued = 0;
			AudioDevice.ALFunc (() =>
				AL.GetSource (sourceId, ALGetSourcei.BuffersQueued, out queued)
			);
			if (queued > 0)
			{
				try
				{
					AudioDevice.ALFunc(() =>
						AL.SourceUnqueueBuffers(sourceId, queued)
					);
				}
				catch (InvalidOperationException)
				{
					//work around OpenAL bug
					int processed = 0;
					AudioDevice.ALFunc (() =>
						AL.GetSource (sourceId, ALGetSourcei.BuffersProcessed, out processed)
					);
					var salvaged = new int[processed];
					if (processed > 0)
					{
						AudioDevice.ALFunc (() => 
							AL.SourceUnqueueBuffers (sourceId, processed, salvaged)
						);
					}
					AudioDevice.ALFunc (() => AL.SourceStop (sourceId));
					Empty();
				}
			}
		}
		public PlayState GetState ()
		{
			return currentState;
		}

		public void Dispose ()
		{
			Stop ();
			AL.DeleteBuffers (bufferIds);
			AL.DeleteSource (sourceId);
		}
	}
}