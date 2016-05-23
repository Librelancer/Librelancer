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
		AudioManager device;
		float volume = 1f;
		public float Volume {
			get {
				return volume;
			} set {
				if (value != volume) {
					volume = value;
					AudioManager.ALFunc (() => AL.Source (sourceId, ALSourcef.Gain, volume));
				}
			}
		}
		internal StreamingAudio (AudioManager device, ALFormat format, int sampleRate)
		{
			bufferFormat = format;
			while (!device.ready)
				;
			AudioManager.ALFunc (() => { sourceId = AL.GenSource(); });
			AudioManager.ALFunc(() => { bufferIds = AL.GenBuffers (4); });
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
				AudioManager.ALFunc (() => AL.SourceStop (sourceId));
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
			AudioManager.ALFunc(() => { state = AL.GetSourceState (sourceId); });

			if (currentState == PlayState.Paused) {
				if (state != ALSourceState.Paused)
					AudioManager.ALFunc(() => AL.SourcePause (sourceId));
				return;
			}

			//load buffers
			int processed_count;
			AL.GetSource (sourceId, ALGetSourcei.BuffersProcessed, out processed_count);
			while (processed_count > 0) {
				int bid = 0;
				AudioManager.ALFunc(() => AL.SourceUnqueueBuffer (sourceId));
				if (bid != 0 && !finished) {
					byte[] buffer;
					finished = !BufferNeeded (this, out buffer);
					if (!finished) {
						AudioManager.ALFunc(() => 
							AL.BufferData (bid, bufferFormat, buffer, buffer.Length, sampleRate)
						);
						AudioManager.ALFunc(() => 
							AL.SourceQueueBuffer (sourceId, bid)
						);
					}
				}
				--processed_count;
			}
			//check buffer
			if (state == ALSourceState.Stopped && !finished) {
				AudioManager.ALFunc(() => AL.SourcePlay (sourceId));
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
					AudioManager.ALFunc (() =>
						AL.BufferData (bufferIds [i], bufferFormat, buffer, buffer.Length, sampleRate)
					);
					AudioManager.ALFunc (() =>
						AL.SourceQueueBuffer (sourceId, bufferIds [i])
					);
					AudioManager.ALFunc (() =>
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
			AudioManager.ALFunc (() =>
				AL.GetSource (sourceId, ALGetSourcei.BuffersQueued, out queued)
			);
			if (queued > 0)
			{
				try
				{
					AudioManager.ALFunc(() =>
						AL.SourceUnqueueBuffers(sourceId, queued)
					);
				}
				catch (InvalidOperationException)
				{
					//work around OpenAL bug
					int processed = 0;
					AudioManager.ALFunc (() =>
						AL.GetSource (sourceId, ALGetSourcei.BuffersProcessed, out processed)
					);
					var salvaged = new int[processed];
					if (processed > 0)
					{
						AudioManager.ALFunc (() => 
							AL.SourceUnqueueBuffers (sourceId, processed, salvaged)
						);
					}
					AudioManager.ALFunc (() => AL.SourceStop (sourceId));
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