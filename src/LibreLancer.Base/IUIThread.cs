using System;

namespace LibreLancer
{
	public interface IUIThread
	{
		void EnsureUIThread(Action work);
		void QueueUIThread(Action work);
	}
}

