// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Thn
{
	public enum EventTypes
	{
        UndefinedEvent = 0,
        UserEvent = 1,
		SetCamera = 2,
        StartSound = 3,
        StartLightPropAnim = 4,
        StartCameraPropAnim = 5,
        StartPathAnimation = 6,
        StartSpatialPropAnim = 7,
		AttachEntity = 8,
        ConnectHardpoints = 9,
        StartMotion = 10,
        StartIK = 11,
        StartSubScene = 12, 
		StartPSys = 13,
		StartPSysPropAnim = 14,
        StartAudioPropAnim = 15,
        StartFogPropAnim = 16,
        StartReverbPropAnim = 17,
        StartFloorHeightAnim = 18,
        Subtitle = 19
    }
}

