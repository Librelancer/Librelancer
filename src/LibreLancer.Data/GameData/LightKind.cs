// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Data.GameData
{
	public enum LightKind : byte
	{
		Directional = 0, //DX8 directional light - standard DX8 attenuation
		Point = 1, //Point light - standard DX8 attenuation
		PointAttenCurve = 2, //Point light - IGraph attenuation
		Spotlight = 3 //ugh
	}
}

