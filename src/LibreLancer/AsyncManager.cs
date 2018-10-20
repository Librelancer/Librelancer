// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

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

