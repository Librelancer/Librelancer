// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer.ImUI
{
	public abstract class DockTab : IDisposable
	{
        public string DocumentName = "document";
        public string Title = "tab";
		static long _ids = 1;
		static Random rand = new Random();
		protected long Unique;
		protected DockTab()
		{
			Unique = _ids;
			_ids += 2;
		}
		public abstract void Draw();
		public virtual void Update(double elapsed)
		{
		}
		public virtual void Dispose()
		{
		}
	}
}
