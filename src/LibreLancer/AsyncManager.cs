using System;
using System.Threading.Tasks;

namespace LibreLancer
{
	public static class AsyncManager
	{
		public static void RunTask(Action task)
		{
			Task.Run (task);
		}
	}
}

