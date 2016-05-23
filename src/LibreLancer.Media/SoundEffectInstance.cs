using System;
namespace LibreLancer.Media
{
	public class SoundEffectInstance
	{
		int sid;
		AudioManager au;

		internal SoundEffectInstance(AudioManager manager, int source)
		{
			this.sid = source;
			this.au = manager;
		}

		public void Play()
		{
			au.PlayInternal(sid);
		}
	}
}

