// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer
{
	public enum WrapMode
	{
		ClampToEdge = GL.GL_CLAMP_TO_EDGE,
		ClampToBorder = GL.GL_CLAMP_TO_BORDER,
		MirroredRepeat = GL.GL_MIRRORED_REPEAT
	}
}

