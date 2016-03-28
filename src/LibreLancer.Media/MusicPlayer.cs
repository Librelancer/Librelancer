#if false
using System;

namespace LibreLancer.Media
{
	public class MusicPlayer
	{
		AudioDevice dev;
		MusicDecoder dec;
		StreamingAudio stream;
		public MusicPlayer (AudioDevice adev)
		{
			dev = adev;
		}

		public void Play(string filename, bool loop = false)
		{
			Stop ();
			dec = new MusicDecoder (filename);
			stream = new StreamingAudio (dev, dec.Format, dec.Frequency);
			stream.BufferNeeded += (StreamingAudio instance, out byte[] buffer) => {
				byte[] buf = null;
				var ret = dec.GetBuffer(ref buf);
				buffer = buf;
				return ret;
			};
			stream.PlaybackFinished += (sender, e) => {
				if(loop)
					Play(filename);
				else {
					stream.Dispose();
					dec.Dispose();
					stream = null;
				}
			};
			stream.Play ();
		}

		public void Stop()
		{
			if(State == PlayState.Playing) {
				stream.Stop ();
				stream.Dispose();
				dec.Dispose();
				stream = null;
			}
		}

		public PlayState State {
			get {
				return stream == null ? PlayState.Stopped : PlayState.Playing;
			}
		}
	}
}
#endif

