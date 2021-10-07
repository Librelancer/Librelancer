// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
namespace LibreLancer.Utf.Ale
{
	static class AleCrc
	{
		public static readonly Dictionary<uint, string> FxCrc = new Dictionary<uint,string> {
			//MANUAL ENTRIES
			{ 0x1C65B7B9, "BeamApp_LineAppearance" },
			//AUTO-GENERATED ENTRIES
        	{ 0x10423CEB, "AirField_Approach" },
			{ 0xE5E3524C, "AirField_Magnitude" },
			{ 0xC53B008, "Appearance_LODCurve" },
			{ 0x4FC9016, "BasicApp_Alpha" },
			{ 0x1506EB6C, "BasicApp_BlendInfo" },
			{ 0x48767E8, "BasicApp_Color" },
			{ 0xF52B8DD5, "BasicApp_CommonTexFrame" },
			{ 0xF3D52EE4, "BasicApp_FlipTexU" },
			{ 0xE8DC7F5E, "BasicApp_FlipTexV" },
			{ 0xE0E97650, "BasicApp_HToVAspect" },
			{ 0xE91467F1, "BasicApp_MotionBlur" },
			{ 0xF87B5FD5, "BasicApp_QuadTexture" },
			{ 0x5DB630E, "BasicApp_Rotate" },
			{ 0xF7C2EBA9, "BasicApp_Size" },
			{ 0x15A6F47, "BasicApp_TexFrame" },
			{ 0x1BA23359, "BasicApp_TexName" },
			{ 0xF863872E, "BasicApp_TriTexture" },
			{ 0xE2F60EEB, "BasicApp_UseCommonTexFrame" },
			{ 0x8508C35, "BeamApp_DisablePlaceHolder" },
			{ 0xE8B8CDE7, "BeamApp_DupeFirstParticle" },
			{ 0x11336C98, "CollideField_Height" },
			{ 0xF2468210, "CollideField_Reflectivity" },
			{ 0x138BDB51, "CollideField_Width" },
			{ 0xF636F07F, "ConeEmitter_MaxRadius" },
			{ 0xFA4B5E41, "ConeEmitter_MaxSpread" },
			{ 0x1C73D16B, "ConeEmitter_MinRadius" },
			{ 0x100E7F55, "ConeEmitter_MinSpread" },
			{ 0x14B74EB2, "CubeEmitter_Depth" },
			{ 0xEFB0F8CF, "CubeEmitter_Height" },
			{ 0x8C7217F, "CubeEmitter_MaxSpread" },
			{ 0xE282006B, "CubeEmitter_MinSpread" },
			{ 0xFD22A11C, "CubeEmitter_Width" },
			{ 0xE7221F95, "Emitter_EmitCount" },
			{ 0x23C350C, "Emitter_Frequency" },
			{ 0xF9A9D52, "Emitter_InitialParticles" },
			{ 0xA635880, "Emitter_InitLifeSpan" },
			{ 0xFE077E05, "Emitter_LODCurve" },
			{ 0xF5042852, "Emitter_MaxParticles" },
			{ 0xAB180C5, "Emitter_Pressure" },
			{ 0x681233E, "Emitter_VelocityApproach" },
			{ 0xE02B8BD4, "GravityField_Gravity" },
			{ 0xB7EE41C, "MeshApp_MeshId" },
			{ 0x18DADCA8, "MeshApp_MeshName" },
			{ 0xF0004D58, "MeshApp_ParticleTransform" },
			{ 0x1FC112, "MeshApp_UseParticleTransform" },
			{ 0xF54EF296, "Node_ClassName" },
			{ 0xF27FDE7D, "Node_LifeSpan" },
			{ 0xEFA8CE01, "Node_Name" },
			{ 0xE13A59A1, "Node_Transform" },
			{ 0xF267B8E1, "OrientedApp_Height" },
			{ 0xFA03992F, "OrientedApp_Width" },
			{ 0x3B76E6C, "ParticleApp_DeathName" },
			{ 0xEC7A290, "ParticleApp_LifeName" },
			{ 0x49A6DBE, "ParticleApp_SmoothRotation" },
			{ 0xE34C3C55, "ParticleApp_UseDynamicRotation" },
			{ 0xF3CF5EA5, "RadialField_Approach" },
			{ 0x1A433168, "RadialField_Attenuation" },
			{ 0x64B3B9, "RadialField_Magnitude" },
			{ 0xFF28F620, "RadialField_Radius" },
			{ 0xFB7958C1, "RectApp_CenterOnPos" },
			{ 0xE48DDAE2, "RectApp_Length" },
			{ 0xFBA203B8, "RectApp_Scale" },
			{ 0xF9FEBF0D, "RectApp_ViewingAngleFade" },
			{ 0x1266735F, "RectApp_Width" },
			{ 0x1874DE74, "SphereEmitter_MaxRadius" },
			{ 0xF231FF60, "SphereEmitter_MinRadius" },
			{ 0x1B970B9A, "TurbulenceField_Approach" },
			{ 0x1CEAC6D1, "TurbulenceField_Magnitude" },
		};
	}
}

