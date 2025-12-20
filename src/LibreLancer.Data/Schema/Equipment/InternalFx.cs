// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Equipment
{
    [ParsedSection]
	public partial class InternalFx : AbstractEquipment
	{
        [Entry("use_animation")]
		public string UseAnimation;
        [Entry("use_sound")]
		public string UseSound;
	}
}
