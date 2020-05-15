// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LancerEdit
{
	public struct MissingReference
	{
		public string Missing;
		public string Reference;
		public MissingReference(string m, string r)
		{
			Missing = m;
			Reference = r;
		}
	}
}
