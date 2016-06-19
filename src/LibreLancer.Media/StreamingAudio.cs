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
using System.Collections.Concurrent;

namespace LibreLancer.Media
{
	delegate bool BufferNeededHandler(StreamingAudio instance, out byte[] buffer); 
	class StreamingAudio
	{
		public event BufferNeededHandler BufferNeeded;
		public event EventHandler<bool> PlaybackFinished;
		int bufferFormat;
		int sampleRate;
		uint sourceId;
		uint[] bufferIds;
		PlayState currentState = PlayState.Stopped;
		AudioManager manager;
		float volume = 1f;
		public float Volume {
			get {
				return volume;
			} set {
				if (value != volume) {
					volume = value;
					AudioManager.ALFunc (() => Al.alSourcef (sourceId, Al.AL_GAIN, volume));
				}
			}
		}
		internal StreamingAudio (AudioManager device, int format, int sampleRate)
		{
			bufferFormat = format;
			while (!device.Ready) { }
			if (!device.AllocateSourceStreaming(out sourceId))
				throw new Exception("Out of sources for music, should not happen");
			bufferIds = new uint[4];
			AudioManager.ALFunc(() => Al.alGenBuffers(4, bufferIds));
			this.manager = device;
			this.sampleRate = sampleRate;
		}
		bool finished = false;
		bool threadRunning = false;
		bool userStopped = false;
		internal void Update()
		{
			//manage state
			if (currentState == PlayState.Stopped) {
				AudioManager.ALFunc (() => Al.alSourceStopv (1, ref sourceId));
				manager.Remove (this);
				threadRunning = false;
				if (!userStopped) {
					if (PlaybackFinished != null)
						PlaybackFinished (this, true);
				}
				userStopped = false;
				return;
			}
			int state = Al.AL_PLAYING;
			AudioManager.ALFunc(() => Al.alGetSourcei(sourceId, Al.AL_SOURCE_STATE, out state));

			if (currentState == PlayState.Paused) {
				if (state != Al.AL_PAUSED)
					AudioManager.ALFunc(() => Al.alSourcePausev (1, ref sourceId));
				return;
			}

			//load buffers
			int processed_count = 0;
			AudioManager.ALFunc(() => Al.alGetSourcei(sourceId, Al.AL_BUFFERS_PROCESSED, out processed_count));
			while (processed_count > 0) {
				uint bid = 0;

				AudioManager.ALFunc(() => Al.alSourceUnqueueBuffers(sourceId, 1, ref bid));
				if (bid != 0 && !finished) {
					byte[] buffer;
					finished = !BufferNeeded (this, out buffer);
					if (!finished)
					{
						AudioManager.ALFunc(() =>
							Al.BufferData(bid, bufferFormat, buffer, buffer.Length, sampleRate)
						);
						AudioManager.ALFunc(() =>
							Al.alSourceQueueBuffers(sourceId, 1, ref bid)
						);
					}
				}
				--processed_count;
			}
			//check buffer
			if (state == Al.AL_STOPPED && !finished) {
				AudioManager.ALFunc(() => Al.alSourcePlay(sourceId));
			}

			//are we finished?
			if (finished && state == Al.AL_STOPPED) {
				manager.Remove (this);
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
					AudioManager.ALFunc (() =>
						Al.BufferData (bufferIds [i], bufferFormat, buffer, buffer.Length, sampleRate)
					);
					AudioManager.ALFunc (() =>
						Al.alSourceQueueBuffers (sourceId, 1, ref bufferIds [i])
					);
				}
				AudioManager.ALFunc(() =>
				   Al.alSourcePlay(sourceId)
				);
				manager.Add (this);
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
			AudioManager.ALFunc(() => Al.alSourceStopv(1, ref sourceId));
			manager.Remove (this);
			Empty ();
		}

		void Empty()
		{
			int queued = 0;
			AudioManager.ALFunc (() =>
				Al.alGetSourcei (sourceId, Al.AL_BUFFERS_QUEUED, out queued)
			);
			if (queued > 0)
			{
				try
				{
					var temp = new uint[queued];
					AudioManager.ALFunc(() =>
						Al.alSourceUnqueueBuffers(sourceId, queued, temp)
					);
				}
				catch (InvalidOperationException)
				{
					//work around OpenAL bug
					int processed = 0;
					AudioManager.ALFunc (() =>
						Al.alGetSourcei (sourceId, Al.AL_BUFFERS_PROCESSED, out processed)
					);
					var salvaged = new uint[processed];
					if (processed > 0)
					{
						AudioManager.ALFunc (() => 
							Al.alSourceUnqueueBuffers (sourceId, processed, salvaged)
						);
					}
					AudioManager.ALFunc (() => Al.alSourceStopv (1, ref sourceId));
					Empty();
				}
			}
			AudioManager.ALFunc(() => Al.alSourcei(sourceId, Al.AL_BUFFER, 0));
		}
		public PlayState GetState ()
		{
			return currentState;
		}

		public void Dispose ()
		{
			Stop ();
			AudioManager.ALFunc(() => Al.alDeleteBuffers(bufferIds.Length, bufferIds));
			manager.RelinquishSourceStreaming(sourceId);
		}
	}
}